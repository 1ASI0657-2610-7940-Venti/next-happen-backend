using nexthappen_backend.EventDiscovery.Application.Services;

namespace nexthappen_backend.EventDiscovery.Application.UseCases;

public class GetPublicEventsHandler
{
    private readonly EventDiscoveryService _service;

    public GetPublicEventsHandler(EventDiscoveryService service)
    {
        _service = service;
    }

    public async Task<IEnumerable<object>> Handle()
    {
        var events = await _service.GetPublicEventsAsync();

        return events.Select(e => new
        {
            e.Id,
            e.Title,
            e.Description,
            StartDate = e.DateRange.StartDate,
            EndDate = e.DateRange.EndDate,
            e.Category,
            e.Price,
            e.Location,
            Photos = e.Photos
        });
    }
}