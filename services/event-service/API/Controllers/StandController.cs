using Microsoft.AspNetCore.Mvc;
using NextHappen.Event.Application.Services;
using NextHappen.Event.Domain.Entities;

namespace NextHappen.Event.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/stands")]
public class StandController : ControllerBase
{
    private readonly StandService _service;

    public StandController(StandService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssigned(Guid eventId)
    {
        return Ok(await _service.GetByEventAsync(eventId));
    }

    [HttpPost]
    public async Task<IActionResult> Assign(Guid eventId, [FromBody] AssignedStand body)
    {
        var result = await _service.AssignAsync(eventId, body.Name, body.Category);
        return Ok(result);
    }
}

[ApiController]
[Route("api/stands")]
public class StandEditController : ControllerBase
{
    private readonly StandService _service;

    public StandEditController(StandService service)
    {
        _service = service;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AssignedStand body)
    {
        var updated = await _service.UpdateAsync(id, body.Name, body.Category);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? Ok() : NotFound();
    }
}
