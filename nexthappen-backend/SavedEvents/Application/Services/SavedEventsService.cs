using nexthappen_backend.SavedEvents.Domain;
using nexthappen_backend.SavedEvents.Domain.Entities;

namespace nexthappen_backend.SavedEvents.Application.Services;

public class SavedEventsService
{
    private readonly ISavedEventRepository _repository;

    public SavedEventsService(ISavedEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> SaveEventAsync(Guid userId, Guid eventId)
    {
        if (await _repository.ExistsAsync(userId, eventId))
            return false;

        var savedEvent = new SavedEvent(userId, eventId);

        await _repository.AddAsync(savedEvent);
        return true;
    }

    public async Task<bool> RemoveSavedEventAsync(Guid userId, Guid eventId)
    {
        if (!await _repository.ExistsAsync(userId, eventId))
            return false;

        await _repository.RemoveAsync(userId, eventId);
        return true;
    }

    public Task<IEnumerable<SavedEvent>> GetSavedEventsAsync(Guid userId)
        => _repository.GetSavedEventsAsync(userId);
}
