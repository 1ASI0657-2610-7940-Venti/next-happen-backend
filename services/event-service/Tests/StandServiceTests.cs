using Moq;
using NextHappen.Event.Application.Services;
using NextHappen.Event.Domain.Entities;
using NextHappen.Event.Domain.Repositories;
using Xunit;

namespace NextHappen.Event.Tests;

public class StandServiceTests
{
    private readonly Mock<IAssignedStandRepository> _repoMock;
    private readonly StandService _service;

    public StandServiceTests()
    {
        _repoMock = new Mock<IAssignedStandRepository>();
        _service = new StandService(_repoMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AssignAsync_ShouldCreateStand()
    {
        var eventId = Guid.NewGuid();

        var result = await _service.AssignAsync(eventId, "Stand A", "Food");

        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal("Stand A", result.Name);
        Assert.Equal("Food", result.Category);
        _repoMock.Verify(r => r.AddAsync(It.Is<AssignedStand>(s =>
            s.EventId == eventId && s.Name == "Stand A")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByEventAsync_ShouldReturnStands()
    {
        var eventId = Guid.NewGuid();
        var stands = new List<AssignedStand>
        {
            new AssignedStand(eventId, "Stand A", "Food"),
            new AssignedStand(eventId, "Stand B", "Drinks"),
        };
        _repoMock.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(stands);

        var result = await _service.GetByEventAsync(eventId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_ShouldUpdateAndReturn_WhenExists()
    {
        var standId = Guid.NewGuid();
        var existing = new AssignedStand(Guid.NewGuid(), "Old Name", "Old Category");
        _repoMock.Setup(r => r.GetByIdAsync(standId)).ReturnsAsync(existing);

        var result = await _service.UpdateAsync(standId, "New Name", "New Category");

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Category", result.Category);
        _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_ShouldReturnNull_WhenNotExists()
    {
        var standId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(standId)).ReturnsAsync((AssignedStand?)null);

        var result = await _service.UpdateAsync(standId, "Name", "Category");

        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<AssignedStand>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_ShouldReturnTrue_WhenExists()
    {
        var standId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(standId)).ReturnsAsync(new AssignedStand(Guid.NewGuid(), "Name", "Cat"));

        var result = await _service.DeleteAsync(standId);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(standId), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        var standId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(standId)).ReturnsAsync((AssignedStand?)null);

        var result = await _service.DeleteAsync(standId);

        Assert.False(result);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}