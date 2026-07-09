using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
                    options.UseInMemoryDatabase("TestIamDb");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    private static string GenerateToken(Guid userId, string role = "User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASuperSecretKeyForTestingOnly1234567890"));
        var token = new JwtSecurityToken(
            issuer: "NextHappen",
            audience: "NextHappenUsers",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthHeader(Guid userId, string role = "User")
    {
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GenerateToken(userId, role)}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_ShouldReturnOk_AndAllowLogin()
    {
        var registerRequest = new RegisterRequest
        {
            FullName = "Integration User",
            Email = "int@example.com",
            Password = "SecurePassword123",
            Role = "User"
        };

        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        _output.WriteLine($"Register Response: {regResponse.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        _output.WriteLine($"Login Response: {loginResponse.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse?.Token);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_ShouldFail_WhenEmailAlreadyExists()
    {
        var request = new RegisterRequest
        {
            FullName = "User",
            Email = "duplicate@example.com",
            Password = "Pass123",
            Role = "User"
        };

        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_ShouldFail_WithWrongPassword()
    {
        var registerRequest = new RegisterRequest
        {
            FullName = "User",
            Email = "wrongpass@example.com",
            Password = "CorrectPass123",
            Role = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "wrongpass@example.com",
            Password = "WrongPass123"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUser_ShouldReturnUser_WhenAuthenticated()
    {
        var registerRequest = new RegisterRequest
        {
            FullName = "Auth User",
            Email = "authuser@example.com",
            Password = "Pass123",
            Role = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "authuser@example.com",
            Password = "Pass123"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth.Token}");

        var response = await _client.GetAsync($"/api/users/{auth.UserId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUser_ShouldReturnNotFound_WhenNotExists()
    {
        SetAuthHeader(Guid.NewGuid());

        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
