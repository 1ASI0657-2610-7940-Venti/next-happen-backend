using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextHappen.Event.Application.DTOs;
using NextHappen.Event.Application.Services;

namespace NextHappen.Event.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly EventService _service;

    public EventController(EventService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var ev = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ToResponse(ev));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ev = await _service.GetByIdAsync(id);
        return ev is null ? NotFound() : Ok(ToResponse(ev));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _service.GetAllAsync();
        return Ok(events.Select(ToResponse));
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicEvents()
    {
        var events = await _service.GetPublicAsync();
        return Ok(events.Select(ToResponse));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(ToResponse(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ── Helper ──
    private static EventResponse ToResponse(Domain.Entities.Event ev) => new()
    {
        Id = ev.Id,
        Organizer = ev.Organizer,
        Title = ev.Title,
        Description = ev.Description,
        Price = ev.Price,
        Quantity = ev.Quantity,
        Category = ev.Category,
        Address = ev.Address,
        Location = ev.Location,
        Photos = ev.Photos,
        StartDate = ev.DateRange.StartDate,
        EndDate = ev.DateRange.EndDate
    };
}
