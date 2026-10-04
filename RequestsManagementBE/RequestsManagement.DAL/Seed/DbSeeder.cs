using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL.Context;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DAL.Seed;

/// <summary>
/// מייצר נתוני בדיקה. ניתן להרצה חוזרת - מנקה את הטבלאות ומייצר מחדש.
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _context;

    // קבוע ליצירת נתונים דטרמיניסטיים-למחצה (אפשר לשחזר תוצאות)
    private static readonly Random Random = new(12345);

    public DbSeeder(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// מנקה את הנתונים הקיימים ומייצר כמות חדשה.
    /// </summary>
    /// <param name="count">מספר הפניות לייצר (ברירת מחדל 100,000).</param>
    public async Task SeedAsync(int count = 100_000, CancellationToken cancellationToken = default)
    {
        await ClearAsync(cancellationToken);
        await GenerateRequestsAsync(count, cancellationToken);
    }

    /// <summary>
    /// ניקוי מהיר של הטבלאות באמצעות DELETE + RESEED של ה-Identity.
    /// (לא TRUNCATE כי קיים FK בין הטבלאות.)
    /// </summary>
    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM RequestStatusHistory; DELETE FROM Requests;", cancellationToken);

        await _context.Database.ExecuteSqlRawAsync(
            "DBCC CHECKIDENT ('Requests', RESEED, 0); DBCC CHECKIDENT ('RequestStatusHistory', RESEED, 0);",
            cancellationToken);
    }

    private async Task GenerateRequestsAsync(int count, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues<RequestStatus>();
        var priorities = Enum.GetValues<RequestPriority>();

        const int batchSize = 5_000;
        var batch = new List<Request>(batchSize);

        // טווח תאריכים: שנתיים אחורה עד היום
        var now = DateTime.UtcNow;
        var earliest = now.AddYears(-2);
        var totalDays = (now - earliest).Days;

        for (int i = 1; i <= count; i++)
        {
            var createdAt = earliest.AddDays(Random.Next(totalDays))
                                    .AddMinutes(Random.Next(1440));
            var status = statuses[Random.Next(statuses.Length)];

            // UpdatedAt אחרי CreatedAt (או שווה אם סטטוס New)
            var updatedAt = status == RequestStatus.New
                ? createdAt
                : createdAt.AddHours(Random.Next(1, 240));

            var request = new Request
            {
                Title = SeedData.TitleTemplates[Random.Next(SeedData.TitleTemplates.Length)],
                OrganizationName = SeedData.OrganizationNames[Random.Next(SeedData.OrganizationNames.Length)],
                Status = status,
                Priority = priorities[Random.Next(priorities.Length)],
                AssignedTo = SeedData.Handlers[Random.Next(SeedData.Handlers.Length)],
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // רשומת היסטוריה ראשונית - יצירת הפנייה
            request.StatusHistory.Add(new RequestStatusHistory
            {
                PreviousStatus = null,
                NewStatus = status,
                ChangedAt = createdAt,
                ChangedBy = "System (Seed)"
            });

            batch.Add(request);

            if (batch.Count >= batchSize)
            {
                await SaveBatchAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await SaveBatchAsync(batch, cancellationToken);
        }
    }

    private async Task SaveBatchAsync(List<Request> batch, CancellationToken cancellationToken)
    {
        _context.Requests.AddRange(batch);
        await _context.SaveChangesAsync(cancellationToken);

        // ניקוי ה-ChangeTracker כדי לשמור על ביצועים וזיכרון בכמות גדולה
        _context.ChangeTracker.Clear();
    }
}
