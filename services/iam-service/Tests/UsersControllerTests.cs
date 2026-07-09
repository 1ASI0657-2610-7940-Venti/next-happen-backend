using Microsoft.AspNetCore.Mvc;
using Moq;
using NextHappen.IAM.API.Controllers;
using NextHappen.IAM.Application.DTOs;
using NextHappen.IAM.Domain.Entities;
using NextHappen.IAM.Domain.Repositories;
using Xunit;

namespace NextHappen.IAM.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _controller = new UsersController(_repoMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUser_ShouldReturnUser_WhenExists()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "John Doe",
            Email = "john@example.com",
            Role = "User",
            AvatarUrl = "https://example.com/avatar.png"
        };
        _repoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _controller.GetUser(userId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var dict = value.GetType();
        Assert.Equal(userId, dict.GetProperty("Id")!.GetValue(value));
        Assert.Equal("John Doe", dict.GetProperty("FullName")!.GetValue(value));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUser_ShouldReturnNotFound_WhenNotExists()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _controller.GetUser(userId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateUser_ShouldUpdateFields_WhenValid()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "Old Name",
            Email = "old@example.com",
            Role = "User"
        };
        _repoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var request = new UpdateUserRequest
        {
            FullName = "New Name",
            Email = "new@example.com",
            AvatarUrl = "https://example.com/new.png"
        };

        var result = await _controller.UpdateUser(userId, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Perfil actualizado correctamente", okResult.Value.GetType().GetProperty("message")!.GetValue(okResult.Value)!.ToString());
        Assert.Equal("New Name", user.FullName);
        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("https://example.com/new.png", user.AvatarUrl);
        _repoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateUser_ShouldReturnNotFound_WhenNotExists()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _controller.UpdateUser(userId, new UpdateUserRequest());

        Assert.IsType<NotFoundResult>(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}