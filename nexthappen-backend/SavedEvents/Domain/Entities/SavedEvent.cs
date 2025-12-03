using nexthappen_backend.SavedEvents.Domain.ValueObjects;

namespace nexthappen_backend.SavedEvents.Domain.Entities;

public class SavedEvent
{
    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public SavedEventStatus Status { get; private set; }

    protected SavedEvent() { }

    public SavedEvent(Guid userId, Guid eventId)
    {
        UserId = userId;
        EventId = eventId;
        Status = SavedEventStatus.Active;
    }

    public void Deactivate() => Status = SavedEventStatus.Inactive;
}
