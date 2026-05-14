using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextHappen.Event.Application.DTOs;
using NextHappen.Event.Infrastructure.Persistence;
using Xunit;

namespace NextHappen.Event.Tests.Integration;

public class EventIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EventIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<EventDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestEventDb_" + Guid.NewGuid().ToString());
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAll_ShouldReturnEmptyList_Initially()
    {
        // Act
        var response = await _client.GetAsync("/api/events");

        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<IEnumerable<EventResponse>>();
        Assert.Empty(events!);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_ShouldReturnCreated_AndPersistData()
    {
        // Arrange
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

        // Act
        var postResponse = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        
        var createdEvent = await postResponse.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(createdEvent);
        Assert.Equal(request.Title, createdEvent.Title);
    }
}
