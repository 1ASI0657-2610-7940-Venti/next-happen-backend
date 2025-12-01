using nexthappen_backend.Metrics.Domain.Entities;

namespace nexthappen_backend.Metrics.Domain;

public interface IMetricRepository
{
    Task AddAsync(Metric metric);
    Task<List<Metric>> GetAllAsync();
}