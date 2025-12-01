namespace nexthappen_backend.CreateEvent.Application.Contracts;

public class EventResponse
{
    public Guid Id { get; set; }
    public string Organizer { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public string Category { get; set; }
    public string Address { get; set; }
    public string Location { get; set; }
    public List<string> Photos { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}