using NextHappen.Engagement.Domain.Entities;

namespace NextHappen.Engagement.Domain.Repositories;

public interface ISavedEventRepository
{
    Task AddAsync(SavedEvent savedEvent);
    Task RemoveAsync(Guid userId, Guid eventId);
    Task<bool> ExistsAsync(Guid userId, Guid eventId);
    Task<IEnumerable<SavedEvent>> GetByUserIdAsync(Guid userId);
}

public interface IMetricRepository
{
    Task AddAsync(Metric metric);
    Task<List<Metric>> GetAllAsync();
}
