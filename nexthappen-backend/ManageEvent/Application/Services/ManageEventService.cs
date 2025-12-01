using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.CreateEvent.Domain.ValueObjects;
using nexthappen_backend.ManageEvent.Domain;

namespace nexthappen_backend.ManageEvent.Application.Services;

public class ManageEventService
{
    private readonly IManageEventRepository _repository;

    public ManageEventService(IManageEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Event>> GetAllEventsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Event?> UpdateEventAsync(
        Guid id,
        string organizer,
        string title,
        string description,
        decimal? price,
        int? quantity,
        string category,
        string address,
        string location,
        IEnumerable<string> photos,
        EventDateRange dateRange,
        bool isPublic)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return null;

        existing.UpdateDetails(
            organizer,
            title,
            description,
            price,
            quantity,
            category,
            address,
            location,
            photos,
            dateRange,
            isPublic
        );

        await _repository.UpdateAsync(existing);
        return existing;
    }

    
    public async Task<bool> DeleteEventAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return false;

        await _repository.DeleteByIdAsync(id);
        return true;
    }


}