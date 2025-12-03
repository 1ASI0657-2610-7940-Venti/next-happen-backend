namespace nexthappen_backend.Notifications.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime Timestamp { get; set; }
}