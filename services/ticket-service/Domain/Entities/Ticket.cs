namespace NextHappen.Ticket.Domain.Entities;

/// <summary>
/// Estados posibles del ciclo de vida de una entrada.
/// </summary>
public static class TicketStatus
{
    public const string Active = "Active";       // Pagada y válida para ingresar
    public const string Used = "Used";           // Validada en la puerta (QR escaneado)
    public const string Refunded = "Refunded";   // Reembolsada vía Stripe
    public const string Cancelled = "Cancelled"; // Cancelada manualmente
}

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }

    /// <summary>Pedido (Order) de Stripe al que pertenece esta entrada.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Precio unitario pagado (snapshot al momento de la compra).</summary>
    public decimal Price { get; set; }

    /// <summary>Token único e imposible de adivinar que se codifica en el QR.</summary>
    public string QrCode { get; set; } = string.Empty;

    /// <summary>
    /// Código corto legible (6 caracteres, alfabeto sin ambigüedades) para validar
    /// la entrada a mano desde una computadora, sin escanear el QR.
    /// </summary>
    public string ShortCode { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    /// <summary>Fecha en que se validó la entrada en la puerta (si aplica).</summary>
    public DateTime? ValidatedAt { get; set; }

    public string Status { get; set; } = TicketStatus.Active;
}
