using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextHappen.Ticket.API.Controllers;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Application.Services;
using Xunit;

namespace NextHappen.Ticket.Tests;

public class TicketControllerTests
{
    private readonly Mock<ITicketService> _serviceMock;
    private readonly TicketController _controller;
    private readonly FakePaymentService _payments;

    public TicketControllerTests()
    {
        _serviceMock = new Mock<ITicketService>();
        _payments = new FakePaymentService();
        _controller = new TicketController(_serviceMock.Object, _payments);
    }

    private void SetUser(Guid userId, string role = "User")
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserTickets_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        var tickets = new List<NextHappen.Ticket.Domain.Entities.Ticket>
        {
            new() { Id = Guid.NewGuid(), UserId = userId }
        };
        _serviceMock.Setup(s => s.GetByUserAsync(userId)).ReturnsAsync(tickets);

        var result = await _controller.GetUserTickets(userId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tickets, ok.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserTickets_ShouldReturnForbid_WhenNotSelf()
    {
        SetUser(Guid.NewGuid());

        var result = await _controller.GetUserTickets(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTicketDetail_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        var ticket = new NextHappen.Ticket.Domain.Entities.Ticket { Id = Guid.NewGuid(), UserId = userId };
        _serviceMock.Setup(m => m.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);

        var result = await _controller.GetTicketDetail(ticket.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(ticket, ok.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTicketDetail_ShouldReturnNotFound()
    {
        SetUser(Guid.NewGuid());
        _serviceMock.Setup(m => m.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((NextHappen.Ticket.Domain.Entities.Ticket?)null);

        var result = await _controller.GetTicketDetail(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Validate_ShouldReturnOk_WhenValidCode()
    {
        SetUser(Guid.NewGuid(), "Admin");
        var response = new ValidateTicketResponse { Valid = true, Message = "Ingreso permitido." };
        _serviceMock.Setup(m => m.ValidateAsync("TEST")).ReturnsAsync(response);

        var result = await _controller.Validate(new ValidateTicketRequest { QrCode = "TEST" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Refund_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        SetUser(userId, "Admin");
        _payments.ShouldSucceed = true;

        var result = await _controller.Refund(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok!.Value!.GetType().GetProperty("message")!.GetValue(ok.Value);
        Assert.Equal("Entrada reembolsada correctamente.", value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Refund_ShouldReturnForbidden_WhenUnauthorized()
    {
        SetUser(Guid.NewGuid());
        _payments.ThrowUnauthorized = true;

        var result = await _controller.Refund(Guid.NewGuid());

        var statusCode = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCode.StatusCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Refund_ShouldReturnBadRequest_WhenAlreadyRefunded()
    {
        var userId = Guid.NewGuid();
        SetUser(userId, "Admin");
        _payments.ThrowAlreadyRefunded = true;

        var result = await _controller.Refund(Guid.NewGuid());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }
}

/// <summary>Stub manual para PaymentService — usa override porque RefundTicketAsync es virtual.</summary>
internal class FakePaymentService : PaymentService
{
    public bool ShouldSucceed { get; set; }
    public bool ThrowUnauthorized { get; set; }
    public bool ThrowAlreadyRefunded { get; set; }

    public FakePaymentService()
        : base(Mock.Of<NextHappen.Ticket.Domain.Repositories.IOrderRepository>(),
              Mock.Of<NextHappen.Ticket.Domain.Repositories.ITicketRepository>(),
              Mock.Of<ITicketService>(),
              Mock.Of<NextHappen.Ticket.Infrastructure.Http.IEventCatalogClient>(),
              Mock.Of<MassTransit.IPublishEndpoint>(),
              Mock.Of<NextHappen.Ticket.Infrastructure.Payments.ISessionService>(),
              Mock.Of<NextHappen.Ticket.Infrastructure.Payments.IRefundService>(),
              Mock.Of<Microsoft.Extensions.Options.IOptions<NextHappen.Ticket.Infrastructure.Payments.StripeOptions>>(),
              Mock.Of<Microsoft.Extensions.Logging.ILogger<PaymentService>>()) { }

    public override Task RefundTicketAsync(Guid ticketId, Guid requesterId, bool isAdmin)
    {
        if (ThrowUnauthorized)
            throw new UnauthorizedAccessException("No puedes reembolsar una entrada que no es tuya.");
        if (ThrowAlreadyRefunded)
            throw new InvalidOperationException("La entrada ya fue reembolsada.");
        return Task.CompletedTask;
    }
}