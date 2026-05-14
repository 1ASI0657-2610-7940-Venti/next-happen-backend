namespace NextHappen.Engagement.Domain.Entities;

public class SavedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public SavedEvent() { }
    public SavedEvent(Guid userId, Guid eventId)
    {
        UserId = userId;
        EventId = eventId;
    }
}
