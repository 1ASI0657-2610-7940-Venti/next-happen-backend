using nexthappen_backend.SavedEvents.Domain.ValueObjects;

namespace nexthappen_backend.SavedEvents.Domain.Entities;

public interface ISavedEventRepository
{
    Task AddAsync(SavedEvent savedEvent);
    Task RemoveAsync(Guid userId, Guid eventId);
    Task<IEnumerable<SavedEvent>> GetSavedEventsAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId, Guid eventId);
}
