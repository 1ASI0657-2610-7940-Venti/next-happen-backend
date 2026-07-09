using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Infrastructure.Payments;
using Stripe;

namespace NextHappen.Ticket.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _payments;
    private readonly StripeOptions _stripe;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentService payments, IOptions<StripeOptions> stripe, ILogger<PaymentController> logger)
    {
        _payments = payments;
        _stripe = stripe.Value;
        _logger = logger;
    }

    /// <summary>
    /// Inicia el checkout: reserva cupos, crea el pedido y una sesión de Stripe.
    /// Devuelve la URL a la que el frontend debe redirigir al usuario.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(new { error = "Sesión inválida." });

        try
        {
            var result = await _payments.CreateCheckoutSessionAsync(userId, request.EventId, request.Quantity);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[Stripe] Error creando la sesión de checkout");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "No se pudo iniciar el pago. Intenta de nuevo." });
        }
    }

    /// <summary>
    /// Confirma el pago al volver de Stripe (respaldo del webhook). Consulta la sesión
    /// en Stripe y emite las entradas si el cobro se completó. Idempotente.
    /// </summary>
    [HttpGet("confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm([FromQuery(Name = "session_id")] string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { error = "session_id es requerido." });

        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(new { error = "Sesión inválida." });

        try
        {
            var result = await _payments.ConfirmSessionAsync(sessionId, userId, User.IsInRole("Admin"));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[Stripe] Error confirmando la sesión {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "No se pudo confirmar el pago." });
        }
    }

    /// <summary>
    /// Endpoint público que recibe los webhooks de Stripe. La autenticidad se verifica
    /// mediante la firma (Stripe-Signature), no con JWT.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _stripe.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "[Stripe] Firma de webhook inválida");
            return BadRequest();
        }

        try
        {
            await _payments.HandleStripeEventAsync(stripeEvent);
        }
        catch (Exception ex)
        {
            // Devolver 500 hace que Stripe reintente el envío del webhook.
            _logger.LogError(ex, "[Stripe] Error procesando el webhook {Type}", stripeEvent.Type);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }

    private Guid GetUserId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }
}
