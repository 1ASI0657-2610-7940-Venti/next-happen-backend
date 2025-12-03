using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.Metrics.Domain.Entities;
using nexthappen_backend.Notifications.Application.Services;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly MetricsService _service;
    private readonly NotificationService _notificationService;

    public MetricsController(MetricsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] Metric body)
    {
        await _service.RegisterAsync(body.EventId, body.Action, body.Timestamp);
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
        await _service.RegisterAsync(eventId, "view-event", DateTime.UtcNow);
        
        // Notificación al organizador
        await _notificationService.NotifyOrganizerAsync(
            eventId,
            "Un usuario visitó tu evento."
        );
        
        return Ok();
    }
}