using Moq;
using NextHappen.Engagement.Application.DTOs;
using NextHappen.Engagement.Application.Services;
using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;
using Xunit;

namespace NextHappen.Engagement.Tests;

public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _repoMock;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _repoMock = new Mock<IReviewRepository>();
        _service = new ReviewService(_repoMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpsertAsync_ShouldCreateReview_WhenNotExists()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByUserAndEventAsync(userId, eventId)).ReturnsAsync((Review?)null);

        var result = await _service.UpsertAsync(eventId, userId, "John", 5, "Excelente");

        Assert.NotNull(result);
        Assert.Equal("John", result.UserName);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Excelente", result.Comment);
        _repoMock.Verify(r => r.AddAsync(It.Is<Review>(rev =>
            rev.EventId == eventId &&
            rev.UserId == userId &&
            rev.Rating == 5)), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpsertAsync_ShouldUpdateReview_WhenAlreadyExists()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = new Review(eventId, userId, "John", 3, "Regular");
        _repoMock.Setup(r => r.GetByUserAndEventAsync(userId, eventId)).ReturnsAsync(existing);

        var result = await _service.UpsertAsync(eventId, userId, "John Updated", 4, "Mejoró");

        Assert.NotNull(result);
        Assert.Equal(4, result.Rating);
        Assert.Equal("Mejoró", result.Comment);
        Assert.Equal("John Updated", result.UserName);
        _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpsertAsync_ShouldThrow_WhenRatingIsBelowOne()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpsertAsync(Guid.NewGuid(), Guid.NewGuid(), "John", 0, "Malo"));

        Assert.Equal("La calificación debe estar entre 1 y 5.", exception.Message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpsertAsync_ShouldThrow_WhenRatingIsAboveFive()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpsertAsync(Guid.NewGuid(), Guid.NewGuid(), "John", 6, "Exagerado"));

        Assert.Equal("La calificación debe estar entre 1 y 5.", exception.Message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetSummaryAsync_ShouldReturnSummary_WithAggregatedData()
    {
        var eventId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new Review(eventId, Guid.NewGuid(), "A", 5, "Excelente"),
            new Review(eventId, Guid.NewGuid(), "B", 4, "Bueno"),
            new Review(eventId, Guid.NewGuid(), "C", 3, "Regular"),
        };
        _repoMock.Setup(r => r.GetByEventAsync(eventId)).ReturnsAsync(reviews);

        var result = await _service.GetSummaryAsync(eventId);

        Assert.Equal(eventId, result.EventId);
        Assert.Equal(3, result.Count);
        Assert.Equal(4.0, result.Average);
        Assert.Equal(1, result.Distribution[5]);
        Assert.Equal(1, result.Distribution[4]);
        Assert.Equal(1, result.Distribution[3]);
        Assert.Equal(0, result.Distribution[2]);
        Assert.Equal(0, result.Distribution[1]);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetSummaryAsync_ShouldReturnEmptySummary_WhenNoReviews()
    {
        var eventId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByEventAsync(eventId)).ReturnsAsync(new List<Review>());

        var result = await _service.GetSummaryAsync(eventId);

        Assert.Equal(eventId, result.EventId);
        Assert.Equal(0, result.Count);
        Assert.Equal(0, result.Average);
        Assert.All(result.Distribution, kvp => Assert.Equal(0, kvp.Value));
        Assert.Empty(result.Items);
    }
}