using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.EventDiscovery.Application.UseCases;

namespace nexthappen_backend.EventDiscovery.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventDiscoveryController : ControllerBase
{
    private readonly GetPublicEventsHandler _handler;

    public EventDiscoveryController(GetPublicEventsHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicEvents()
    {
        var result = await _handler.Handle();
        return Ok(result);
    }
}