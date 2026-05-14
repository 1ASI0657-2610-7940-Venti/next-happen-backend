using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextHappen.Ticket.Infrastructure.Persistence;
using Xunit;
using Moq;
using Moq.Protected;

namespace NextHappen.Ticket.Tests.Integration;

public class TicketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TicketIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TicketDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<TicketDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestTicketDb_" + Guid.NewGuid().ToString());
                });

                // Mock HttpClient for EventService
                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(new { Id = Guid.NewGuid(), Price = 100.0m })
                    });

                var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://event-service") };
                var factoryMock = new Mock<IHttpClientFactory>();
                factoryMock.Setup(f => f.CreateClient("EventService")).Returns(httpClient);
                
                services.AddSingleton(factoryMock.Object);
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Purchase_ShouldReturnSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var quantity = 2;
        
        // Correct route: api/events/{eventId}/tickets/purchase?userId={userId}&quantity={quantity}
        var url = $"/api/events/{eventId}/tickets/purchase?userId={userId}&quantity={quantity}";

        // Act
        var response = await _client.PostAsync(url, null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
