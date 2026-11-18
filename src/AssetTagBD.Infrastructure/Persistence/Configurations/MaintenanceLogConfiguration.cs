using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTagBD.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceLogConfiguration : IEntityTypeConfiguration<MaintenanceLog>
{
    public void Configure(EntityTypeBuilder<MaintenanceLog> builder)
    {
        builder.ToTable("MaintenanceLogs");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.IssueDescription)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Status)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.ResolvedAt);

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.HasIndex(m => m.TenantId);

        builder.HasOne(m => m.Asset)
            .WithMany(a => a.MaintenanceLogs)
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ReportedByUser)
            .WithMany(u => u.ReportedMaintenanceLogs)
            .HasForeignKey(m => m.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.AssetId);

        builder.HasIndex(m => m.Status);
    }
}
