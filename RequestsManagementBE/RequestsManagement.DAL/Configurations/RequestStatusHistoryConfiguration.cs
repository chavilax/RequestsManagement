using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Configurations;

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("RequestStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedOnAdd();

        builder.Property(h => h.RequestId)
            .IsRequired();

        builder.Property(h => h.PreviousStatus)
            .HasConversion<int?>();

        builder.Property(h => h.NewStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.Property(h => h.ChangedBy)
            .IsRequired()
            .HasMaxLength(100);

        // שליפת היסטוריה לפי פנייה, ממוינת לפי זמן השינוי (דרישה §3)
        builder.HasIndex(h => new { h.RequestId, h.ChangedAt })
            .HasDatabaseName("IX_RequestStatusHistory_RequestId_ChangedAt");
    }
}
