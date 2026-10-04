using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.DAL.Context;

namespace RequestsManagement.Tests;

/// <summary>
/// מריץ את ה-API השלם בזיכרון לצורכי Integration Testing,
/// אך מחליף את ה-DB מ-SQL Server ל-SQLite in-memory.
/// SQLite נבחר (ולא EF InMemory) כי הוא תומך ב-RowVersion האמיתי,
/// וכך ניתן לבדוק את ה-Optimistic Concurrency מקצה לקצה.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // סביבת Testing - גורמת ל-Program לדלג על רישום ה-SQL Server DbContext
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // חיבור SQLite in-memory - נשאר פתוח למשך כל חיי ה-factory
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // יצירת הסכמה
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
