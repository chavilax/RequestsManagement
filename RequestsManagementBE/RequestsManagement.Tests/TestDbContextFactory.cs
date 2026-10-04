using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL.Context;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DTO.Enums;

namespace RequestsManagement.Tests;

/// <summary>
/// עוזר ליצירת AppDbContext מבוסס זיכרון (InMemory) לבדיקות.
/// כל בדיקה מקבלת DB נקי ומבודד (שם ייחודי) כדי שבדיקות לא ישפיעו זו על זו.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// יוצר DB עם מספר פניות לדוגמה, מגוונות בסטטוס ובעדיפות.
    /// </summary>
    public static AppDbContext CreateWithSeed()
    {
        var context = Create();

        var requests = new List<Request>
        {
            MakeRequest("בקשה א", "מקורות", RequestStatus.New, RequestPriority.High, "דנה"),
            MakeRequest("בקשה ב", "בזק", RequestStatus.New, RequestPriority.Low, "יוסי"),
            MakeRequest("בקשה ג", "מקורות", RequestStatus.InProgress, RequestPriority.Medium, "דנה"),
            MakeRequest("בקשה ד", "סלקום", RequestStatus.Completed, RequestPriority.High, null),
            MakeRequest("בקשה ה", "בזק", RequestStatus.Waiting, RequestPriority.Medium, "מאיה"),
        };

        context.Requests.AddRange(requests);
        context.SaveChanges();

        return context;
    }

    private static Request MakeRequest(
        string title, string org, RequestStatus status, RequestPriority priority, string? assignedTo)
    {
        return new Request
        {
            Title = title,
            OrganizationName = org,
            Status = status,
            Priority = priority,
            AssignedTo = assignedTo,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };
    }
}
