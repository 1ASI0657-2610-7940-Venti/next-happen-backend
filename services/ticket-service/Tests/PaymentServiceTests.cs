using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NextHappen.Contracts.Events;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;
using NextHappen.Ticket.Infrastructure.Http;
using NextHappen.Ticket.Infrastructure.Payments;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace NextHappen.Ticket.Tests;

public class PaymentServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<ITicketService> _ticketServiceMock;
    private readonly Mock<IEventCatalogClient> _eventsMock;
    private readonly Mock<IPublishEndpoint> _publishMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IRefundService> _refundServiceMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _ticketRepoMock = new Mock<ITicketRepository>();
        _ticketServiceMock = new Mock<ITicketService>();
        _eventsMock = new Mock<IEventCatalogClient>();
        _publishMock = new Mock<IPublishEndpoint>();
        _sessionServiceMock = new Mock<ISessionService>();
        _refundServiceMock = new Mock<IRefundService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        var stripeOptions = Options.Create(new StripeOptions
        {
            Currency = "pen",
            FrontendBaseUrl = "http://localhost:5173"
        });

        _service = new PaymentService(
            _orderRepoMock.Object,
            _ticketRepoMock.Object,
            _ticketServiceMock.Object,
            _eventsMock.Object,
            _publishMock.Object,
            _sessionServiceMock.Object,
            _refundServiceMock.Object,
            stripeOptions,
            _loggerMock.Object);
    }

    private static EventInfo MakeEvent(decimal? price = 100m, string title = "Test Event")
        => new EventInfo(Guid.NewGuid(), title, price, "Org");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateCheckoutSessionAsync_ShouldCreateSession_WhenEventValid()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ev = MakeEvent(100m);
        _eventsMock.Setup(e => e.GetEventAsync(eventId)).ReturnsAsync(ev);
        _eventsMock.Setup(e => e.ReserveSeatsAsync(eventId, 2)).ReturnsAsync(true);
        _sessionServiceMock.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>()))
            .ReturnsAsync(new Session { Id = "cs_test", Url = "https://checkout.stripe.com/test" });

        var result = await _service.CreateCheckoutSessionAsync(userId, eventId, 2);

        Assert.NotNull(result);
        Assert.Equal("https://checkout.stripe.com/test", result.CheckoutUrl);
        _orderRepoMock.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.UserId == userId && o.EventId == eventId && o.Quantity == 2 &&
            o.Status == OrderStatus.Pending && o.StripeSessionId == "cs_test")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateCheckoutSessionAsync_ShouldThrow_WhenQuantityInvalid()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateCheckoutSessionAsync(Guid.NewGuid(), Guid.NewGuid(), 0));

        Assert.Equal("La cantidad debe ser al menos 1.", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateCheckoutSessionAsync_ShouldThrow_WhenEventNotFound()
    {
        _eventsMock.Setup(e => e.GetEventAsync(It.IsAny<Guid>())).ReturnsAsync((EventInfo?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateCheckoutSessionAsync(Guid.NewGuid(), Guid.NewGuid(), 1));

        Assert.Contains("no existe", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateCheckoutSessionAsync_ShouldThrow_WhenPriceIsZero()
    {
        var ev = MakeEvent(0m);
        _eventsMock.Setup(e => e.GetEventAsync(It.IsAny<Guid>())).ReturnsAsync(ev);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateCheckoutSessionAsync(Guid.NewGuid(), Guid.NewGuid(), 1));

        Assert.Contains("precio válido", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateCheckoutSessionAsync_ShouldReleaseSeats_OnStripeException()
    {
        var eventId = Guid.NewGuid();
        _eventsMock.Setup(e => e.GetEventAsync(eventId)).ReturnsAsync(MakeEvent());
        _eventsMock.Setup(e => e.ReserveSeatsAsync(eventId, 1)).ReturnsAsync(true);
        _sessionServiceMock.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>())).ThrowsAsync(new StripeException());

        await Assert.ThrowsAsync<StripeException>(() =>
            _service.CreateCheckoutSessionAsync(Guid.NewGuid(), eventId, 1));

        _eventsMock.Verify(e => e.ReleaseSeatsAsync(eventId, 1), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleStripeEventAsync_CheckoutCompleted_ShouldMarkPaidAndIssueTickets()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending };
        _orderRepoMock.Setup(r => r.GetBySessionIdAsync("cs_test")).ReturnsAsync(order);
        _ticketServiceMock.Setup(s => s.IssueTicketsForOrderAsync(order))
            .ReturnsAsync(new List<Domain.Entities.Ticket>
            {
                new Domain.Entities.Ticket { Id = Guid.NewGuid(), EventId = order.EventId, UserId = order.UserId }
            });

        var stripeEvent = new Stripe.Event
        {
            Type = "checkout.session.completed",
            Data = new Stripe.EventData
            {
                Object = new Session { Id = "cs_test", PaymentIntentId = "pi_test" }
            }
        };

        await _service.HandleStripeEventAsync(stripeEvent);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.PaidAt);
        Assert.Equal("pi_test", order.StripePaymentIntentId);
        _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _ticketServiceMock.Verify(s => s.IssueTicketsForOrderAsync(order), Times.Once);
        _publishMock.Verify(p => p.Publish(It.IsAny<TicketPurchasedEvent>(), default), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleStripeEventAsync_CheckoutExpired_ShouldMarkFailedAndReleaseSeats()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Quantity = 2,
            Status = OrderStatus.Pending
        };
        _orderRepoMock.Setup(r => r.GetBySessionIdAsync("cs_expired")).ReturnsAsync(order);

        var stripeEvent = new Stripe.Event
        {
            Type = "checkout.session.expired",
            Data = new Stripe.EventData
            {
                Object = new Session { Id = "cs_expired" }
            }
        };

        await _service.HandleStripeEventAsync(stripeEvent);

        Assert.Equal(OrderStatus.Failed, order.Status);
        _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _eventsMock.Verify(e => e.ReleaseSeatsAsync(order.EventId, order.Quantity), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConfirmSessionAsync_ShouldConfirm_WhenPaymentPaid()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Quantity = 1
        };
        _orderRepoMock.Setup(r => r.GetBySessionIdAsync("cs_test")).ReturnsAsync(order);
        _ticketServiceMock.Setup(s => s.IssueTicketsForOrderAsync(order))
            .ReturnsAsync(new List<Domain.Entities.Ticket> { new Domain.Entities.Ticket { Id = Guid.NewGuid() } });

        var session = new Session { PaymentStatus = "paid", PaymentIntentId = "pi_test" };
        _sessionServiceMock.Setup(s => s.GetAsync("cs_test")).ReturnsAsync(session);

        var result = await _service.ConfirmSessionAsync("cs_test", order.UserId, false);

        Assert.Equal(OrderStatus.Paid, result.Status);
        Assert.Equal(1, result.Quantity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConfirmSessionAsync_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetBySessionIdAsync("unknown")).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ConfirmSessionAsync("unknown", Guid.NewGuid(), false));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConfirmSessionAsync_ShouldThrow_WhenNotOwnerAndNotAdmin()
    {
        var order = new Order { UserId = Guid.NewGuid() };
        _orderRepoMock.Setup(r => r.GetBySessionIdAsync("cs_test")).ReturnsAsync(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ConfirmSessionAsync("cs_test", Guid.NewGuid(), false));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RefundTicketAsync_ShouldRefund_WhenValid()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Price = 50m,
            Status = TicketStatus.Active,
            OrderId = Guid.NewGuid()
        };
        var order = new Order
        {
            Id = ticket.OrderId,
            StripePaymentIntentId = "pi_test",
            Currency = "pen"
        };

        _ticketRepoMock.Setup(r => r.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);
        _orderRepoMock.Setup(r => r.GetByIdAsync(ticket.OrderId)).ReturnsAsync(order);

        await _service.RefundTicketAsync(ticket.Id, ticket.UserId, false);

        _refundServiceMock.Verify(r => r.CreateAsync(It.Is<RefundCreateOptions>(o =>
            o.PaymentIntent == "pi_test" &&
            o.Amount == 5000)), Times.Once);
        _ticketRepoMock.Verify(r => r.UpdateAsync(It.Is<Domain.Entities.Ticket>(t =>
            t.Status == TicketStatus.Refunded)), Times.Once);
        _eventsMock.Verify(e => e.ReleaseSeatsAsync(ticket.EventId, 1), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RefundTicketAsync_ShouldThrow_WhenAlreadyRefunded()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = TicketStatus.Refunded
        };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RefundTicketAsync(ticket.Id, ticket.UserId, false));

        Assert.Contains("ya fue reembolsada", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RefundTicketAsync_ShouldThrow_WhenAlreadyUsed()
    {
        var ticket = new Domain.Entities.Ticket
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = TicketStatus.Used
        };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RefundTicketAsync(ticket.Id, ticket.UserId, false));

        Assert.Contains("ya utilizada", exception.Message);
    }
}