using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using NextHappen.Notification.Domain.Repositories;
using NextHappen.Notification.Infrastructure.Messaging;
using NextHappen.Notification.Infrastructure.Persistence;
using NextHappen.Notification.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NextHappen Notification Service", Version = "v1" });
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
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateIssuerSigningKey = true, ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── RabbitMQ (MassTransit) — Consumers ──
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EventSavedConsumer>();
    x.AddConsumer<EventUnsavedConsumer>();
    x.AddConsumer<EventViewedConsumer>();

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
                h.UseSsl(s =>
                {
                    s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                });
            }
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 32)),
        mysql => mysql.SchemaBehavior(MySqlSchemaBehavior.Ignore));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try { scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.EnsureCreated(); }
    catch (Exception ex) { Console.WriteLine($"[Notification] Migration Error: {ex.Message}"); }
}

app.MapGet("/", () => Results.Ok("NextHappen Notification Service is running"));
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
