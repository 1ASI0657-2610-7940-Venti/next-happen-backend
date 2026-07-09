using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextHappen.Event.Application.DTOs;
using NextHappen.Event.Domain.Entities;
using NextHappen.Event.Infrastructure.Persistence;
using Xunit;

namespace NextHappen.Event.Tests.Integration;

public class EventIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public EventIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var dbName = "TestEventDb_" + Guid.NewGuid().ToString();
        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "ThisIsASuperSecretKeyForTestingOnly1234567890");
            builder.UseSetting("Jwt:Issuer", "NextHappen");
            builder.UseSetting("Jwt:Audience", "NextHappenUsers");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<EventDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        });
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private static string GenerateToken(Guid userId, string role = "Organizer")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASuperSecretKeyForTestingOnly1234567890"));
        var token = new JwtSecurityToken(
            issuer: "NextHappen",
            audience: "NextHappenUsers",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test Org"),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthHeader(Guid userId, string role = "Organizer")
    {
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GenerateToken(userId, role)}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAll_ShouldReturnEmptyList_Initially()
    {
        var response = await _client.GetAsync("/api/events");

        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<IEnumerable<EventResponse>>();
        Assert.Empty(events!);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_ShouldReturnCreated_AndPersistData()
    {
        SetAuthHeader(Guid.NewGuid());
        var request = new CreateEventRequest
        {
            Title = "Integration Test Event",
            Organizer = "Test Org",
            Description = "Testing the full stack",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Category = "Test",
            IsPublic = true
        };

        var postResponse = await _client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var createdEvent = await postResponse.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(createdEvent);
        Assert.Equal(request.Title, createdEvent.Title);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetById_ShouldReturnEvent_WhenExists()
    {
        SetAuthHeader(Guid.NewGuid());
        var createRequest = new CreateEventRequest
        {
            Title = "Find Me",
            Organizer = "Org",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Category = "Test",
            IsPublic = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        var response = await _client.GetAsync($"/api/events/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var retrieved = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetById_ShouldReturnNotFound_WhenNotExists()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Stands_Get_ShouldReturnEmptyList()
    {
        var eventId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/events/{eventId}/stands");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stands = await response.Content.ReadFromJsonAsync<List<AssignedStand>>();
        Assert.Empty(stands!);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Stands_Assign_ShouldReturnOk()
    {
        var eventId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/stands",
            new { Name = "Stand A", Category = "Food" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stand = await response.Content.ReadFromJsonAsync<AssignedStand>();
        Assert.NotNull(stand);
        Assert.Equal("Stand A", stand.Name);
    }
}
