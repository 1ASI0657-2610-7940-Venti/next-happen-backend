using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.SavedEvents.Application.Services;
using nexthappen_backend.SavedEvents.Application.UseCases;

[ApiController]
[Route("api/users/{userId:guid}/saved-events")]
public class SavedEventsController : ControllerBase
{
    private readonly SavedEventsService _service;
    private readonly GetSavedEventsHandler _handler;

    public SavedEventsController(SavedEventsService service, GetSavedEventsHandler handler)
    {
        _service = service;
        _handler = handler;
    }

    [HttpPost("{eventId:guid}")]
    public async Task<IActionResult> SaveEvent(Guid userId, Guid eventId)
    {
        var success = await _service.SaveEventAsync(userId, eventId);
        return success ? Ok() : Conflict("Event already saved.");
    }

    [HttpDelete("{eventId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid eventId)
    {
        var success = await _service.RemoveSavedEventAsync(userId, eventId);
        return success ? Ok() : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId)
    {
        var events = await _handler.Handle(userId);
        return Ok(events);
    }
}