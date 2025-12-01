using Microsoft.EntityFrameworkCore;
using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace nexthappen_backend.CreateEvent.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Event entity)
    {
        _context.Set<Event>().Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
        => await _context.Set<Event>().ToListAsync();

    public async Task<Event?> GetByIdAsync(Guid id)
        => await _context.Set<Event>().FindAsync(id);

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync(); 
    }
    
    public async Task UpdateAsync(Event ev)
    {
        _context.Events.Update(ev);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteByIdAsync(Guid id)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM Events WHERE Id = {0}", id);
    }
    
    public async Task<IEnumerable<Event>> GetPublicEventsAsync()
    {
        return await _context.Events
            .Where(e => e.IsPublic == true)
            .OrderBy(e => e.DateRange.StartDate)
            .ToListAsync();
    }
}