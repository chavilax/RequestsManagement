using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.OrganizationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.AssignedTo)
            .HasMaxLength(100);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        // Concurrency token - SQL Server rowversion.
        // מתעדכן אוטומטית בכל UPDATE ומשמש את EF Core לזיהוי עדכונים מתחרים.
        builder.Property(r => r.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // --- אינדקסים לתמיכה בסינון, מיון וחיפוש (דרישה §1, §6) ---

        // סינון לפי סטטוס + מיון לפי תאריך (התרחיש הנפוץ ביותר)
        builder.HasIndex(r => new { r.Status, r.CreatedAt })
            .HasDatabaseName("IX_Requests_Status_CreatedAt");

        // סינון לפי עדיפות
        builder.HasIndex(r => r.Priority)
            .HasDatabaseName("IX_Requests_Priority");

        // סינון לפי מטפל
        builder.HasIndex(r => r.AssignedTo)
            .HasDatabaseName("IX_Requests_AssignedTo");

        // חיפוש/סינון לפי שם ארגון
        builder.HasIndex(r => r.OrganizationName)
            .HasDatabaseName("IX_Requests_OrganizationName");

        // מיון/סינון לפי תאריך עדכון
        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Requests_CreatedAt");

        builder.HasMany(r => r.StatusHistory)
            .WithOne(h => h.Request)
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
