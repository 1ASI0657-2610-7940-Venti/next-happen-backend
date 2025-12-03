namespace nexthappen_backend.Tickets.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = "Active";
}