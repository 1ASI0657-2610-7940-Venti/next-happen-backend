using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.CreateEvent.Domain.ValueObjects;

namespace nexthappen_backend.CreateEvent.Application.Services;

public class CreateEventService
{
    private readonly IEventRepository _repository;

    public CreateEventService(IEventRepository repository)
    {
        _repository = repository;
    }

    // ✅ Nueva firma: usa EventDateRange en lugar de startDate y endDate
    public async Task<Event> ExecuteAsync(
        string organizer,
        string title,
        string description,
        decimal? price,
        int? quantity,
        string category,
        string address,
        string location,
        IEnumerable<string> photos,
        EventDateRange dateRange,
        bool isPublic)
    {
        // Validaciones simples
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.");

        if (dateRange is null)
            throw new ArgumentException("El rango de fechas no puede ser nulo.");

        // Crear entidad de dominio
        var newEvent = new Event(
            organizer,
            title,
            description,
            price,
            quantity,
            category,
            address,
            location,
            photos,
            dateRange, 
            isPublic
        );

        // Guardar en la base de datos
        await _repository.AddAsync(newEvent);
        await _repository.SaveChangesAsync();

        return newEvent;
    }
}