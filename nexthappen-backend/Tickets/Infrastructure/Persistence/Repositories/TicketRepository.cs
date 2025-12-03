using Microsoft.EntityFrameworkCore;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;
using nexthappen_backend.Tickets.Domain.Entities;

namespace nexthappen_backend.Tickets.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> AddAsync(Ticket ticket)
    {
        await _context.Tickets.AddAsync(ticket);
        await _context.SaveChangesAsync();
        return ticket.Id;
    }

    public async Task<List<Ticket>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Tickets
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    public async Task<Ticket?> GetByIdAsync(Guid ticketId)
    {
        return await _context.Tickets.FindAsync(ticketId);
    }

    public async Task<bool> CancelAsync(Guid ticketId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;

        ticket.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return true;
    }
}