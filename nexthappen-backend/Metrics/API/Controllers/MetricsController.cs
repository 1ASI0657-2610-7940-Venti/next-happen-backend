using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.Metrics.Domain.Entities;

[ApiController]
[Route("metrics")]
public class MetricsController : ControllerBase
{
    private readonly MetricsService _service;

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
}