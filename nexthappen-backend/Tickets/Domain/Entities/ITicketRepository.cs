namespace nexthappen_backend.Tickets.Domain.Entities;

public interface ITicketRepository
{
    Task<Guid> AddAsync(Ticket ticket);
    Task<List<Ticket>> GetByUserIdAsync(Guid userId);
    Task<Ticket?> GetByIdAsync(Guid ticketId);
    Task<bool> CancelAsync(Guid ticketId);
}