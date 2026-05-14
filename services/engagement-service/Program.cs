using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using NextHappen.Engagement.Application.Services;
using NextHappen.Engagement.Domain.Repositories;
using NextHappen.Engagement.Infrastructure.Persistence;
using NextHappen.Engagement.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NextHappen Engagement Service", Version = "v1" });
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
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (!string.IsNullOrEmpty(jwtKey))
        {
            var key = Encoding.UTF8.GetBytes(jwtKey);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true,
                ValidateIssuerSigningKey = true, ValidateLifetime = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        }
    });

builder.Services.AddScoped<ISavedEventRepository, SavedEventRepository>();
builder.Services.AddScoped<IMetricRepository, MetricRepository>();
builder.Services.AddScoped<SavedEventService>();
builder.Services.AddScoped<MetricService>();

builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── RabbitMQ (MassTransit) ──
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
            cfg.ConfigureEndpoints(context);
        });
    });
}
else
{
    builder.Services.AddMassTransit(x => x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context)));
}

builder.Services.AddDbContext<EngagementDbContext>(options =>
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

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        try { scope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.EnsureCreated(); }
        catch (Exception ex) { Console.WriteLine($"[Engagement] Migration Error: {ex.Message}"); }
    }
}

app.MapGet("/", () => Results.Ok("NextHappen Engagement Service is running"));
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
