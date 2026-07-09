namespace NextHappen.Ticket.Infrastructure.Payments;

/// <summary>
/// Configuración de Stripe. Se enlaza desde la sección "Stripe" de la configuración
/// (variables de entorno Stripe__SecretKey, Stripe__WebhookSecret, etc.).
/// Los valores reales NUNCA se versionan; se inyectan por entorno.
/// </summary>
public class StripeOptions
{
    /// <summary>Clave secreta de la API (sk_test_... en modo prueba).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Secreto para verificar la firma de los webhooks (whsec_...).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Moneda ISO en minúsculas (Stripe la exige así). Perú: "pen".</summary>
    public string Currency { get; set; } = "pen";

    /// <summary>URL base del frontend para construir success/cancel URLs.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
