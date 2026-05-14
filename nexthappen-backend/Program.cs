using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;



// AssignStands
using nexthappen_backend.AssignStands.Application.Services;
using nexthappen_backend.AssignStands.Domain.Entities;
using nexthappen_backend.AssignStands.Infrastructure.Persistence.Repositories;

// Shared
using nexthappen_backend.Shared.Infrastructure.Interfaces.ASP.Configuration;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;

// CreateEvent
using nexthappen_backend.CreateEvent.Application.Services;
using nexthappen_backend.CreateEvent.Application.UseCases;
using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.CreateEvent.Infrastructure.Persistence.Repositories;

// ManageEvent
using nexthappen_backend.ManageEvent.Application.Services;
using nexthappen_backend.ManageEvent.Application.UseCases;
using nexthappen_backend.ManageEvent.Domain;
using nexthappen_backend.ManageEvent.Infrastructure.Repositories;

// EventDiscovery
using nexthappen_backend.EventDiscovery.Application.Services;
using nexthappen_backend.EventDiscovery.Application.UseCases;
using nexthappen_backend.EventDiscovery.Domain.Entities;
using nexthappen_backend.EventDiscovery.Infrastructure.Persistence.Repositories;

// Saved Events
using nexthappen_backend.SavedEvents.Domain.Entities;
using nexthappen_backend.SavedEvents.Infrastructure.Persistence.Repositories;
using nexthappen_backend.SavedEvents.Application.Services;
using nexthappen_backend.SavedEvents.Application.UseCases;

// Tickets
using nexthappen_backend.Tickets.Domain.Entities;
using nexthappen_backend.Tickets.Infrastructure.Persistence.Repositories;
using nexthappen_backend.Tickets.Application.Services;
using nexthappen_backend.Tickets.Application.UseCases;

// IAM
using nexthappen_backend.IAM.Domain.Services;
using nexthappen_backend.IAM.Infrastructure.Security;
using nexthappen_backend.IAM.Domain.Repositories;
using nexthappen_backend.IAM.Infrastructure.Persistence;
using nexthappen_backend.IAM.Application.UseCases;

// Metrics
using nexthappen_backend.Metrics.Domain;
using nexthappen_backend.Notifications.Application.Services;

// Notifications
using nexthappen_backend.Notifications.Domain;
using nexthappen_backend.Notifications.Infrastructure.Persistence.Repositories;


var builder = WebApplication.CreateBuilder(args);

// Routing
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Controllers
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new KebabCaseRouteNamingConvention());
});

// Swagger
builder.Services.AddSwaggerGen(options => 
{ 
    options.EnableAnnotations(); 
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT (no necesitas escribir 'Bearer', solo el token)"
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

// JWT CONFIG
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Repositorios y servicios sin duplicados
builder.Services.AddScoped<IManageEventRepository, ManageEventRepository>();
builder.Services.AddScoped<ManageEventService>();
builder.Services.AddScoped<UpdateEventHandler>();
builder.Services.AddScoped<DeleteEventHandler>();

builder.Services.AddScoped<IDiscoveryEventRepository, DiscoveryEventRepository>();
builder.Services.AddScoped<EventDiscoveryService>();
builder.Services.AddScoped<GetPublicEventsHandler>();

builder.Services.AddScoped<ISavedEventRepository, SavedEventRepository>();
builder.Services.AddScoped<SavedEventsService>();
builder.Services.AddScoped<GetSavedEventsHandler>();

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<TicketsService>();
builder.Services.AddScoped<PurchaseTicketHandler>();
builder.Services.AddScoped<GetUserTicketsHandler>();
builder.Services.AddScoped<GetTicketByIdHandler>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<CreateEventService>();

builder.Services.AddScoped<nexthappen_backend.ManageEvent.Application.UseCases.GetAllEventsHandler>();
builder.Services.AddScoped<nexthappen_backend.CreateEvent.Application.UseCases.GetAllEventsHandler>();
builder.Services.AddScoped<CreateEventHandler>();
builder.Services.AddScoped<GetEventByIdHandler>();

builder.Services.AddScoped<IAssignedStandRepository, AssignedStandRepository>();
builder.Services.AddScoped<AssignStandsService>();

builder.Services.AddScoped<IMetricRepository, MetricRepository>();
builder.Services.AddScoped<MetricsService>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationService>();


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 32)),
        mySqlOptions =>
        {
            mySqlOptions.MigrationsAssembly("nexthappen-backend");
            mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
        }
    );
});



var app = builder.Build();


// MIGRAR AUTOMÁTICAMENTE 
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration Error: " + ex.Message);
    }
}

// Root
app.MapGet("/", () => Results.Ok("NextHappen API is running"));

app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
