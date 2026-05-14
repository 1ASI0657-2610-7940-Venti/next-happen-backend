using Microsoft.EntityFrameworkCore;
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

    public async Task<List<Domain.Entities.Ticket>> GetByUserIdAsync(Guid userId)
        => await _context.Tickets.Where(t => t.UserId == userId).ToListAsync();

    public async Task<Domain.Entities.Ticket?> GetByIdAsync(Guid id)
        => await _context.Tickets.FindAsync(id);

    public async Task<bool> CancelAsync(Guid ticketId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;

        ticket.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return true;
    }
}
