namespace NextHappen.Ticket.Application.DTOs;

public class PurchaseRequest
{
    public Guid UserId { get; set; }
    public int Quantity { get; set; } = 1;
}
