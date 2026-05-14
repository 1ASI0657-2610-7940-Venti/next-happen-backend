using NextHappen.Event.Domain.Repositories;
using NextHappen.Event.Domain.ValueObjects;
using NextHappen.Event.Application.DTOs;

namespace NextHappen.Event.Application.Services;

public class EventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<Domain.Entities.Event> CreateAsync(CreateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("El título es obligatorio.");

        var dateRange = new EventDateRange(request.StartDate, request.EndDate);

        var newEvent = new Domain.Entities.Event(
            request.Organizer, request.Title, request.Description,
            request.Price, request.Quantity, request.Category,
            request.Address, request.Location,
            request.Photos, dateRange, request.IsPublic
        );

        await _repository.AddAsync(newEvent);
        await _repository.SaveChangesAsync();
        return newEvent;
    }

    public async Task<IEnumerable<Domain.Entities.Event>> GetAllAsync()
        => await _repository.GetAllAsync();

    public async Task<Domain.Entities.Event?> GetByIdAsync(Guid id)
        => await _repository.GetByIdAsync(id);

    public async Task<IEnumerable<Domain.Entities.Event>> GetPublicAsync()
        => await _repository.GetPublicEventsAsync();

    public async Task<Domain.Entities.Event?> UpdateAsync(Guid id, UpdateEventRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return null;

        var range = new EventDateRange(request.StartDate, request.EndDate);
        existing.UpdateDetails(
            request.Organizer, request.Title, request.Description,
            request.Price, request.Quantity, request.Category,
            request.Address, request.Location,
            request.Photos, range, request.IsPublic
        );

        await _repository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return false;

        await _repository.DeleteByIdAsync(id);
        return true;
    }
}
