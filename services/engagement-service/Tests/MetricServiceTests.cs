using Moq;
using MassTransit;
using NextHappen.Engagement.Application.Services;
using NextHappen.Engagement.Domain.Entities;
using NextHappen.Engagement.Domain.Repositories;
using NextHappen.Contracts.Events;
using Xunit;

namespace NextHappen.Engagement.Tests;

public class SavedEventServiceAdditionalTests
{
    private readonly Mock<ISavedEventRepository> _savedEventRepoMock;
    private readonly Mock<IMetricRepository> _metricRepoMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly SavedEventService _service;

    public SavedEventServiceAdditionalTests()
    {
        _savedEventRepoMock = new Mock<ISavedEventRepository>();
        _metricRepoMock = new Mock<IMetricRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _service = new SavedEventService(_savedEventRepoMock.Object, _metricRepoMock.Object, _publishEndpointMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveAsync_ShouldReturnTrue_WhenEventIsSaved()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _savedEventRepoMock.Setup(r => r.ExistsAsync(userId, eventId)).ReturnsAsync(true);

        var result = await _service.RemoveAsync(userId, eventId);

        Assert.True(result);
        _savedEventRepoMock.Verify(r => r.RemoveAsync(userId, eventId), Times.Once);
        _metricRepoMock.Verify(r => r.AddAsync(It.Is<Metric>(m =>
            m.EventId == eventId && m.Action == "removed-saved-event")), Times.Once);
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<EventUnsavedEvent>(), default), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveAsync_ShouldReturnFalse_WhenEventIsNotSaved()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _savedEventRepoMock.Setup(r => r.ExistsAsync(userId, eventId)).ReturnsAsync(false);

        var result = await _service.RemoveAsync(userId, eventId);

        Assert.False(result);
        _savedEventRepoMock.Verify(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUserAsync_ShouldReturnSavedEvents()
    {
        var userId = Guid.NewGuid();
        var savedEvents = new List<SavedEvent>
        {
            new SavedEvent(userId, Guid.NewGuid()),
            new SavedEvent(userId, Guid.NewGuid()),
        };
        _savedEventRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(savedEvents);

        var result = await _service.GetByUserAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
}

public class MetricServiceTests
{
    private readonly Mock<IMetricRepository> _repoMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly MetricService _service;

    public MetricServiceTests()
    {
        _repoMock = new Mock<IMetricRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _service = new MetricService(_repoMock.Object, _publishEndpointMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterAsync_ShouldAddMetricAndPublishEvent_WhenActionIsViewEvent()
    {
        var eventId = Guid.NewGuid();

        await _service.RegisterAsync(eventId, "view-event");

        _repoMock.Verify(r => r.AddAsync(It.Is<Metric>(m =>
            m.EventId == eventId && m.Action == "view-event")), Times.Once);
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<EventViewedEvent>(), default), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterAsync_ShouldAddMetric_WhenActionIsGeneric()
    {
        var eventId = Guid.NewGuid();

        await _service.RegisterAsync(eventId, "some-action");

        _repoMock.Verify(r => r.AddAsync(It.Is<Metric>(m =>
            m.EventId == eventId && m.Action == "some-action")), Times.Once);
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<EventViewedEvent>(), default), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllAsync_ShouldReturnAllMetrics()
    {
        var metrics = new List<Metric>
        {
            new Metric { EventId = Guid.NewGuid(), Action = "view-event" },
            new Metric { EventId = Guid.NewGuid(), Action = "saved-event" },
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(metrics);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }
}