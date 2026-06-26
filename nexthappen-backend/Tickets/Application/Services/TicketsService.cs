using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.Tickets.Domain.Entities;

namespace nexthappen_backend.Tickets.Application.Services;

public class TicketsService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IEventRepository _eventRepo;

    public TicketsService(ITicketRepository ticketRepo, IEventRepository eventRepo)
    {
        _ticketRepo = ticketRepo;
        _eventRepo = eventRepo;
    }

    public async Task<object> PurchaseTicketsAsync(Guid eventId, Guid userId, int quantity)
    {
        try
        {
            // Reservar cupo de forma atómica usando bloqueo pesimista
            bool reserved = await _eventRepo.ReserveSeatsAsync(eventId, quantity);
            if (!reserved)
                throw new Exception("No hay suficientes cupos disponibles o el evento no existe.");

            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null)
                throw new Exception("El evento no existe en la base de datos.");

            if (ev.Price == null)
                throw new Exception("El evento no tiene precio asignado.");

            decimal unitPrice = ev.Price.Value;
            decimal total = unitPrice * quantity;

            var ticketIds = new List<Guid>();

            for (int i = 0; i < quantity; i++)
            {
                var ticket = new Ticket
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventId = eventId,
                    PurchaseDate = DateTime.UtcNow,
                    Status = "Active"
                };

                await _ticketRepo.AddAsync(ticket);
                ticketIds.Add(ticket.Id);
            }

            return new
            {
                EventId = eventId,
                UserId = userId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Total = total,
                Tickets = ticketIds
            };
        }
        catch (Exception ex)
        {
            throw new Exception("ERROR EN PURCHASE: " + ex.Message);
        }
    }

    public Task<bool> CancelTicketAsync(Guid ticketId)
    {
        return _ticketRepo.CancelAsync(ticketId);
    }
}