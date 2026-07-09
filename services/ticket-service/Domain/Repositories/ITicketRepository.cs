using NextHappen.Ticket.Domain.Entities;

namespace NextHappen.Ticket.Domain.Repositories;

public interface ITicketRepository
{
    Task AddAsync(Entities.Ticket ticket);
    Task AddRangeAsync(IEnumerable<Entities.Ticket> tickets);
    Task<List<Entities.Ticket>> GetByUserIdAsync(Guid userId);
    Task<Entities.Ticket?> GetByIdAsync(Guid id);
    Task<Entities.Ticket?> GetByQrCodeAsync(string qrCode);
    Task<Entities.Ticket?> GetByShortCodeAsync(string shortCode);
    Task<bool> ShortCodeExistsAsync(string shortCode);
    Task<List<Entities.Ticket>> GetByEventIdAsync(Guid eventId);
    Task<List<Entities.Ticket>> GetByEventIdsAsync(IEnumerable<Guid> eventIds);
    Task UpdateAsync(Entities.Ticket ticket);
    Task<bool> CancelAsync(Guid ticketId);
}
