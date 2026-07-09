namespace NextHappen.Engagement.Domain.Entities;

/// <summary>
/// Reseña de un usuario sobre un evento. Un usuario puede tener a lo sumo una
/// reseña por evento (se actualiza si vuelve a enviar).
/// </summary>
public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    /// <summary>Calificación de 1 a 5 estrellas.</summary>
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Review() { }

    public Review(Guid eventId, Guid userId, string userName, int rating, string comment)
    {
        EventId = eventId;
        UserId = userId;
        UserName = userName;
        SetContent(rating, comment);
    }

    public void SetContent(int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("La calificación debe estar entre 1 y 5.");
        Rating = rating;
        Comment = comment?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
