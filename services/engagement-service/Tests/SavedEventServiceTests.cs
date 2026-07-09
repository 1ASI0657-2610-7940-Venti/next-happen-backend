using Moq;
using MassTransit;
using NextHappen.Engagement.Application.Services;
using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;
using NextHappen.Contracts.Events;
using Xunit;

namespace NextHappen.Engagement.Tests;

public class SavedEventServiceTests
{
    private readonly Mock<ISavedEventRepository> _savedEventRepoMock;
    private readonly Mock<IMetricRepository> _metricRepoMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly SavedEventService _service;

    public SavedEventServiceTests()
    {
        _savedEventRepoMock = new Mock<ISavedEventRepository>();
        _metricRepoMock = new Mock<IMetricRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _service = new SavedEventService(_savedEventRepoMock.Object, _metricRepoMock.Object, _publishEndpointMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveEventAsync_ShouldReturnTrue_WhenEventNotAlreadySaved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _savedEventRepoMock.Setup(r => r.ExistsAsync(userId, eventId)).ReturnsAsync(false);

        // Act
        var result = await _service.SaveEventAsync(userId, eventId);

        // Assert
        Assert.True(result);
        _savedEventRepoMock.Verify(r => r.AddAsync(It.IsAny<SavedEvent>()), Times.Once);
        _metricRepoMock.Verify(r => r.AddAsync(It.IsAny<Metric>()), Times.Once);
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<EventSavedEvent>(), default), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveEventAsync_ShouldReturnFalse_WhenEventAlreadySaved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _savedEventRepoMock.Setup(r => r.ExistsAsync(userId, eventId)).ReturnsAsync(true);

        // Act
        var result = await _service.SaveEventAsync(userId, eventId);

        // Assert
        Assert.False(result);
        _savedEventRepoMock.Verify(r => r.AddAsync(It.IsAny<SavedEvent>()), Times.Never);
    }
}
