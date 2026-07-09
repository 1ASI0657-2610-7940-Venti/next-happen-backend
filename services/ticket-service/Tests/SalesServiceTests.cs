using Moq;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;
using Xunit;

namespace NextHappen.Ticket.Tests;

public class SalesServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly SalesService _service;

    public SalesServiceTests()
    {
        _ticketRepoMock = new Mock<ITicketRepository>();
        _service = new SalesService(_ticketRepoMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetForEventAsync_ShouldAggregateCorrectly()
    {
        var eventId = Guid.NewGuid();
        var tickets = new List<Domain.Entities.Ticket>
        {
            new Domain.Entities.Ticket { EventId = eventId, Status = TicketStatus.Active, Price = 100m, PurchaseDate = new DateTime(2025, 6, 1, 10, 0, 0) },
            new Domain.Entities.Ticket { EventId = eventId, Status = TicketStatus.Used, Price = 100m, PurchaseDate = new DateTime(2025, 6, 1, 11, 0, 0) },
            new Domain.Entities.Ticket { EventId = eventId, Status = TicketStatus.Refunded, Price = 100m },
            new Domain.Entities.Ticket { EventId = eventId, Status = TicketStatus.Cancelled, Price = 100m },
        };
        _ticketRepoMock.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(tickets);

        var result = await _service.GetForEventAsync(eventId);

        Assert.Equal(eventId, result.EventId);
        Assert.Equal(2, result.TicketsSold);
        Assert.Equal(1, result.TicketsValidated);
        Assert.Equal(1, result.TicketsRefunded);
        Assert.Equal(200m, result.GrossRevenue);
        Assert.Equal(100m, result.RefundedAmount);
        Assert.Equal(200m, result.NetRevenue);
        Assert.Single(result.ByDay);
        Assert.Equal("2025-06-01", result.ByDay[0].Date);
        Assert.Equal(2, result.ByDay[0].Tickets);
        Assert.Equal(200m, result.ByDay[0].Revenue);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetForEventAsync_ShouldReturnEmpty_WhenNoTickets()
    {
        var eventId = Guid.NewGuid();
        _ticketRepoMock.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(new List<Domain.Entities.Ticket>());

        var result = await _service.GetForEventAsync(eventId);

        Assert.Equal(eventId, result.EventId);
        Assert.Equal(0, result.TicketsSold);
        Assert.Equal(0, result.TicketsValidated);
        Assert.Equal(0, result.TicketsRefunded);
        Assert.Equal(0m, result.GrossRevenue);
        Assert.Empty(result.ByDay);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetForEventsAsync_ShouldReturnMultipleSummaries()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var tickets1 = new List<Domain.Entities.Ticket>
        {
            new Domain.Entities.Ticket { EventId = eventId1, Status = TicketStatus.Active, Price = 50m, PurchaseDate = DateTime.UtcNow },
        };
        var tickets2 = new List<Domain.Entities.Ticket>
        {
            new Domain.Entities.Ticket { EventId = eventId2, Status = TicketStatus.Active, Price = 100m, PurchaseDate = DateTime.UtcNow },
            new Domain.Entities.Ticket { EventId = eventId2, Status = TicketStatus.Active, Price = 100m, PurchaseDate = DateTime.UtcNow },
        };
        _ticketRepoMock.Setup(r => r.GetByEventIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(tickets1.Concat(tickets2).ToList());

        var result = await _service.GetForEventsAsync(new[] { eventId1, eventId2 });

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].TicketsSold);
        Assert.Equal(2, result[1].TicketsSold);
    }
}