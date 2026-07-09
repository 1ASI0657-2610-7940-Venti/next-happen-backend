using Moq;
using NextHappen.Event.Application.DTOs;
using NextHappen.Event.Application.Services;
using NextHappen.Event.Domain.Repositories;
using NextHappen.Event.Domain.Entities;
using NextHappen.Event.Domain.ValueObjects;
using Xunit;

namespace NextHappen.Event.Tests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repositoryMock;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _eventService = new EventService(_repositoryMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_ShouldReturnEvent_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Organizer = "Google",
            Title = "I/O 2026",
            Description = "Developer conference",
            Price = 0,
            Quantity = 1000,
            Category = "Tech",
            Address = "Mountain View",
            Location = "Amphitheatre",
            Photos = new List<string> { "photo1.jpg" },
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            IsPublic = true
        };

        // Act
        var result = await _eventService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Organizer, result.Organizer);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Event>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenTitleIsEmpty()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "", // Invalid
            Organizer = "Google"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _eventService.CreateAsync(request));
        Assert.Equal("El título es obligatorio.", exception.Message);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Event>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllAsync_ShouldReturnEvents_WhenRepositoryHasData()
    {
        // Arrange
        var events = new List<Domain.Entities.Event>
        {
            new Domain.Entities.Event("Org1", "Event 1", "Desc 1", 10, 100, "Cat1", "Addr1", "Loc1", new List<string>(), new EventDateRange(DateTime.UtcNow, DateTime.UtcNow.AddHours(1))),
            new Domain.Entities.Event("Org2", "Event 2", "Desc 2", 20, 200, "Cat2", "Addr2", "Loc2", new List<string>(), new EventDateRange(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)))
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(events);

        // Act
        var result = await _eventService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(events, result);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_ShouldReturnEvent_WhenExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Domain.Entities.Event("Org1", "Event 1", "Desc 1", 10, 100, "Cat1", "Addr1", "Loc1", new List<string>(), new EventDateRange(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(existingEvent);

        // Act
        var result = await _eventService.GetByIdAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingEvent, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_ShouldReturnTrue_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Domain.Entities.Event("Org1", "Event 1", "Desc 1", 10, 100, "Cat1", "Addr1", "Loc1", new List<string>(), new EventDateRange(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(existingEvent);

        // Act
        var result = await _eventService.DeleteAsync(eventId);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(r => r.DeleteByIdAsync(eventId), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_ShouldReturnFalse_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Domain.Entities.Event?)null);

        // Act
        var result = await _eventService.DeleteAsync(eventId);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(r => r.DeleteByIdAsync(eventId), Times.Never);
    }
}
