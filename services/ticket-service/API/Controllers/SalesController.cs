using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextHappen.Ticket.Application.Services;

namespace NextHappen.Ticket.API.Controllers;

/// <summary>
/// Métricas de ventas para el panel del organizador. Los datos provienen de las
/// entradas realmente pagadas (no de reservas ni intentos de pago fallidos).
/// </summary>
[ApiController]
[Authorize(Roles = "Organizer,Admin")]
public class SalesController : ControllerBase
{
    private readonly SalesService _sales;

    public SalesController(SalesService sales)
    {
        _sales = sales;
    }

    /// <summary>Resumen de ventas de un evento.</summary>
    [HttpGet("api/events/{eventId:guid}/sales")]
    public async Task<IActionResult> GetForEvent(Guid eventId)
        => Ok(await _sales.GetForEventAsync(eventId));

    /// <summary>Resumen de ventas de varios eventos (para agregarlos en el dashboard).</summary>
    [HttpPost("api/sales/summary")]
    public async Task<IActionResult> GetForEvents([FromBody] SalesSummaryRequest request)
    {
        if (request.EventIds is null || request.EventIds.Count == 0)
            return Ok(Array.Empty<object>());
        return Ok(await _sales.GetForEventsAsync(request.EventIds));
    }

    public class SalesSummaryRequest
    {
        public List<Guid> EventIds { get; set; } = new();
    }
}
