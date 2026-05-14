using NextHappen.Notification.Domain.Entities;

namespace NextHappen.Notification.Domain.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Entities.Notification notification);
    Task<List<Entities.Notification>> GetByUserIdAsync(Guid userId);
    Task MarkAsReadAsync(int id);
}
