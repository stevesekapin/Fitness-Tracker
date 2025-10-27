using Microsoft.OpenApi.Models;
using FitnessTracker.Api.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// 1️⃣ Configure Services
// ----------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fitness Tracker API",
        Version = "v1",
        Description = "A RESTful API for tracking calories, exercises, and daily progress with historical summaries and file persistence."
    });
});

var app = builder.Build();

// ----------------------
// 2️⃣ Enable Swagger Always
// ----------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fitness Tracker API v1");
    c.RoutePrefix = "swagger";
});

// ----------------------
// 3️⃣ Middleware
// ----------------------
app.UseHttpsRedirection();

// ----------------------
// 4️⃣ Persistent Data Setup
// ----------------------

// File paths
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
var calorieFile = Path.Combine(dataDir, "calorieLogs.json");
var exerciseFile = Path.Combine(dataDir, "exerciseLogs.json");
var summaryFile = Path.Combine(dataDir, "summaries.json");

// In-memory data
var calorieLogs = new List<CalorieLog>();
var exerciseLogs = new List<ExerciseLog>();
var summaries = new List<DailySummary>();

// Load data on startup
if (File.Exists(calorieFile))
    calorieLogs = JsonSerializer.Deserialize<List<CalorieLog>>(File.ReadAllText(calorieFile)) ?? new();

if (File.Exists(exerciseFile))
    exerciseLogs = JsonSerializer.Deserialize<List<ExerciseLog>>(File.ReadAllText(exerciseFile)) ?? new();

if (File.Exists(summaryFile))
    summaries = JsonSerializer.Deserialize<List<DailySummary>>(File.ReadAllText(summaryFile)) ?? new();

// Helper method to save data
void SaveData<T>(string path, List<T> data)
{
    if (!Directory.Exists(dataDir))
        Directory.CreateDirectory(dataDir);

    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
}

// ----------------------
// 5️⃣ Endpoints
// ----------------------

// ✅ Health Check
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ✅ Add Calorie Log with Validation
app.MapPost("/calories/log", (CalorieLog log) =>
{
    if (string.IsNullOrWhiteSpace(log.MealName))
        return Results.BadRequest(new { message = "Meal name is required." });

    if (log.Calories <= 0)
        return Results.BadRequest(new { message = "Calories must be greater than 0." });

    log.Date = DateTime.Now;
    calorieLogs.Add(log);
    SaveData(calorieFile, calorieLogs);

    return Results.Created("/calories/log", new
    {
        message = "Calorie log added successfully!",
        log
    });
});

// ✅ View All Calorie Logs
app.MapGet("/calories/history", () =>
{
    if (!calorieLogs.Any())
        return Results.Ok(new { message = "No calorie logs yet." });

    return Results.Ok(calorieLogs);
});

// ✅ Daily Calorie Progress
app.MapGet("/progress/daily", () =>
{
    if (!calorieLogs.Any())
        return Results.Ok(new { message = "No logs to summarize." });

    var totalCalories = calorieLogs.Sum(c => c.Calories);
    return Results.Ok(new
    {
        totalCalories,
        totalMeals = calorieLogs.Count,
        lastMeal = calorieLogs.Last().MealName,
        lastCalories = calorieLogs.Last().Calories,
        lastDate = calorieLogs.Last().Date
    });
});

// ----------------------
// 🏋️ Exercise Tracker
// ----------------------

// ✅ Add Exercise Log with Validation
app.MapPost("/exercises/log", (ExerciseLog log) =>
{
    if (string.IsNullOrWhiteSpace(log.Workout))
        return Results.BadRequest(new { message = "Workout name is required." });

    if (log.Duration <= 0)
        return Results.BadRequest(new { message = "Duration must be greater than 0." });

    log.Date = DateTime.Now;
    exerciseLogs.Add(log);
    SaveData(exerciseFile, exerciseLogs);

    return Results.Created("/exercises/log", new
    {
        message = "Exercise log added successfully!",
        log
    });
});

// ✅ View All Exercise Logs
app.MapGet("/exercises/history", () =>
{
    if (!exerciseLogs.Any())
        return Results.Ok(new { message = "No exercise logs yet." });

    return Results.Ok(exerciseLogs);
});

// ✅ Exercise Progress Summary
app.MapGet("/exercises/summary", () =>
{
    if (!exerciseLogs.Any())
        return Results.Ok(new { message = "No workouts recorded." });

    var totalDuration = exerciseLogs.Sum(e => e.Duration);
    return Results.Ok(new
    {
        totalDuration,
        totalWorkouts = exerciseLogs.Count,
        lastWorkout = exerciseLogs.Last().Workout,
        lastDuration = exerciseLogs.Last().Duration,
        lastDate = exerciseLogs.Last().Date
    });
});

// ✅ Combined Daily Summary with Historical Storage + Persistence
app.MapGet("/summary", () =>
{
    if (!calorieLogs.Any() && !exerciseLogs.Any())
        return Results.Ok(new { message = "No calorie or exercise logs found." });

    var today = DateTime.Now.ToString("yyyy-MM-dd");
    var totalCalories = calorieLogs.Sum(c => c.Calories);
    var totalMeals = calorieLogs.Count;
    var totalDuration = exerciseLogs.Sum(e => e.Duration);
    var totalWorkouts = exerciseLogs.Count;

    var summary = new DailySummary
    {
        Date = today,
        TotalCalories = totalCalories,
        TotalMeals = totalMeals,
        TotalDuration = totalDuration,
        TotalWorkouts = totalWorkouts,
        LastMeal = calorieLogs.LastOrDefault()?.MealName ?? "N/A",
        LastWorkout = exerciseLogs.LastOrDefault()?.Workout ?? "N/A"
    };

    // Save or update today's summary
    var existing = summaries.FirstOrDefault(s => s.Date == today);
    if (existing == null)
        summaries.Add(summary);
    else
    {
        existing.TotalCalories = totalCalories;
        existing.TotalMeals = totalMeals;
        existing.TotalDuration = totalDuration;
        existing.TotalWorkouts = totalWorkouts;
        existing.LastMeal = summary.LastMeal;
        existing.LastWorkout = summary.LastWorkout;
    }

    // Persist summaries
    SaveData(summaryFile, summaries);

    return Results.Ok(new
    {
        message = "Daily summary generated successfully.",
        summary
    });
});

// ✅ View All Saved Summaries
app.MapGet("/summary/history", () =>
{
    if (!summaries.Any())
        return Results.Ok(new { message = "No summaries saved yet." });

    return Results.Ok(summaries);
});

// ✅ View Summary by Date (e.g., /summary/2025-10-27)
app.MapGet("/summary/{date}", (string date) =>
{
    var result = summaries.FirstOrDefault(s => s.Date == date);
    if (result == null)
        return Results.NotFound(new { message = $"No summary found for {date}." });

    return Results.Ok(result);
});

// ----------------------
// 6️⃣ Run App
// ----------------------
app.Run();
