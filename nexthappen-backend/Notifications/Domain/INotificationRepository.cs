using nexthappen_backend.Notifications.Domain.Entities;

namespace nexthappen_backend.Notifications.Domain;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<List<Notification>> GetByUserIdAsync(Guid userId);
    Task MarkAsReadAsync(int id);
}