using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextHappen.Notification.Domain.Entities;
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
                    options.UseInMemoryDatabase("TestNotificationDb");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateNotification_ShouldReturnOk()
    {
        var request = new
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Message = "Integration test message"
        };

        var response = await _client.PostAsJsonAsync("/api/notifications", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByUser_ShouldReturnNotifications()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/notifications", new { UserId = userId, EventId = eventId, Message = "Notif 1" });
        await _client.PostAsJsonAsync("/api/notifications", new { UserId = userId, EventId = eventId, Message = "Notif 2" });

        var response = await _client.GetAsync($"/api/notifications/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<Domain.Entities.Notification>>();
        Assert.NotNull(notifications);
        Assert.Equal(2, notifications.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByUser_ShouldReturnEmpty_WhenNoNotifications()
    {
        var userId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/notifications/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<Domain.Entities.Notification>>();
        Assert.Empty(notifications!);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MarkAsRead_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var createResponse = await _client.PostAsJsonAsync("/api/notifications", new { UserId = userId, EventId = eventId, Message = "Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<Domain.Entities.Notification>();
        Assert.NotNull(created);

        var response = await _client.PostAsync($"/api/notifications/{created.Id}/read", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
