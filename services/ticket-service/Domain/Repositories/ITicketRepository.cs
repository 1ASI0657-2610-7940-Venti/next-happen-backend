using NextHappen.Ticket.Domain.Entities;

namespace NextHappen.Ticket.Domain.Repositories;

public interface ITicketRepository
{
    Task AddAsync(Entities.Ticket ticket);
    Task<List<Entities.Ticket>> GetByUserIdAsync(Guid userId);
    Task<Entities.Ticket?> GetByIdAsync(Guid id);
    Task<bool> CancelAsync(Guid ticketId);
}
