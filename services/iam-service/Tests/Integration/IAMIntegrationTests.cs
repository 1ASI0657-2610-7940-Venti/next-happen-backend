using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextHappen.IAM.Application.DTOs;
using NextHappen.IAM.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace NextHappen.IAM.Tests.Integration;

public class IAMIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public IAMIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "ThisIsASuperSecretKeyForTestingOnly1234567890");
            builder.UseSetting("Jwt:Issuer", "NextHappen");
            builder.UseSetting("Jwt:Audience", "NextHappenUsers");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<IamDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<IamDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestIamDb_Single");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_ShouldReturnOk_AndAllowLogin()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FullName = "Integration User",
            Email = "int@example.com",
            Password = "SecurePassword123",
            Role = "User"
        };

        // Act - Register
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var regContent = await regResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Register Response: {regResponse.StatusCode} - {regContent}");

        // Assert - Register
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        // Act - Login
        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Login Response: {loginResponse.StatusCode} - {loginContent}");

        // Assert - Login
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse?.Token);
    }
}
