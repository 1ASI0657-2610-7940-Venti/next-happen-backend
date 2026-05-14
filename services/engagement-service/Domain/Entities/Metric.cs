namespace NextHappen.Engagement.Domain.Entities;

public class Metric
{
    public int Id { get; set; }
    public Guid EventId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
