using Microsoft.EntityFrameworkCore;
using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;

namespace NextHappen.Engagement.Infrastructure.Persistence.Repositories;

public class SavedEventRepository : ISavedEventRepository
{
    private readonly EngagementDbContext _context;

    public SavedEventRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SavedEvent savedEvent)
    {
        await _context.SavedEvents.AddAsync(savedEvent);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid userId, Guid eventId)
    {
        var entity = await _context.SavedEvents
            .FirstOrDefaultAsync(s => s.UserId == userId && s.EventId == eventId);
        if (entity != null)
        {
            _context.SavedEvents.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid eventId)
        => await _context.SavedEvents.AnyAsync(s => s.UserId == userId && s.EventId == eventId);

    public async Task<IEnumerable<SavedEvent>> GetByUserIdAsync(Guid userId)
        => await _context.SavedEvents.Where(s => s.UserId == userId).ToListAsync();
}

public class MetricRepository : IMetricRepository
{
    private readonly EngagementDbContext _context;

    public MetricRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Metric metric)
    {
        await _context.Metrics.AddAsync(metric);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Metric>> GetAllAsync()
        => await _context.Metrics.ToListAsync();
}
