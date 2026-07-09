using Moq;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;
using Xunit;

namespace NextHappen.Ticket.Tests;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _repoMock;
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _repoMock = new Mock<ITicketRepository>();
        _service = new TicketService(_repoMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IssueTicketsForOrderAsync_ShouldCreateTicketsAndSave()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Quantity = 3,
            UnitPrice = 50.0m,
            PaidAt = DateTime.UtcNow
        };

        var result = await _service.IssueTicketsForOrderAsync(order);

        Assert.Equal(3, result.Count);
        Assert.All(result, t =>
        {
            Assert.Equal(order.UserId, t.UserId);
            Assert.Equal(order.EventId, t.EventId);
            Assert.Equal(order.Id, t.OrderId);
            Assert.Equal(order.UnitPrice, t.Price);
            Assert.Equal(TicketStatus.Active, t.Status);
            Assert.StartsWith("NH-", t.QrCode);
            Assert.NotNull(t.ShortCode);
            Assert.Equal(6, t.ShortCode.Length);
        });
        _repoMock.Verify(r => r.AddRangeAsync(It.Is<List<Domain.Entities.Ticket>>(list => list.Count == 3)), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IssueTicketsForOrderAsync_ShouldUsePaidAt_WhenProvided()
    {
        var paidAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order
        {
            Quantity = 1,
            PaidAt = paidAt,
            UnitPrice = 1.0m
        };

        var result = await _service.IssueTicketsForOrderAsync(order);

        Assert.Equal(paidAt, result[0].PurchaseDate);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IssueTicketsForOrderAsync_ShouldGenerateUniqueCodes()
    {
        var order = new Order { Quantity = 10, UnitPrice = 1.0m, PaidAt = DateTime.UtcNow };

        var result = await _service.IssueTicketsForOrderAsync(order);

        var shortCodes = result.Select(t => t.ShortCode).Distinct();
        var qrCodes = result.Select(t => t.QrCode).Distinct();
        Assert.Equal(10, shortCodes.Count());
        Assert.Equal(10, qrCodes.Count());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUserAsync_ShouldReturnTickets()
    {
        var userId = Guid.NewGuid();
        var tickets = new List<Domain.Entities.Ticket>
        {
            new Domain.Entities.Ticket { UserId = userId, EventId = Guid.NewGuid() },
            new Domain.Entities.Ticket { UserId = userId, EventId = Guid.NewGuid() },
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(tickets);

        var result = await _service.GetByUserAsync(userId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEventTicketsAsync_ShouldReturnOrderedRows_WithoutQrCode()
    {
        var eventId = Guid.NewGuid();
        var tickets = new List<Domain.Entities.Ticket>
        {
            new Domain.Entities.Ticket { EventId = eventId, Status = TicketStatus.Active, Price = 50m, PurchaseDate = DateTime.UtcNow.AddHours(-1), ShortCode = "ABC123" },
            new Domain.Entities.Ticket { EventId = eventId, Status = TicketStatus.Used, Price = 30m, PurchaseDate = DateTime.UtcNow, ShortCode = "XYZ789" },
        };
        _repoMock.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(tickets);

        var result = await _service.GetEventTicketsAsync(eventId);

        Assert.Equal(2, result.Count);
        Assert.All(result, row =>
        {
            Assert.False(string.IsNullOrEmpty(row.ShortCode));
            Assert.NotNull(row.Status);
        });
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_ShouldReturnTicket_WhenExists()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Domain.Entities.Ticket { Id = ticketId };
        _repoMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

        var result = await _service.GetByIdAsync(ticketId);

        Assert.NotNull(result);
        Assert.Equal(ticketId, result.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var ticketId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync((Domain.Entities.Ticket?)null);

        var result = await _service.GetByIdAsync(ticketId);

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenActive()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            QrCode = "NH-TESTQR"
        };
        _repoMock.Setup(r => r.GetByQrCodeAsync("NH-TESTQR")).ReturnsAsync(ticket);

        var result = await _service.ValidateAsync("NH-TESTQR");

        Assert.True(result.Valid);
        Assert.Equal("Ingreso permitido.", result.Message);
        Assert.Equal(TicketStatus.Used, result.Status);
        _repoMock.Verify(r => r.UpdateAsync(ticket), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenAlreadyUsed()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Status = TicketStatus.Used,
            ValidatedAt = new DateTime(2025, 6, 1, 14, 0, 0),
            QrCode = "USED"
        };
        _repoMock.Setup(r => r.GetByQrCodeAsync("USED")).ReturnsAsync(ticket);

        var result = await _service.ValidateAsync("USED");

        Assert.False(result.Valid);
        Assert.Contains("ya utilizada", result.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenRefunded()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Status = TicketStatus.Refunded,
            QrCode = "REFUNDED"
        };
        _repoMock.Setup(r => r.GetByQrCodeAsync("REFUNDED")).ReturnsAsync(ticket);

        var result = await _service.ValidateAsync("REFUNDED");

        Assert.False(result.Valid);
        Assert.Contains("reembolsada", result.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenCancelled()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Status = TicketStatus.Cancelled,
            QrCode = "CANCELLED"
        };
        _repoMock.Setup(r => r.GetByQrCodeAsync("CANCELLED")).ReturnsAsync(ticket);

        var result = await _service.ValidateAsync("CANCELLED");

        Assert.False(result.Valid);
        Assert.Contains("cancelada", result.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByQrCodeAsync(It.IsAny<string>())).ReturnsAsync((Domain.Entities.Ticket?)null);
        _repoMock.Setup(r => r.GetByShortCodeAsync(It.IsAny<string>())).ReturnsAsync((Domain.Entities.Ticket?)null);

        var result = await _service.ValidateAsync("UNKNOWN");

        Assert.False(result.Valid);
        Assert.Contains("no encontrada", result.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldAcceptShortCode_WhenQrNotFound()
    {
        var shortCode = "7K4P9Q";
        var ticket = new Domain.Entities.Ticket { Status = TicketStatus.Active, QrCode = "QR", ShortCode = shortCode };
        _repoMock.Setup(r => r.GetByQrCodeAsync(shortCode)).ReturnsAsync((Domain.Entities.Ticket?)null);
        _repoMock.Setup(r => r.GetByShortCodeAsync(shortCode)).ReturnsAsync(ticket);

        var result = await _service.ValidateAsync(shortCode);

        Assert.True(result.Valid);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenEmptyCode()
    {
        var result = await _service.ValidateAsync("");

        Assert.False(result.Valid);
        Assert.Contains("Ingresa un código", result.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateAsync_ShouldReturnFailure_WhenWhiteSpaceCode()
    {
        var result = await _service.ValidateAsync("   ");

        Assert.False(result.Valid);
        Assert.Contains("Ingresa un código", result.Message);
    }
}