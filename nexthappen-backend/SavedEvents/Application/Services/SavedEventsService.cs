using nexthappen_backend.Metrics.Domain;
using nexthappen_backend.Metrics.Domain.Entities;
using nexthappen_backend.Notifications.Application.Services;
using nexthappen_backend.SavedEvents.Domain;
using nexthappen_backend.SavedEvents.Domain.Entities;

namespace nexthappen_backend.SavedEvents.Application.Services;

public class SavedEventsService
{
    private readonly ISavedEventRepository _repository;
    private readonly IMetricRepository _metricRepository;
    private readonly NotificationService _notificationService;
    
    public SavedEventsService(
        ISavedEventRepository repository,
        IMetricRepository metricRepository,
        NotificationService notificationService)
    {
        _repository = repository;
        _metricRepository = metricRepository;
        _notificationService = notificationService;
    }

    public async Task<bool> SaveEventAsync(Guid userId, Guid eventId)
    {
        if (await _repository.ExistsAsync(userId, eventId))
            return false;

        var savedEvent = new SavedEvent(userId, eventId);
        await _repository.AddAsync(savedEvent);

        // REGISTRAR MÉTRICA
        await _metricRepository.AddAsync(new Metric
        {
            EventId = eventId,
            Action = "saved-event",
            Timestamp = DateTime.UtcNow
        });
        
        await _notificationService.NotifyOrganizerAsync(eventId, "Un usuario guardó tu evento.");

        return true;
    }

    public async Task<bool> RemoveSavedEventAsync(Guid userId, Guid eventId)
    {
        if (!await _repository.ExistsAsync(userId, eventId))
            return false;

        await _repository.RemoveAsync(userId, eventId);

        // REGISTRAR MÉTRICA
        await _metricRepository.AddAsync(new Metric
        {
            EventId = eventId,
            Action = "removed-saved-event",
            Timestamp = DateTime.UtcNow
        });
        
        await _notificationService.NotifyOrganizerAsync(eventId, "Un usuario quitó tu evento de guardados.");

        return true;
    }

    public Task<IEnumerable<SavedEvent>> GetSavedEventsAsync(Guid userId)
        => _repository.GetSavedEventsAsync(userId);
}