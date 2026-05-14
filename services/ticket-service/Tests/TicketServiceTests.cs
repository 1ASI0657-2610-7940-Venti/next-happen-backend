using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Domain.Repositories;
using NextHappen.Ticket.Domain.Entities;
using Xunit;

namespace NextHappen.Ticket.Tests;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<IHttpClientFactory> _httpFactoryMock;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly TicketService _ticketService;

    public TicketServiceTests()
    {
        _ticketRepoMock = new Mock<ITicketRepository>();
        _httpFactoryMock = new Mock<IHttpClientFactory>();
        _handlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://event-service")
        };

        _httpFactoryMock.Setup(f => f.CreateClient("EventService")).Returns(httpClient);
        _ticketService = new TicketService(_ticketRepoMock.Object, _httpFactoryMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PurchaseAsync_ShouldCreateTickets_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var quantity = 2;
        var eventInfo = new { Id = eventId, Price = 50.0m };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(eventInfo)
            });

        // Act
        var result = await _ticketService.PurchaseAsync(eventId, userId, quantity);

        // Assert
        Assert.NotNull(result);
        _ticketRepoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Ticket>()), Times.Exactly(quantity));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PurchaseAsync_ShouldThrowException_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _ticketService.PurchaseAsync(eventId, Guid.NewGuid(), 1));
    }
}
