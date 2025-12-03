using nexthappen_backend.Notifications.Domain;
using nexthappen_backend.Notifications.Domain.Entities;
using nexthappen_backend.IAM.Domain.Repositories;
using nexthappen_backend.CreateEvent.Domain.Entities;

namespace nexthappen_backend.Notifications.Application.Services;

public class NotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IEventRepository _eventRepo;
    private readonly IUserRepository _userRepo;

    public NotificationService(
        INotificationRepository repo,
        IEventRepository eventRepo,
        IUserRepository userRepo)
    {
        _repo = repo;
        _eventRepo = eventRepo;
        _userRepo = userRepo;
    }

    public async Task NotifyOrganizerAsync(Guid eventId, string message)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId);
        if (ev == null) return;

        var organizer = await _userRepo.GetByFullNameAndRoleAsync(ev.Organizer, "Organizer");
        if (organizer == null) return;

        var notif = new Notification
        {
            UserId = organizer.Id,
            EventId = eventId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await _repo.AddAsync(notif);
    }
}