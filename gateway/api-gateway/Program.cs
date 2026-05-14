var builder = WebApplication.CreateBuilder(args);

// ── YARP Reverse Proxy ──
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Vite dev
                "http://localhost:3000",  // Alternate dev
                "http://localhost:4173"   // Vite preview
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ── Health Check ──
app.MapGet("/", () => Results.Ok(new
{
    service = "NextHappen API Gateway",
    status = "running",
    routes = new
    {
        iam = "http://localhost:5001",
        events = "http://localhost:5002",
        tickets = "http://localhost:5003",
        engagement = "http://localhost:5004",
        notifications = "http://localhost:5005"
    }
}));

app.UseCors("AllowFrontend");

app.MapReverseProxy();

app.Run();
