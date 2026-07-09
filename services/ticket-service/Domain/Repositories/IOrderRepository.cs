using NextHappen.Ticket.Domain.Entities;

namespace NextHappen.Ticket.Domain.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetBySessionIdAsync(string stripeSessionId);
    Task UpdateAsync(Order order);
}
