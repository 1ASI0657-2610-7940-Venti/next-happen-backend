using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using NextHappen.Event.Application.Services;
using NextHappen.Event.Domain.Repositories;
using NextHappen.Event.Infrastructure.Persistence;
using NextHappen.Event.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── Routing ──
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// ── Controllers ──
builder.Services.AddControllers();

// ── Swagger ──
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NextHappen Event Service",
        Version = "v1",
        Description = "Event management microservice (CRUD, Discovery, Stands)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── JWT (validate only — tokens are issued by iam-service) ──
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
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        }
    });

// ── DI ──
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IAssignedStandRepository, AssignedStandRepository>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<StandService>();

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ── Database ──
builder.Services.AddDbContext<EventDbContext>(options =>
{
    // Skip configuration if we are in Testing environment (tests will provide their own DB provider)
    if (builder.Environment.IsEnvironment("Testing")) return;

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 32)),
            mysql => mysql.SchemaBehavior(MySqlSchemaBehavior.Ignore)
        );
    }
});

var app = builder.Build();

// ── Auto-migrate ──
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Event] Migration Error: {ex.Message}");
        }
    }
}

// ── Pipeline ──
app.MapGet("/", () => Results.Ok("NextHappen Event Service is running"));

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
