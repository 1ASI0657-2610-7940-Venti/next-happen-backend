using nexthappen_backend.SavedEvents.Application.Services;
using nexthappen_backend.SavedEvents.Domain.Entities;

namespace nexthappen_backend.SavedEvents.Application.UseCases;

public class GetSavedEventsHandler
{
    private readonly SavedEventsService _service;

    public GetSavedEventsHandler(SavedEventsService service)
    {
        _service = service;
    }

    public Task<IEnumerable<SavedEvent>> Handle(Guid userId)
        => _service.GetSavedEventsAsync(userId);
}