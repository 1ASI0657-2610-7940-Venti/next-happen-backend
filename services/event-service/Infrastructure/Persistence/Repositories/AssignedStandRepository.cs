using Microsoft.EntityFrameworkCore;
using NextHappen.Event.Domain.Entities;
using NextHappen.Event.Domain.Repositories;

namespace NextHappen.Event.Infrastructure.Persistence.Repositories;

public class AssignedStandRepository : IAssignedStandRepository
{
    private readonly EventDbContext _context;

    public AssignedStandRepository(EventDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AssignedStand stand)
    {
        await _context.AssignedStands.AddAsync(stand);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AssignedStand>> GetByEventIdAsync(Guid eventId)
    {
        return await _context.AssignedStands
            .Where(x => x.EventId == eventId)
            .ToListAsync();
    }

    public async Task<AssignedStand?> GetByIdAsync(Guid id)
        => await _context.AssignedStands.FindAsync(id);

    public async Task UpdateAsync(AssignedStand stand)
    {
        _context.AssignedStands.Update(stand);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var stand = await _context.AssignedStands.FindAsync(id);
        if (stand != null)
        {
            _context.AssignedStands.Remove(stand);
            await _context.SaveChangesAsync();
        }
    }
}
