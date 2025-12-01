using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Application.UseCases;

namespace nexthappen_backend.CreateEvent.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly CreateEventHandler _create;
    private readonly GetEventByIdHandler _byId;
    private readonly GetAllEventsHandler _all;

    public EventController(
        CreateEventHandler create,
        GetEventByIdHandler byId,
        GetAllEventsHandler all)
    {
        _create = create;
        _byId = byId;
        _all = all;
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
}