using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using NextHappen.IAM.Domain.Repositories;
using NextHappen.IAM.Domain.Services;
using NextHappen.IAM.Infrastructure.Persistence;
using NextHappen.IAM.Infrastructure.Persistence.Repositories;
using NextHappen.IAM.Infrastructure.Security;
using NextHappen.IAM.Application.UseCases;

var builder = WebApplication.CreateBuilder(args);

// ── Routing ──
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// ── Controllers ──
builder.Services.AddControllers();

// ── Swagger + JWT ──
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NextHappen IAM Service",
        Version = "v1",
        Description = "Identity & Access Management microservice"
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

// ── JWT Authentication ──
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
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

    });

// ── DI: Domain & Application ──
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();

// ── CORS (configurable whitelist) ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:4173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Database ──
builder.Services.AddDbContext<IamDbContext>(options =>
{
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

// ── Database initialization (dev convenience; disable in prod with Database:AutoCreate=false) ──
if (app.Configuration.GetValue("Database:AutoCreate", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        scope.ServiceProvider.GetRequiredService<IamDbContext>().Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "[IAM] Database initialization failed");
        throw;

    }
}

// ── Pipeline ──
app.MapGet("/", () => Results.Ok("NextHappen IAM Service is running"));
app.MapGet("/health", async (IamDbContext db) =>
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
