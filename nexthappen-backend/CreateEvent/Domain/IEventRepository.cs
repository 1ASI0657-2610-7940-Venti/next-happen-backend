namespace nexthappen_backend.CreateEvent.Domain.Entities;

public interface IEventRepository
{
    Task AddAsync(Event entity);
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
    Task UpdateAsync(Event ev);
    Task DeleteByIdAsync(Guid id);
    Task<IEnumerable<Event>> GetPublicEventsAsync();
    Task<bool> ReserveSeatsAsync(Guid id, int quantity);
}