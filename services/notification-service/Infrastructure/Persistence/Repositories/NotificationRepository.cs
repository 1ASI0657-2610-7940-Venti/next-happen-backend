using Microsoft.EntityFrameworkCore;
using NextHappen.Notification.Domain.Repositories;

namespace NextHappen.Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Domain.Entities.Notification notification)
    {
        await _context.Set<Domain.Entities.Notification>().AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Domain.Entities.Notification>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Set<Domain.Entities.Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Timestamp)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var notif = await _context.Set<Domain.Entities.Notification>().FindAsync(id);
        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}
