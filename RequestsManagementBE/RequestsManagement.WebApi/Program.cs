using Microsoft.EntityFrameworkCore;
using RequestsManagement.BL.Interfaces;
using RequestsManagement.BL.Services;
using RequestsManagement.DAL.Context;
using RequestsManagement.DAL.Repositories;
using RequestsManagement.DAL.Seed;
using RequestsManagement.WebApi.Middleware;
using RequestsManagement.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Controllers ---
builder.Services.AddControllers();

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Database (EF Core + SQL Server) ---
// בסביבת Testing ה-DbContext נרשם ע"י ה-WebApplicationFactory (SQLite), אז מדלגים כאן.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")));
}

// --- Seeder ---
builder.Services.AddScoped<DbSeeder>();

// --- Cache (IMemoryCache עבור ה-Aggregations) ---
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IStatsCache, MemoryStatsCache>();

// --- Application services (DAL + BL) ---
builder.Services.AddScoped<IRequestDAL, RequestDAL>();
builder.Services.AddScoped<IRequestBL, RequestBL>();

// --- CORS (עבור ה-Angular client) ---
const string AngularCorsPolicy = "AngularClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --- Seed mode ---
// הרצה: dotnet run -- --seed [count]
// מייצר נתוני בדיקה (ברירת מחדל 100,000) ויוצא מבלי להפעיל את השרת.
if (args.Contains("--seed"))
{
    await RunSeedAsync(app, args);
    return;
}

// --- HTTP pipeline ---

// טיפול מרכזי בשגיאות - ראשון ב-pipeline כדי לתפוס הכל
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(AngularCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

// --- Seed helper ---
static async Task RunSeedAsync(WebApplication app, string[] args)
{
    // כמות אופציונלית אחרי --seed, לדוגמה: --seed 50000
    var count = 100_000;
    var seedIndex = Array.IndexOf(args, "--seed");
    if (seedIndex >= 0 && seedIndex + 1 < args.Length && int.TryParse(args[seedIndex + 1], out var parsed))
    {
        count = parsed;
    }

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();

    logger.LogInformation("Seeding {Count} requests...", count);
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    await seeder.SeedAsync(count);

    stopwatch.Stop();
    logger.LogInformation("Seed completed: {Count} requests in {Seconds:F1}s",
        count, stopwatch.Elapsed.TotalSeconds);
}

// נדרש כדי ש-WebApplicationFactory בבדיקות ה-Integration יוכל להתייחס ל-Program
public partial class Program { }
