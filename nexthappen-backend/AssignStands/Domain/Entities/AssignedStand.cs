namespace nexthappen_backend.AssignStands.Domain.Entities;

public class AssignedStand
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;

    private AssignedStand() {}

    public AssignedStand(Guid eventId, string name, string category)
    {
        EventId = eventId;
        Name = name.Trim();
        Category = category.Trim();
    }

    public void Update(string name, string category)
    {
        Name = name.Trim();
        Category = category.Trim();
    }
}