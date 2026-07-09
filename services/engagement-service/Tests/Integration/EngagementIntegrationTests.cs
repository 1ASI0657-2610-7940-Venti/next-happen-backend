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
using NextHappen.Engagement.Application.DTOs;
using NextHappen.Engagement.Infrastructure.Persistence;
using Xunit;

namespace NextHappen.Engagement.Tests.Integration;

public class EngagementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EngagementIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "ThisIsASuperSecretKeyForTestingOnly1234567890");
            builder.UseSetting("Jwt:Issuer", "NextHappen");
            builder.UseSetting("Jwt:Audience", "NextHappenUsers");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EngagementDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<EngagementDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestEngagementDb_" + Guid.NewGuid().ToString());
                });
            });
        });
        _client = _factory.CreateClient();
    }

    private static string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASuperSecretKeyForTestingOnly1234567890"));
        var token = new JwtSecurityToken(
            issuer: "NextHappen",
            audience: "NextHappenUsers",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, "User")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthHeader(Guid userId)
    {
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GenerateToken(userId)}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Metrics_RegisterEventView_ShouldReturnOk()
    {
        var eventId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/metrics/event-view/{eventId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Reviews_GetForEvent_ShouldReturnSummary_WhenPublic()
    {
        var eventId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/events/{eventId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<ReviewSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(eventId, summary.EventId);
        Assert.Equal(0, summary.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Reviews_Create_ShouldReturnOk_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetAuthHeader(userId);

        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/reviews",
            new CreateReviewRequest { Rating = 5, Comment = "Excelente" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<ReviewResponse>();
        Assert.NotNull(review);
        Assert.Equal(5, review.Rating);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Reviews_Create_ShouldReturnUnauthorized_WithoutToken()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");
        var eventId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/reviews",
            new { Rating = 5, Comment = "Test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SavedEvents_Save_ShouldReturnOk_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetAuthHeader(userId);

        var response = await _client.PostAsync($"/api/users/{userId}/saved-events/{eventId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SavedEvents_Save_ShouldReturnUnauthorized_WithoutToken()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Remove("Authorization");

        var response = await _client.PostAsync($"/api/users/{userId}/saved-events/{eventId}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SavedEvents_GetAll_ShouldReturnList_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        SetAuthHeader(userId);

        var response = await _client.GetAsync($"/api/users/{userId}/saved-events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
