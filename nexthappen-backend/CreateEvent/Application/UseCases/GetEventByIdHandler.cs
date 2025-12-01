using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Domain.Entities;

namespace nexthappen_backend.CreateEvent.Application.UseCases;

public class GetEventByIdHandler
{
    private readonly IEventRepository _repository;

    public GetEventByIdHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<EventResponse?> Handle(Guid id)
    {
        var ev = await _repository.GetByIdAsync(id);
        if (ev is null) return null;

        return new EventResponse
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
        };
    }
}
