using nexthappen_backend.Metrics.Domain;
using nexthappen_backend.Metrics.Domain.Entities;

public class MetricsService
{
    private readonly IMetricRepository _repository;

    public MetricsService(IMetricRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Metric>> GetAllAsync()
        => _repository.GetAllAsync();

    public async Task RegisterAsync(Guid eventId, string action, DateTime timestamp)
    {
        var metric = new Metric
        {
            EventId = eventId,
            Action = action,
            Timestamp = timestamp
        };

        await _repository.AddAsync(metric);
    }
}