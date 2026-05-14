using Microsoft.AspNetCore.Mvc;
using NextHappen.Ticket.Application.Services;

namespace NextHappen.Ticket.API.Controllers;

[ApiController]
public class TicketController : ControllerBase
{
    private readonly TicketService _service;

    public TicketController(TicketService service)
    {
        _service = service;
    }

    [HttpPost("api/events/{eventId:guid}/tickets/purchase")]
    public async Task<IActionResult> Purchase(Guid eventId, [FromQuery] Guid userId, [FromQuery] int quantity)
    {
        try
        {
            var result = await _service.PurchaseAsync(eventId, userId, quantity);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("api/users/{userId:guid}/tickets")]
    public async Task<IActionResult> GetUserTickets(Guid userId)
    {
        var tickets = await _service.GetByUserAsync(userId);
        return Ok(tickets);
    }

    [HttpGet("api/tickets/{ticketId:guid}")]
    public async Task<IActionResult> GetTicketDetail(Guid ticketId)
    {
        var ticket = await _service.GetByIdAsync(ticketId);
        return ticket != null ? Ok(ticket) : NotFound();
    }

    [HttpDelete("api/tickets/{ticketId:guid}")]
    public async Task<IActionResult> CancelTicket(Guid ticketId)
    {
        var result = await _service.CancelAsync(ticketId);
        return result ? Ok() : NotFound();
    }
}
