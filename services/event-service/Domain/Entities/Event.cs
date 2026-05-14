using NextHappen.Event.Domain.ValueObjects;

namespace NextHappen.Event.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Organizer { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal? Price { get; private set; }
    public int? Quantity { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public List<string> Photos { get; private set; } = new();
    public EventDateRange DateRange { get; private set; } = null!;
    public bool IsPublic { get; private set; }

    private Event() { }

    public Event(
        string organizer, string title, string description,
        decimal? price, int? quantity, string category,
        string address, string location,
        IEnumerable<string> photos, EventDateRange dateRange,
        bool isPublic = true)
    {
        Organizer = organizer;
        Title = title;
        Description = description;
        Price = price;
        Quantity = quantity;
        Category = category;
        Address = address;
        Location = location;
        Photos = photos.ToList();
        DateRange = dateRange;
        IsPublic = isPublic;
    }

    public void UpdateDetails(
        string organizer, string title, string description,
        decimal? price, int? quantity, string category,
        string address, string location,
        IEnumerable<string> photos, EventDateRange dateRange,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título del evento no puede estar vacío.");
        if (price.HasValue && price < 0)
            throw new ArgumentException("El precio no puede ser negativo.");
        if (quantity.HasValue && quantity < 0)
            throw new ArgumentException("La cantidad no puede ser negativa.");
        if (dateRange.StartDate > dateRange.EndDate)
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha de fin.");

        Organizer = organizer;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        Quantity = quantity;
        Category = category?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        Location = location?.Trim() ?? string.Empty;
        Photos = photos?.ToList() ?? new List<string>();
        DateRange = dateRange;
        IsPublic = isPublic;
    }
}
