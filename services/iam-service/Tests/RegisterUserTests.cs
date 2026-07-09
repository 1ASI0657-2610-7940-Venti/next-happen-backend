using Moq;
using NextHappen.IAM.Application.DTOs;
using NextHappen.IAM.Application.UseCases;
using NextHappen.IAM.Domain.Entities;
using NextHappen.IAM.Domain.Repositories;
using NextHappen.IAM.Domain.Services;
using Xunit;

namespace NextHappen.IAM.Tests;

public class RegisterUserTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly RegisterUser _registerUser;

    public RegisterUserTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _registerUser = new RegisterUser(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ShouldRegisterUser_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "password123",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.Hash(request.Password)).Returns("hashed_password");

        // Act
        await _registerUser.HandleAsync(request);

        // Assert
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => 
            u.Email == request.Email && 
            u.FullName == request.FullName && 
            u.PasswordHash == "hashed_password")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new User());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _registerUser.HandleAsync(request));
        Assert.Equal("El correo ya está registrado.", exception.Message);
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }
}
