using Microsoft.AspNetCore.Mvc;
using NextHappen.Notification.Domain.Entities;
using NextHappen.Notification.Domain.Repositories;

namespace NextHappen.Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationRepository _repo;

    public NotificationController(INotificationRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        return Ok(await _repo.GetByUserIdAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        var notif = new Domain.Entities.Notification
        {
            UserId = request.UserId,
            EventId = request.EventId,
            Message = request.Message,
            Timestamp = DateTime.UtcNow
        };
        await _repo.AddAsync(notif);
        return Ok(notif);
    }

    [HttpPost("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        await _repo.MarkAsReadAsync(notificationId);
        return Ok();
    }
}

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public string Message { get; set; } = string.Empty;
}
