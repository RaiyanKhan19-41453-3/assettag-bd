using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTagBD.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Actor).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Details).HasMaxLength(4000);
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => e.OccurredAt);
    }
}
