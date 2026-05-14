using MassTransit;
using NextHappen.Contracts.Events;
using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;

namespace NextHappen.Engagement.Application.Services;

public class SavedEventService
{
    private readonly ISavedEventRepository _repository;
    private readonly IMetricRepository _metricRepo;
    private readonly IPublishEndpoint _publishEndpoint;

    public SavedEventService(
        ISavedEventRepository repository,
        IMetricRepository metricRepo,
        IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _metricRepo = metricRepo;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> SaveEventAsync(Guid userId, Guid eventId)
    {
        if (await _repository.ExistsAsync(userId, eventId))
            return false;

        await _repository.AddAsync(new SavedEvent(userId, eventId));

        // Register metric
        await _metricRepo.AddAsync(new Metric
        {
            EventId = eventId,
            Action = "saved-event",
            Timestamp = DateTime.UtcNow
        });

        // Publish to RabbitMQ → notification-service will consume this
        await _publishEndpoint.Publish(new EventSavedEvent(
            eventId, userId, Guid.Empty, DateTime.UtcNow));

        return true;
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid eventId)
    {
        if (!await _repository.ExistsAsync(userId, eventId))
            return false;

        await _repository.RemoveAsync(userId, eventId);

        await _metricRepo.AddAsync(new Metric
        {
            EventId = eventId,
            Action = "removed-saved-event",
            Timestamp = DateTime.UtcNow
        });

        // Publish to RabbitMQ
        await _publishEndpoint.Publish(new EventUnsavedEvent(
            eventId, userId, Guid.Empty, DateTime.UtcNow));

        return true;
    }

    public Task<IEnumerable<SavedEvent>> GetByUserAsync(Guid userId)
        => _repository.GetByUserIdAsync(userId);
}

public class MetricService
{
    private readonly IMetricRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public MetricService(IMetricRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public Task<List<Metric>> GetAllAsync() => _repository.GetAllAsync();

    public async Task RegisterAsync(Guid eventId, string action)
    {
        await _repository.AddAsync(new Metric
        {
            EventId = eventId,
            Action = action,
            Timestamp = DateTime.UtcNow
        });

        // Publish event view to RabbitMQ
        if (action == "view-event")
        {
            await _publishEndpoint.Publish(new EventViewedEvent(
                eventId, Guid.Empty, DateTime.UtcNow));
        }
    }
}

