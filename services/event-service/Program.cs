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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 32)),
        mysql => mysql.SchemaBehavior(MySqlSchemaBehavior.Ignore)
    );
});

var app = builder.Build();

// ── Auto-migrate ──
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

// ── Pipeline ──
app.MapGet("/", () => Results.Ok("NextHappen Event Service is running"));

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
