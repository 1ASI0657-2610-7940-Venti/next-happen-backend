using NextHappen.Event.Domain.Entities;

namespace NextHappen.Event.Domain.Repositories;

public interface IAssignedStandRepository
{
    Task<List<AssignedStand>> GetByEventIdAsync(Guid eventId);
    Task<AssignedStand?> GetByIdAsync(Guid id);
    Task AddAsync(AssignedStand stand);
    Task UpdateAsync(AssignedStand stand);
    Task DeleteAsync(Guid id);
}
