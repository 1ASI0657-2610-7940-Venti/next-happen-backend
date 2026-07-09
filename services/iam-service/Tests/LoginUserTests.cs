using Moq;
using NextHappen.IAM.Application.DTOs;
using NextHappen.IAM.Application.UseCases;
using NextHappen.IAM.Domain.Entities;
using NextHappen.IAM.Domain.Repositories;
using NextHappen.IAM.Domain.Services;
using Xunit;

namespace NextHappen.IAM.Tests;

public class LoginUserTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock;
    private readonly LoginUser _loginUser;

    public LoginUserTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _loginUser = new LoginUser(_userRepositoryMock.Object, _passwordHasherMock.Object, _tokenGeneratorMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "password123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hashed_password",
            Role = "User",
            AvatarUrl = "https://example.com/avatar.png"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(request.Password, user.PasswordHash)).Returns(true);
        _tokenGeneratorMock.Setup(t => t.GenerateToken(user.Id, user.Email, user.Role, user.FullName))
            .Returns("fake_jwt_token");

        var result = await _loginUser.HandleAsync(request);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Role, result.Role);
        Assert.Equal("fake_jwt_token", result.Token);
        Assert.Equal(user.AvatarUrl, result.AvatarUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ShouldThrowException_WhenEmailNotFound()
    {
        var request = new LoginRequest
        {
            Email = "notfound@example.com",
            Password = "password123"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<Exception>(() => _loginUser.HandleAsync(request));
        Assert.Equal("Credenciales inválidas.", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ShouldThrowException_WhenPasswordIsInvalid()
    {
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "wrong_password"
        };

        var user = new User
        {
            Email = "john@example.com",
            PasswordHash = "hashed_password"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(request.Password, user.PasswordHash)).Returns(false);

        var exception = await Assert.ThrowsAsync<Exception>(() => _loginUser.HandleAsync(request));
        Assert.Equal("Credenciales inválidas.", exception.Message);
    }
}