using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.Tickets.Application.DTOs;
using nexthappen_backend.Tickets.Application.Services;
using nexthappen_backend.Tickets.Application.UseCases;

namespace nexthappen_backend.Tickets.API.Controllers;

[ApiController]
public class TicketsController : ControllerBase
{
    private readonly PurchaseTicketHandler _purchaseHandler;
    private readonly GetUserTicketsHandler _listHandler;
    private readonly GetTicketByIdHandler _detailHandler;
    private readonly TicketsService _service;

    public TicketsController(
        PurchaseTicketHandler purchaseHandler,
        GetUserTicketsHandler listHandler,
        GetTicketByIdHandler detailHandler,
        TicketsService service)
    {
        _purchaseHandler = purchaseHandler;
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _service = service;
    }

    [HttpPost("api/events/{eventId:guid}/tickets/purchase")]
    public async Task<IActionResult> Purchase(Guid eventId, [FromQuery] Guid userId, [FromQuery] int quantity)
    {
        var result = await _purchaseHandler.Handle(eventId, userId, quantity);
        return Ok(result);
    }



    [HttpGet("api/users/{userId:guid}/tickets")]
    public async Task<IActionResult> GetUserTickets(Guid userId)
    {
        var tickets = await _listHandler.Handle(userId);
        return Ok(tickets);
    }

    [HttpGet("api/tickets/{ticketId:guid}")]
    public async Task<IActionResult> GetTicketDetail(Guid ticketId)
    {
        var ticket = await _detailHandler.Handle(ticketId);
        return ticket != null ? Ok(ticket) : NotFound();
    }

    [HttpDelete("api/tickets/{ticketId:guid}")]
    public async Task<IActionResult> CancelTicket(Guid ticketId)
    {
        var result = await _service.CancelTicketAsync(ticketId);
        return result ? Ok() : NotFound();
    }
}