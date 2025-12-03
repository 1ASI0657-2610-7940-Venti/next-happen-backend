using nexthappen_backend.Tickets.Application.Services;

namespace nexthappen_backend.Tickets.Application.UseCases;

public class PurchaseTicketHandler
{
    private readonly TicketsService _service;

    public PurchaseTicketHandler(TicketsService service)
    {
        _service = service;
    }

    public Task<object> Handle(Guid eventId, Guid userId, int quantity)
    {
        return _service.PurchaseTicketsAsync(eventId, userId, quantity);
    }
}
