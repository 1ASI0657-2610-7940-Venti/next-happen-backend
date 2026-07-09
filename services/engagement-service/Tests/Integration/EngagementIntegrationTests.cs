using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Metrics_RegisterEventView_ShouldReturnOk()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act - Using MetricsController which doesn't require Authorization
        var response = await _client.PostAsync($"/api/metrics/event-view/{eventId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
