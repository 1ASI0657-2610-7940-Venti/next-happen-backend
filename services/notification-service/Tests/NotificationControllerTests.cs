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
        var request = new CreateNotificationRequest
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Message = "Test message"
        };

        var result = await _controller.Create(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotif = Assert.IsType<Domain.Entities.Notification>(okResult.Value);
        Assert.Equal(request.Message, returnedNotif.Message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Notification>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUser_ShouldReturnNotifications_WhenExist()
    {
        var userId = Guid.NewGuid();
        var notifications = new List<Domain.Entities.Notification>
        {
            new Domain.Entities.Notification { UserId = userId, Message = "Notif 1" },
            new Domain.Entities.Notification { UserId = userId, Message = "Notif 2" },
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(notifications);

        var result = await _controller.GetByUser(userId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<Domain.Entities.Notification>>(okResult.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUser_ShouldReturnEmptyList_WhenNoNotifications()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Domain.Entities.Notification>());

        var result = await _controller.GetByUser(userId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<Domain.Entities.Notification>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkAsRead_ShouldCallRepo()
    {
        var notificationId = 42;

        var result = await _controller.MarkAsRead(notificationId);

        Assert.IsType<OkResult>(result);
        _repoMock.Verify(r => r.MarkAsReadAsync(notificationId), Times.Once);
    }
}
