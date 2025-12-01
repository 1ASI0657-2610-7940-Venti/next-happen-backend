using nexthappen_backend.CreateEvent.Application.Contracts;
using nexthappen_backend.CreateEvent.Domain.ValueObjects;
using nexthappen_backend.CreateEvent.Application.Services;

namespace nexthappen_backend.CreateEvent.Application.UseCases;

public class CreateEventHandler
{
    private readonly CreateEventService _service;

    public CreateEventHandler(CreateEventService service)
    {
        _service = service;
    }

    public async Task<EventResponse> Handle(CreateEventRequest request)
    {
        var dateRange = new EventDateRange(request.StartDate, request.EndDate);

        var ev = await _service.ExecuteAsync(
            request.Organizer,
            request.Title,
            request.Description,
            request.Price,
            request.Quantity,
            request.Category,
            request.Address,
            request.Location,
            request.Photos,
            dateRange, 
            request.IsPublic
        );

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