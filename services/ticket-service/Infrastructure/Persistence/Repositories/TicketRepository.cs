using Microsoft.EntityFrameworkCore;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;

namespace NextHappen.Ticket.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly TicketDbContext _context;

    public TicketRepository(TicketDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Domain.Entities.Ticket ticket)
    {
        await _context.Tickets.AddAsync(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Domain.Entities.Ticket> tickets)
    {
        await _context.Tickets.AddRangeAsync(tickets);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Domain.Entities.Ticket>> GetByUserIdAsync(Guid userId)
        => await _context.Tickets.Where(t => t.UserId == userId)
            .OrderByDescending(t => t.PurchaseDate).ToListAsync();

    public async Task<Domain.Entities.Ticket?> GetByIdAsync(Guid id)
        => await _context.Tickets.FindAsync(id);

    public async Task<Domain.Entities.Ticket?> GetByQrCodeAsync(string qrCode)
        => await _context.Tickets.FirstOrDefaultAsync(t => t.QrCode == qrCode);

    public async Task<Domain.Entities.Ticket?> GetByShortCodeAsync(string shortCode)
        => await _context.Tickets.FirstOrDefaultAsync(t => t.ShortCode == shortCode);

    public async Task<bool> ShortCodeExistsAsync(string shortCode)
        => await _context.Tickets.AnyAsync(t => t.ShortCode == shortCode);

    public async Task<List<Domain.Entities.Ticket>> GetByEventIdAsync(Guid eventId)
        => await _context.Tickets.Where(t => t.EventId == eventId).ToListAsync();

    public async Task<List<Domain.Entities.Ticket>> GetByEventIdsAsync(IEnumerable<Guid> eventIds)
    {
        var ids = eventIds.ToList();
        return await _context.Tickets.Where(t => ids.Contains(t.EventId)).ToListAsync();
    }

    public async Task UpdateAsync(Domain.Entities.Ticket ticket)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CancelAsync(Guid ticketId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;

        ticket.Status = TicketStatus.Cancelled;
        await _context.SaveChangesAsync();
        return true;
    }
}
