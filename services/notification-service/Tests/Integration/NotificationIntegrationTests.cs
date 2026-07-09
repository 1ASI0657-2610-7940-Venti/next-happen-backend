using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextHappen.Notification.Infrastructure.Persistence;
using Xunit;

namespace NextHappen.Notification.Tests.Integration;

public class NotificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NotificationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NotificationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<NotificationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestNotificationDb_" + Guid.NewGuid().ToString());
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateNotification_ShouldReturnOk()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Message = "Integration test message"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
