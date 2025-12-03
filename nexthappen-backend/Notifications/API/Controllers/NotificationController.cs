using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.Notifications.Domain;

namespace nexthappen_backend.Notifications.API.Controllers;

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

    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        await _repo.MarkAsReadAsync(notificationId);
        return Ok();
    }
}
