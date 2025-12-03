using Microsoft.EntityFrameworkCore;
using nexthappen_backend.SavedEvents.Domain;
using nexthappen_backend.SavedEvents.Domain.Entities;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;


namespace nexthappen_backend.SavedEvents.Infrastructure.Persistence.Repositories;

public class SavedEventRepository : ISavedEventRepository
{
    private readonly AppDbContext _context;

    public SavedEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SavedEvent savedEvent)
    {
        await _context.Set<SavedEvent>().AddAsync(savedEvent);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid userId, Guid eventId)
    {
        var entity = await _context.Set<SavedEvent>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == eventId);

        if (entity != null)
        {
            _context.Set<SavedEvent>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<SavedEvent>> GetSavedEventsAsync(Guid userId)
    {
        return await _context.Set<SavedEvent>()
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public Task<bool> ExistsAsync(Guid userId, Guid eventId)
    {
        return _context.Set<SavedEvent>()
            .AnyAsync(x => x.UserId == userId && x.EventId == eventId);
    }
}
