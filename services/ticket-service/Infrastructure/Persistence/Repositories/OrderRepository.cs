using Microsoft.EntityFrameworkCore;
using NextHappen.Ticket.Domain.Entities;
using NextHappen.Ticket.Domain.Repositories;

namespace NextHappen.Ticket.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly TicketDbContext _context;

    public OrderRepository(TicketDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
        => await _context.Orders.FindAsync(id);

    public async Task<Order?> GetBySessionIdAsync(string stripeSessionId)
        => await _context.Orders.FirstOrDefaultAsync(o => o.StripeSessionId == stripeSessionId);

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
}
