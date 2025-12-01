using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.EventDiscovery.Domain.Entities;

namespace nexthappen_backend.EventDiscovery.Application.Services;

public class EventDiscoveryService
{
    private readonly IEventRepository _repository;

    public EventDiscoveryService(IEventRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<Event>> GetPublicEventsAsync()
    {
        return _repository.GetPublicEventsAsync();
    }
}