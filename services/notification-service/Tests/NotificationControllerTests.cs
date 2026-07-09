using Moq;
using Microsoft.AspNetCore.Mvc;
using NextHappen.Notification.API.Controllers;
using NextHappen.Notification.Domain.Entities;
using NextHappen.Notification.Domain.Repositories;
using Xunit;

namespace NextHappen.Notification.Tests;

public class NotificationControllerTests
{
    private readonly Mock<INotificationRepository> _repoMock;
    private readonly NotificationController _controller;

    public NotificationControllerTests()
    {
        _repoMock = new Mock<INotificationRepository>();
        _controller = new NotificationController(_repoMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Create_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateNotificationRequest
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Message = "Test message"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotif = Assert.IsType<Domain.Entities.Notification>(okResult.Value);
        Assert.Equal(request.Message, returnedNotif.Message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Notification>()), Times.Once);
    }
}
