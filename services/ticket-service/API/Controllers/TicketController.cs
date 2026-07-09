using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Application.Services;
using QRCoder;

namespace NextHappen.Ticket.API.Controllers;

[ApiController]
public class TicketController : ControllerBase
{
    private readonly ITicketService _service;
    private readonly PaymentService _payments;

    public TicketController(ITicketService service, PaymentService payments)
    {
        _service = service;
        _payments = payments;
    }

    [HttpGet("api/users/{userId:guid}/tickets")]
    [Authorize]
    public async Task<IActionResult> GetUserTickets(Guid userId)
    {
        if (!IsSelfOrAdmin(userId)) return Forbid();
        var tickets = await _service.GetByUserAsync(userId);
        return Ok(tickets);
    }

    [HttpGet("api/tickets/{ticketId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetTicketDetail(Guid ticketId)
    {
        var ticket = await _service.GetByIdAsync(ticketId);
        if (ticket is null) return NotFound();
        if (!IsSelfOrAdmin(ticket.UserId)) return Forbid();
        return Ok(ticket);
    }

    /// <summary>Devuelve el QR de la entrada como imagen PNG.</summary>
    [HttpGet("api/tickets/{ticketId:guid}/qr")]
    [Authorize]
    public async Task<IActionResult> GetTicketQr(Guid ticketId)
    {
        var ticket = await _service.GetByIdAsync(ticketId);
        if (ticket is null) return NotFound();
        if (!IsSelfOrAdmin(ticket.UserId)) return Forbid();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(ticket.QrCode, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(10);
        return File(png, "image/png");
    }

    /// <summary>Valida una entrada por su código QR o código corto (organizador en la puerta).</summary>
    [HttpPost("api/tickets/validate")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequest request)
    {
        var result = await _service.ValidateAsync(request.QrCode);
        return Ok(result);
    }

    /// <summary>Lista de asistentes (entradas) de un evento, para validar con un clic.</summary>
    [HttpGet("api/events/{eventId:guid}/tickets")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> GetEventTickets(Guid eventId)
    {
        var rows = await _service.GetEventTicketsAsync(eventId);
        return Ok(rows);
    }

    /// <summary>Reembolsa una entrada vía Stripe y libera su cupo.</summary>
    [HttpPost("api/tickets/{ticketId:guid}/refund")]
    [Authorize]
    public async Task<IActionResult> Refund(Guid ticketId)
    {
        try
        {
            await _payments.RefundTicketAsync(ticketId, GetUserId(), IsAdmin());
            return Ok(new { message = "Entrada reembolsada correctamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Helpers de identidad ──
    private Guid GetUserId()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private bool IsSelfOrAdmin(Guid ownerId) => IsAdmin() || GetUserId() == ownerId;
}
