using Microsoft.EntityFrameworkCore;
using nexthappen_backend.Notifications.Domain;
using nexthappen_backend.Notifications.Domain.Entities;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace nexthappen_backend.Notifications.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Set<Notification>().AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Set<Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Timestamp)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var notif = await _context.Set<Notification>().FindAsync(id);
        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}