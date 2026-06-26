using System.Net.Http.Json;
using NextHappen.Ticket.Domain.Repositories;

namespace NextHappen.Ticket.Application.Services;

public class TicketService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly HttpClient _eventClient;

    public TicketService(ITicketRepository ticketRepo, IHttpClientFactory httpFactory)
    {
        _ticketRepo = ticketRepo;
        _eventClient = httpFactory.CreateClient("EventService");
    }

    public async Task<object> PurchaseAsync(Guid eventId, Guid userId, int quantity)
    {
        // 1. Reservamos los cupos con bloqueo pesimista en event-service
        var reserveResponse = await _eventClient.PostAsJsonAsync($"/api/events/{eventId}/reserve", new { Quantity = quantity });
        if (!reserveResponse.IsSuccessStatusCode)
            throw new Exception("No hay suficientes cupos disponibles o el evento no existe.");

        // 2. Fetch event price from event-service via HTTP
        var response = await _eventClient.GetAsync($"/api/events/{eventId}");
        if (!response.IsSuccessStatusCode)
            throw new Exception("El evento no existe o no está disponible.");

        var eventData = await response.Content.ReadFromJsonAsync<EventInfo>();
        decimal unitPrice = eventData?.Price ?? 0;
        decimal total = unitPrice * quantity;

        var ticketIds = new List<Guid>();

        for (int i = 0; i < quantity; i++)
        {
            var ticket = new Domain.Entities.Ticket
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


    public Task<List<Domain.Entities.Ticket>> GetByUserAsync(Guid userId)
        => _ticketRepo.GetByUserIdAsync(userId);

    public Task<Domain.Entities.Ticket?> GetByIdAsync(Guid ticketId)
        => _ticketRepo.GetByIdAsync(ticketId);

    public Task<bool> CancelAsync(Guid ticketId)
        => _ticketRepo.CancelAsync(ticketId);

    // DTO for deserializing event-service response
    private class EventInfo
    {
        public Guid Id { get; set; }
        public decimal? Price { get; set; }
    }
}
