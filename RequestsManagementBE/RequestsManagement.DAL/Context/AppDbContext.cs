using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Request> Requests => Set<Request>();

    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // טעינת כל ה-IEntityTypeConfiguration מהאסמבלי של ה-DAL
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite (בבדיקות) לא מייצר rowversion אוטומטית. מסמנים את המאפיין
        // כ"לא נוצר אוטומטית" כדי שהערך שאנו מגדירים ידנית (HandleSqliteRowVersion) יישלח.
        if (Database.ProviderName!.Contains("Sqlite"))
        {
            modelBuilder.Entity<Request>()
                .Property(r => r.RowVersion)
                .ValueGeneratedNever();
        }
    }

    public override int SaveChanges()
    {
        HandleSqliteRowVersion();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        HandleSqliteRowVersion();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// SQL Server מנהל את RowVersion אוטומטית. SQLite (בשימוש בבדיקות) לא תומך בכך,
    /// לכן מייצרים כאן ערך RowVersion ידני רק עבור provider זה. ב-SQL Server לא מתבצע כלום.
    /// </summary>
    private void HandleSqliteRowVersion()
    {
        if (!Database.ProviderName!.Contains("Sqlite"))
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<Request>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                // ערך חדש ועולה בכל שמירה - מדמה את התנהגות ה-rowversion
                entry.Entity.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            }
        }
    }
}
