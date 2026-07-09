using System.Net.Http.Json;

namespace NextHappen.Ticket.Infrastructure.Http;

/// <summary>
/// Cliente HTTP hacia event-service. Aísla a la capa de aplicación de los detalles
/// de comunicación entre servicios (patrón Adapter).
/// </summary>
public record EventInfo(Guid Id, string? Title, decimal? Price, string? Organizer);

public interface IEventCatalogClient
{
    Task<EventInfo?> GetEventAsync(Guid eventId);
    Task<bool> ReserveSeatsAsync(Guid eventId, int quantity);
    Task<bool> ReleaseSeatsAsync(Guid eventId, int quantity);
}

public class EventCatalogClient : IEventCatalogClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EventCatalogClient> _logger;

    public EventCatalogClient(IHttpClientFactory httpFactory, ILogger<EventCatalogClient> logger)
    {
        _http = httpFactory.CreateClient("EventService");
        _logger = logger;
    }

    /// <summary>Obtiene los datos del evento (precio, título, organizador).</summary>
    public async Task<EventInfo?> GetEventAsync(Guid eventId)
    {
        var response = await _http.GetAsync($"/api/events/{eventId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<EventInfo>();
    }

    /// <summary>Reserva cupos con bloqueo pesimista. Devuelve false si no hay stock.</summary>
    public async Task<bool> ReserveSeatsAsync(Guid eventId, int quantity)
    {
        var response = await _http.PostAsJsonAsync($"/api/events/{eventId}/reserve", new { Quantity = quantity });
        return response.IsSuccessStatusCode;
    }

    /// <summary>Devuelve cupos al inventario (pago expirado o reembolso).</summary>
    public async Task<bool> ReleaseSeatsAsync(Guid eventId, int quantity)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/events/{eventId}/release", new { Quantity = quantity });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Ticket] No se pudieron liberar {Qty} cupos del evento {EventId}", quantity, eventId);
            return false;
        }
    }
}
