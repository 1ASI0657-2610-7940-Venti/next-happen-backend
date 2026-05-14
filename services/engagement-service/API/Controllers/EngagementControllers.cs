using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextHappen.Engagement.Application.Services;
using NextHappen.Engagement.Domain.Entities;
using System.Security.Claims;

namespace NextHappen.Engagement.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/saved-events")]
[Authorize]
public class SavedEventsController : ControllerBase
{
    private readonly SavedEventService _service;

    public SavedEventsController(SavedEventService service)
    {
        _service = service;
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var userId);
        return userId;
    }

    [HttpPost("{eventId:guid}")]
    public async Task<IActionResult> SaveEvent(Guid userId, Guid eventId)
    {
        if (userId != GetAuthenticatedUserId()) return Forbid();

        var success = await _service.SaveEventAsync(userId, eventId);
        return success ? Ok() : Conflict("Event already saved.");
    }

    [HttpDelete("{eventId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid eventId)
    {
        if (userId != GetAuthenticatedUserId()) return Forbid();

        var success = await _service.RemoveAsync(userId, eventId);
        return success ? Ok() : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId)
    {
        if (userId != GetAuthenticatedUserId()) return Forbid();

        var events = await _service.GetByUserAsync(userId);
        return Ok(events);
    }
}

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly MetricService _service;

    public MetricsController(MetricService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] Metric body)
    {
        await _service.RegisterAsync(body.EventId, body.Action);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost("event-view/{eventId:guid}")]
    public async Task<IActionResult> RegisterEventView(Guid eventId)
    {
        await _service.RegisterAsync(eventId, "view-event");
        return Ok();
    }
}
