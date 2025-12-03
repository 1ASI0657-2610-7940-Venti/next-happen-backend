namespace nexthappen_backend.Tickets.Application.DTOs;

public class PurchaseTicketRequest
{
    public Guid UserId { get; set; }
    public int Quantity { get; set; }
}