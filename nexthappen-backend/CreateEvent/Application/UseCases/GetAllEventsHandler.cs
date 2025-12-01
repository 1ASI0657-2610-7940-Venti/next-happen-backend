using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Domain.Entities;

namespace nexthappen_backend.CreateEvent.Application.UseCases;

public class GetAllEventsHandler
{
    private readonly IEventRepository _repo;

    public GetAllEventsHandler(IEventRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<EventResponse>> Handle()
    {
        var events = await _repo.GetAllAsync();

        return events.Select(ev => new EventResponse
        {
            Id = ev.Id,
            Organizer = ev.Organizer,
            Title = ev.Title,
            Description = ev.Description,
            Price = ev.Price,
            Quantity = ev.Quantity,
            Category = ev.Category,
            Address = ev.Address,
            Location = ev.Location,
            Photos = ev.Photos,
            StartDate = ev.DateRange.StartDate,
            EndDate = ev.DateRange.EndDate
        });
    }
}
