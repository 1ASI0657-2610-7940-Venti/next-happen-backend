namespace nexthappen_backend.CreateEvent.Application.Contracts;

public class UpdateEventRequest
{
    public string Organizer { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public bool IsPublic { get; set; } = true;
}