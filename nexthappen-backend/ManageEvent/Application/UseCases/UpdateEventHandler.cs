using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.CreateEvent.Domain.ValueObjects;
using nexthappen_backend.ManageEvent.Application.Services;

namespace nexthappen_backend.ManageEvent.Application.UseCases;

public class UpdateEventHandler
{
    private readonly ManageEventService _service;

    public UpdateEventHandler(ManageEventService service)
    {
        _service = service;
    }

    public async Task<Event?> HandleAsync(Guid id, UpdateEventRequest req)
    {
        var range = new EventDateRange(req.StartDate, req.EndDate);

        return await _service.UpdateEventAsync(
            id,
            req.Organizer,
            req.Title,
            req.Description,
            req.Price,
            req.Quantity,
            req.Category,
            req.Address,
            req.Location,
            req.Photos,
            range,
            req.IsPublic
        );
    }

}