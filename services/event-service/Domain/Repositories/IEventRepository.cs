using NextHappen.Event.Domain.Entities;

namespace NextHappen.Event.Domain.Repositories;

public interface IEventRepository
{
    Task AddAsync(Entities.Event entity);
    Task<IEnumerable<Entities.Event>> GetAllAsync();
    Task<Entities.Event?> GetByIdAsync(Guid id);
    Task<IEnumerable<Entities.Event>> GetPublicEventsAsync();
    Task UpdateAsync(Entities.Event ev);
    Task DeleteByIdAsync(Guid id);
    Task<bool> ReserveSeatsAsync(Guid id, int quantity);
    Task SaveChangesAsync();
}

