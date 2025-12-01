using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.ManageEvent.Application.UseCases;

namespace nexthappen_backend.ManageEvent.API.Controllers;

[ApiController]
[Route("api/manage/events")]
public class ManageEventController : ControllerBase
{
    private readonly GetAllEventsHandler _getAllHandler;
    private readonly UpdateEventHandler _updateHandler;
    private readonly DeleteEventHandler _deleteHandler;

    public ManageEventController(GetAllEventsHandler getAllHandler, UpdateEventHandler updateHandler, DeleteEventHandler deleteHandler)
    {
        _getAllHandler = getAllHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _getAllHandler.HandleAsync();
        return Ok(events);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var result = await _updateHandler.HandleAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted = await _deleteHandler.HandleAsync(id);
        return deleted ? NoContent() : NotFound();
    }



}