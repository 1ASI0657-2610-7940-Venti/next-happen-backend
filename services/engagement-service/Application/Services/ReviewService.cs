using NextHappen.Engagement.Application.DTOs;
using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;

namespace NextHappen.Engagement.Application.Services;

/// <summary>
/// Gestiona las reseñas y calificaciones de eventos. Un usuario tiene a lo sumo una
/// reseña por evento: si ya existe, se actualiza en lugar de duplicarse.
/// </summary>
public class ReviewService
{
    private readonly IReviewRepository _repository;

    public ReviewService(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReviewResponse> UpsertAsync(Guid eventId, Guid userId, string userName, int rating, string comment)
    {
        var existing = await _repository.GetByUserAndEventAsync(userId, eventId);
        if (existing is not null)
        {
            existing.SetContent(rating, comment);
            existing.UserName = userName;
            await _repository.UpdateAsync(existing);
            return ToResponse(existing);
        }

        var review = new Review(eventId, userId, userName, rating, comment);
        await _repository.AddAsync(review);
        return ToResponse(review);
    }

    public async Task<ReviewSummaryResponse> GetSummaryAsync(Guid eventId)
    {
        var reviews = await _repository.GetByEventAsync(eventId);

        var distribution = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
        foreach (var r in reviews)
            if (distribution.ContainsKey(r.Rating)) distribution[r.Rating]++;

        return new ReviewSummaryResponse
        {
            EventId = eventId,
            Count = reviews.Count,
            Average = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 2) : 0,
            Distribution = distribution,
            Items = reviews.Select(ToResponse).ToList()
        };
    }

    private static ReviewResponse ToResponse(Review r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.UserName,
        Rating = r.Rating,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt
    };
}
