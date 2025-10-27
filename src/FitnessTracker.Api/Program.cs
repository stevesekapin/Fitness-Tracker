using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fitness Tracker API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithName("HealthCheck");

// --- Auth (placeholder) ---
app.MapPost("/auth/register", () => Results.Created("/auth/register", new { message = "TODO: create user" }));
app.MapPost("/auth/login", () => Results.Ok(new { token = "TODO: jwt" }));
app.MapPost("/auth/logout", () => Results.NoContent());

// --- Calorie Intake / Exercise Logs (placeholders) ---
app.MapPost("/calories/log", () => Results.Created("/calories/log", new { message = "TODO: save intake" }));
app.MapPost("/exercises/log", () => Results.Created("/exercises/log", new { message = "TODO: save exercise" }));

// --- Progress/History (placeholders) ---
app.MapGet("/progress/daily", () => Results.Ok(new { totalConsumed = 0, totalBurned = 0 }));
app.MapGet("/progress/weekly", () => Results.Ok(new { }));
app.MapGet("/progress/monthly", () => Results.Ok(new { }));

// --- Exercise Suggestions (placeholder) ---
app.MapPost("/suggestions", () => Results.Ok(new { plan = "TODO: compute suggestions" }));

app.Run();

// Make implicit Program discoverable by WebApplicationFactory for tests
public partial class Program { }
