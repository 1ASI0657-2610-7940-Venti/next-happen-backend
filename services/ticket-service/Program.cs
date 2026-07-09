using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Stripe;
using NextHappen.Ticket.Application.Services;
using NextHappen.Ticket.Domain.Repositories;
using NextHappen.Ticket.Infrastructure.Http;
using NextHappen.Ticket.Infrastructure.Payments;
using NextHappen.Ticket.Infrastructure.Persistence;
using NextHappen.Ticket.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NextHappen Ticket Service", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? builder.Configuration["JWT_KEY"] ?? "DefaultSuperSecretKeyForDevelopmentOnly!";
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["JWT_ISSUER"];
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["JWT_AUDIENCE"];

        var key = Encoding.UTF8.GetBytes(jwtKey);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateIssuerSigningKey = true, ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// HttpClient for event-service communication
builder.Services.AddHttpClient("EventService", client =>
{
    var url = builder.Configuration["Services:EventServiceUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(url);
});

// ── Stripe (Checkout + Webhooks) ──
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ── RabbitMQ (MassTransit) — publica TicketPurchasedEvent al confirmarse el pago ──
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var virtualHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
        var useSsl = builder.Configuration.GetValue<bool>("RabbitMQ:UseSsl");
        ushort port = useSsl ? (ushort)5671 : (ushort)5672;

        if (host.Contains(':'))
        {
            var parts = host.Split(':');
            host = parts[0];
            ushort.TryParse(parts[1], out port);
        }

        cfg.Host(host, port, virtualHost, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            if (useSsl)
            {
                h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
            }
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<EventCatalogClient>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<PaymentService>();

// ── CORS (configurable whitelist) ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:4173" };
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddDbContext<TicketDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing")) return;

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 32)),
            mysql => mysql.SchemaBehavior(MySqlSchemaBehavior.Ignore));
    }
});

var app = builder.Build();

// ── Database initialization (dev convenience; disable in prod with Database:AutoCreate=false) ──
if (!app.Environment.IsEnvironment("Testing") && app.Configuration.GetValue("Database:AutoCreate", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        scope.ServiceProvider.GetRequiredService<TicketDbContext>().Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "[Ticket] Database initialization failed");
        throw;
    }
}

app.MapGet("/", () => Results.Ok("NextHappen Ticket Service is running"));
app.MapGet("/health", async (TicketDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
