using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Application.UseCases;
using nexthappen_backend.ManageEvent.Application.UseCases;

namespace nexthappen_backend.CreateEvent.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly CreateEventHandler _create;
    private readonly GetEventByIdHandler _byId;
    private readonly nexthappen_backend.CreateEvent.Application.UseCases.GetAllEventsHandler _all;
    private readonly UpdateEventHandler _updateHandler;
    private readonly DeleteEventHandler _deleteHandler;

    public EventController(
        CreateEventHandler create,
        GetEventByIdHandler byId,
        nexthappen_backend.CreateEvent.Application.UseCases.GetAllEventsHandler all,
        UpdateEventHandler updateHandler,
        DeleteEventHandler deleteHandler)
    {
        _create = create;
        _byId = byId;
        _all = all;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var result = await _create.Handle(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ev = await _byId.Handle(id);
        return ev is null ? NotFound() : Ok(ev);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _all.Handle();
        return Ok(events);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var result = await _updateHandler.HandleAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted = await _deleteHandler.HandleAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}