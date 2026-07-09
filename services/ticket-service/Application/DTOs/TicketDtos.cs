namespace NextHappen.Ticket.Application.DTOs;

/// <summary>Solicitud para iniciar el checkout de Stripe.</summary>
public class CheckoutRequest
{
    public Guid EventId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>Respuesta del checkout: URL de Stripe a la que redirigir al usuario.</summary>
public class CheckoutResponse
{
    public Guid OrderId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
}

/// <summary>Resultado de confirmar una sesión de pago al volver de Stripe.</summary>
public class ConfirmResult
{
    public string Status { get; set; } = string.Empty; // Pending | Paid | Failed
    public int Quantity { get; set; }
    public bool Paid => Status == "Paid";
}

/// <summary>Solicitud de validación de una entrada en la puerta.</summary>
public class ValidateTicketRequest
{
    public string QrCode { get; set; } = string.Empty;
}

/// <summary>Resultado de validar una entrada.</summary>
public class ValidateTicketResponse
{
    public bool Valid { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? TicketId { get; set; }
    public Guid? EventId { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Fila de la lista de asistentes de un evento (panel del organizador).
/// No incluye el token del QR: la validación se hace con el código corto.
/// </summary>
public class EventTicketRow
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? ValidatedAt { get; set; }
}

/// <summary>Resumen de ventas de un evento para el panel del organizador.</summary>
public class SalesSummary
{
    public Guid EventId { get; set; }
    public int TicketsSold { get; set; }
    public int TicketsValidated { get; set; }
    public int TicketsRefunded { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal NetRevenue { get; set; }
    public List<DailySales> ByDay { get; set; } = new();
}

public class DailySales
{
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd
    public int Tickets { get; set; }
    public decimal Revenue { get; set; }
}
