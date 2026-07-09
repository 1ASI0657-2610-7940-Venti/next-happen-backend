namespace NextHappen.Ticket.Domain.Entities;

/// <summary>
/// Estados del pedido según el ciclo de vida del pago en Stripe.
/// </summary>
public static class OrderStatus
{
    public const string Pending = "Pending";   // Sesión de checkout creada, esperando pago
    public const string Paid = "Paid";         // Pago confirmado por webhook → entradas emitidas
    public const string Failed = "Failed";     // Pago fallido o sesión expirada → cupos liberados
    public const string Refunded = "Refunded"; // Todas las entradas del pedido fueron reembolsadas
}

/// <summary>
/// Representa un intento de compra. Se crea en estado Pending al iniciar el checkout
/// de Stripe y solo pasa a Paid (emitiendo las entradas) cuando el webhook confirma el pago.
/// Actúa como registro de auditoría y de idempotencia frente a reintentos del webhook.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "pen";

    /// <summary>Id de la sesión de Stripe Checkout (cs_...).</summary>
    public string StripeSessionId { get; set; } = string.Empty;

    /// <summary>Id del PaymentIntent (pi_...); necesario para emitir reembolsos.</summary>
    public string? StripePaymentIntentId { get; set; }

    public string Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
