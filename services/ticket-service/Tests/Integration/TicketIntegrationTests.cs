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
using Moq;
using Moq.Protected;
using NextHappen.Ticket.Application.DTOs;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Domain.Repositories;
using NextHappen.Ticket.Infrastructure.Persistence;
using Xunit;

namespace NextHappen.Ticket.Tests.Integration;

public class TicketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TicketIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "ThisIsASuperSecretKeyForTestingOnly1234567890");
            builder.UseSetting("Jwt:Issuer", "NextHappen");
            builder.UseSetting("Jwt:Audience", "NextHappenUsers");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TicketDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<TicketDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestTicketDb");
                });

                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(new { Id = Guid.NewGuid(), Price = 100.0m, Title = "Test Event" })
                    });

                var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://event-service") };
                var factoryMock = new Mock<IHttpClientFactory>();
                factoryMock.Setup(f => f.CreateClient("EventService")).Returns(httpClient);

                services.AddSingleton(factoryMock.Object);

                // Replace MassTransit IPublishEndpoint with a stub
                var publishDesc = services.SingleOrDefault(d => d.ServiceType == typeof(MassTransit.IPublishEndpoint));
                if (publishDesc != null) services.Remove(publishDesc);
                var publishMock = new Mock<MassTransit.IPublishEndpoint>();
                publishMock.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
                services.AddSingleton(publishMock.Object);

                // Remove MassTransit hosted services to prevent RabbitMQ connection attempts
                var hostedToRemove = services.Where(d =>
                    d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var hd in hostedToRemove) services.Remove(hd);

                // Register concrete TicketService for controller resolution
                services.AddScoped<TicketService>(sp =>
                    sp.GetRequiredService<ITicketService>() as TicketService
                    ?? ActivatorUtilities.CreateInstance<TicketService>(sp));
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
    public async Task Purchase_ShouldReturnUnauthorized_WithoutToken()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");

        var request = new CheckoutRequest
        {
            EventId = Guid.NewGuid(),
            Quantity = 1
        };

        var response = await _client.PostAsJsonAsync("/api/payments/checkout", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ValidateTicket_ShouldReturnFailure_ForInvalidCode()
    {
        SetAuthHeader(Guid.NewGuid(), "Admin");

        var response = await _client.PostAsJsonAsync("/api/tickets/validate",
            new ValidateTicketRequest { QrCode = "INVALID" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ValidateTicketResponse>();
        Assert.NotNull(result);
        Assert.False(result.Valid);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ValidateTicket_ShouldReturnForbidden_ForUserRole()
    {
        SetAuthHeader(Guid.NewGuid(), "User");

        var response = await _client.PostAsJsonAsync("/api/tickets/validate",
            new ValidateTicketRequest { QrCode = "TEST" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
