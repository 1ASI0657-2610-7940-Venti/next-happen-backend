using MassTransit;
using Microsoft.Extensions.Options;
using NextHappen.Contracts.Events;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;
using NextHappen.Ticket.Infrastructure.Http;
using NextHappen.Ticket.Infrastructure.Payments;
using Stripe;
using Stripe.Checkout;

namespace NextHappen.Ticket.Application.Services;

/// <summary>
/// Orquesta el cobro con Stripe. Las entradas SOLO se emiten cuando Stripe confirma
/// el pago vía webhook (checkout.session.completed); nunca antes. También gestiona
/// la liberación de cupos ante expiración/fallo y los reembolsos.
/// </summary>
public class PaymentService
{
    private readonly IOrderRepository _orderRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketService _ticketService;
    private readonly IEventCatalogClient _events;
    private readonly IPublishEndpoint _publish;
    private readonly ISessionService _sessionService;
    private readonly IRefundService _refundService;
    private readonly StripeOptions _stripe;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orderRepo,
        ITicketRepository ticketRepo,
        ITicketService ticketService,
        IEventCatalogClient events,
        IPublishEndpoint publish,
        ISessionService sessionService,
        IRefundService refundService,
        IOptions<StripeOptions> stripe,
        ILogger<PaymentService> logger)
    {
        _orderRepo = orderRepo;
        _ticketRepo = ticketRepo;
        _ticketService = ticketService;
        _events = events;
        _publish = publish;
        _sessionService = sessionService;
        _refundService = refundService;
        _stripe = stripe.Value;
        _logger = logger;
    }

    /// <summary>
    /// Reserva los cupos, crea un pedido Pending y una sesión de Stripe Checkout.
    /// Devuelve la URL a la que redirigir al usuario para pagar.
    /// </summary>
    public async Task<CheckoutResponse> CreateCheckoutSessionAsync(Guid userId, Guid eventId, int quantity)
    {
        if (quantity < 1) throw new ArgumentException("La cantidad debe ser al menos 1.");

        var ev = await _events.GetEventAsync(eventId)
            ?? throw new InvalidOperationException("El evento no existe o no está disponible.");

        var unitPrice = ev.Price ?? 0m;
        if (unitPrice <= 0)
            throw new InvalidOperationException("Este evento no tiene un precio válido para la venta.");

        // 1) Reservar cupos (bloqueo pesimista). Se liberan si el pago expira.
        if (!await _events.ReserveSeatsAsync(eventId, quantity))
            throw new InvalidOperationException("No hay suficientes cupos disponibles.");

        // 2) Persistir el pedido en estado Pending.
        var order = new Order
        {
            UserId = userId,
            EventId = eventId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalAmount = unitPrice * quantity,
            Currency = _stripe.Currency,
            Status = OrderStatus.Pending
        };

        // 3) Crear la sesión de Stripe Checkout.
        try
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                ClientReferenceId = order.Id.ToString(),
                SuccessUrl = $"{_stripe.FrontendBaseUrl.TrimEnd('/')}/user/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_stripe.FrontendBaseUrl.TrimEnd('/')}/user/checkout/cancel?order_id={order.Id}",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = quantity,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _stripe.Currency,
                            UnitAmount = ToMinorUnits(unitPrice),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = ev.Title ?? "Entrada NextHappen"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString(),
                    ["eventId"] = eventId.ToString(),
                    ["userId"] = userId.ToString()
                }
            };

            var session = await _sessionService.CreateAsync(options);
            order.StripeSessionId = session.Id;
            await _orderRepo.AddAsync(order);

            return new CheckoutResponse { OrderId = order.Id, CheckoutUrl = session.Url };
        }
        catch (StripeException)
        {
            // Si Stripe falla, no dejamos los cupos bloqueados.
            await _events.ReleaseSeatsAsync(eventId, quantity);
            throw;
        }
    }

    /// <summary>
    /// Procesa un webhook de Stripe (firma ya verificada por el controlador).
    /// Es idempotente: reintentos del mismo evento no duplican entradas.
    /// </summary>
    public async Task HandleStripeEventAsync(Stripe.Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await OnCheckoutCompletedAsync((Session)stripeEvent.Data.Object);
                break;

            case "checkout.session.expired":
                await OnCheckoutFailedAsync((Session)stripeEvent.Data.Object);
                break;

            default:
                _logger.LogInformation("[Stripe] Evento no manejado: {Type}", stripeEvent.Type);
                break;
        }
    }

    private async Task OnCheckoutCompletedAsync(Session session)
    {
        var order = await _orderRepo.GetBySessionIdAsync(session.Id);
        if (order is null)
        {
            _logger.LogWarning("[Stripe] Pago recibido para una sesión desconocida {SessionId}", session.Id);
            return;
        }
        await MarkOrderPaidAndIssueTicketsAsync(order, session.PaymentIntentId);
    }

    /// <summary>
    /// Confirma el pago consultando la sesión directamente en Stripe y emite las entradas
    /// si el cobro se completó. Sirve de respaldo a los webhooks (p. ej. al volver de Stripe
    /// a la página de éxito), de modo que la compra funcione aunque el webhook no llegue.
    /// Es idempotente: no duplica entradas si el webhook ya las emitió.
    /// </summary>
    public async Task<ConfirmResult> ConfirmSessionAsync(string sessionId, Guid requesterId, bool isAdmin)
    {
        var order = await _orderRepo.GetBySessionIdAsync(sessionId)
            ?? throw new InvalidOperationException("No se encontró el pedido de esta sesión.");

        if (!isAdmin && order.UserId != requesterId)
            throw new UnauthorizedAccessException("Este pago no te pertenece.");

        if (order.Status != OrderStatus.Paid)
        {
            var session = await _sessionService.GetAsync(sessionId);
            if (session.PaymentStatus == "paid")
                await MarkOrderPaidAndIssueTicketsAsync(order, session.PaymentIntentId);
        }

        return new ConfirmResult { Status = order.Status, Quantity = order.Quantity };
    }

    /// <summary>Marca el pedido como pagado y emite las entradas. Idempotente.</summary>
    private async Task MarkOrderPaidAndIssueTicketsAsync(Order order, string? paymentIntentId)
    {
        if (order.Status == OrderStatus.Paid) return;

        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;
        order.StripePaymentIntentId = paymentIntentId;
        await _orderRepo.UpdateAsync(order);

        var tickets = await _ticketService.IssueTicketsForOrderAsync(order);
        _logger.LogInformation("[Stripe] Pedido {OrderId} pagado; {Count} entradas emitidas", order.Id, tickets.Count);

        // Notificar la compra (una notificación por pedido).
        var first = tickets.FirstOrDefault();
        if (first is not null)
        {
            await _publish.Publish(new TicketPurchasedEvent(
                first.Id, order.EventId, order.UserId, order.TotalAmount, order.PaidAt!.Value));
        }
    }

    private async Task OnCheckoutFailedAsync(Session session)
    {
        var order = await _orderRepo.GetBySessionIdAsync(session.Id);
        if (order is null || order.Status != OrderStatus.Pending) return;

        order.Status = OrderStatus.Failed;
        await _orderRepo.UpdateAsync(order);

        // Devolver los cupos reservados al inventario.
        await _events.ReleaseSeatsAsync(order.EventId, order.Quantity);
        _logger.LogInformation("[Stripe] Sesión {SessionId} expirada; {Qty} cupos liberados", session.Id, order.Quantity);
    }

    /// <summary>
    /// Reembolsa una entrada individual vía Stripe, la marca como Refunded y libera su cupo.
    /// </summary>
    public async Task RefundTicketAsync(Guid ticketId, Guid requesterId, bool isAdmin)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId)
            ?? throw new InvalidOperationException("Entrada no encontrada.");

        if (!isAdmin && ticket.UserId != requesterId)
            throw new UnauthorizedAccessException("No puedes reembolsar una entrada que no es tuya.");

        if (ticket.Status == TicketStatus.Refunded)
            throw new InvalidOperationException("La entrada ya fue reembolsada.");
        if (ticket.Status == TicketStatus.Used)
            throw new InvalidOperationException("No se puede reembolsar una entrada ya utilizada.");

        var order = await _orderRepo.GetByIdAsync(ticket.OrderId)
            ?? throw new InvalidOperationException("Pedido asociado no encontrado.");
        if (string.IsNullOrEmpty(order.StripePaymentIntentId))
            throw new InvalidOperationException("El pago no puede reembolsarse (sin PaymentIntent).");

        // Reembolso parcial: el importe de una sola entrada.
        await _refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = order.StripePaymentIntentId,
            Amount = ToMinorUnits(ticket.Price)
        });

        ticket.Status = TicketStatus.Refunded;
        await _ticketRepo.UpdateAsync(ticket);

        await _events.ReleaseSeatsAsync(ticket.EventId, 1);
        _logger.LogInformation("[Stripe] Entrada {TicketId} reembolsada por {Amount} {Currency}",
            ticket.Id, ticket.Price, order.Currency);
    }

    /// <summary>Convierte un importe decimal a la unidad mínima que exige Stripe (céntimos).</summary>
    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
