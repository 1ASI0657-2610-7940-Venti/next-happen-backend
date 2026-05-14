namespace NextHappen.Ticket.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = "Active";
}
