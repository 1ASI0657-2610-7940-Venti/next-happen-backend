using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;

namespace NextHappen.Engagement.Application.Services;

public class SavedEventService
{
    private readonly ISavedEventRepository _repository;
    private readonly IMetricRepository _metricRepo;

    public SavedEventService(ISavedEventRepository repository, IMetricRepository metricRepo)
    {
        _repository = repository;
        _metricRepo = metricRepo;
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

        return true;
    }

    public Task<IEnumerable<SavedEvent>> GetByUserAsync(Guid userId)
        => _repository.GetByUserIdAsync(userId);
}

public class MetricService
{
    private readonly IMetricRepository _repository;

    public MetricService(IMetricRepository repository)
    {
        _repository = repository;
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
    }
}
