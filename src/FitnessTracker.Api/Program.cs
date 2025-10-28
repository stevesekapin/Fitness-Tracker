using Microsoft.OpenApi.Models;
using FitnessTracker.Api.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------
// 1️⃣ Configure Services
// ------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Jordan Fitness API",
        Version = "v1",
        Description = "A RESTful API for tracking workouts, calories, and summaries — Jordan Edition."
    });
});

var app = builder.Build();

// ------------------------------------------------
// 2️⃣ Enable Swagger Always
// ------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jordan Fitness API v1");
    c.RoutePrefix = string.Empty; // ✅ Swagger loads at root (http://localhost:8080)
});

// ------------------------------------------------
// 3️⃣ Middleware
// ------------------------------------------------
app.UseHttpsRedirection();

// ------------------------------------------------
// 4️⃣ Persistent Data Setup
// ------------------------------------------------
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
var calorieFile = Path.Combine(dataDir, "calorieLogs.json");
var exerciseFile = Path.Combine(dataDir, "exerciseLogs.json");
var summaryFile = Path.Combine(dataDir, "summaries.json");

var calorieLogs = new List<CalorieLog>();
var exerciseLogs = new List<ExerciseLog>();
var summaries = new List<DailySummary>();

if (File.Exists(calorieFile))
    calorieLogs = JsonSerializer.Deserialize<List<CalorieLog>>(File.ReadAllText(calorieFile)) ?? new();

if (File.Exists(exerciseFile))
    exerciseLogs = JsonSerializer.Deserialize<List<ExerciseLog>>(File.ReadAllText(exerciseFile)) ?? new();

if (File.Exists(summaryFile))
    summaries = JsonSerializer.Deserialize<List<DailySummary>>(File.ReadAllText(summaryFile)) ?? new();

void SaveData<T>(string path, List<T> data)
{
    if (!Directory.Exists(dataDir))
        Directory.CreateDirectory(dataDir);

    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
}

// ------------------------------------------------
// 5️⃣ API Endpoints
// ------------------------------------------------

// ✅ Health Check
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ✅ Add Calorie Log
app.MapPost("/calories/log", (CalorieLog log) =>
{
    if (string.IsNullOrWhiteSpace(log.MealName))
        return Results.BadRequest(new { message = "Meal name is required." });
    if (log.Calories <= 0)
        return Results.BadRequest(new { message = "Calories must be greater than 0." });

    log.Date = DateTime.Now;
    calorieLogs.Add(log);
    SaveData(calorieFile, calorieLogs);
    return Results.Created("/calories/log", log);
});

// ✅ View Calorie Logs
app.MapGet("/calories/history", () =>
    !calorieLogs.Any()
        ? Results.Ok(new { message = "No calorie logs yet." })
        : Results.Ok(calorieLogs)
);

// ✅ Add Exercise Log
app.MapPost("/exercises/log", (ExerciseLog log) =>
{
    if (string.IsNullOrWhiteSpace(log.Workout))
        return Results.BadRequest(new { message = "Workout name is required." });
    if (log.Duration <= 0)
        return Results.BadRequest(new { message = "Duration must be greater than 0." });

    log.Date = DateTime.Now;
    exerciseLogs.Add(log);
    SaveData(exerciseFile, exerciseLogs);
    return Results.Created("/exercises/log", log);
});

// ✅ View Exercise Logs
app.MapGet("/exercises/history", () =>
    !exerciseLogs.Any()
        ? Results.Ok(new { message = "No exercise logs yet." })
        : Results.Ok(exerciseLogs)
);

// ✅ Combined Daily Summary
app.MapGet("/summary", () =>
{
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

    SaveData(summaryFile, summaries);
    return Results.Ok(summary);
});

// ✅ Summary History
app.MapGet("/summary/history", () =>
    !summaries.Any()
        ? Results.Ok(new { message = "No summaries saved yet." })
        : Results.Ok(summaries)
);

// ✅ Root Welcome
app.MapGet("/", () =>
    Results.Redirect("/index.html")
);

// ------------------------------------------------
// 6️⃣ Run App
// ------------------------------------------------
app.Run();
