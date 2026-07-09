namespace NextHappen.Engagement.Application.DTOs;

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Resumen de reseñas de un evento: promedio, total y distribución por estrellas.</summary>
public class ReviewSummaryResponse
{
    public Guid EventId { get; set; }
    public double Average { get; set; }
    public int Count { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new();
    public List<ReviewResponse> Items { get; set; } = new();
}
