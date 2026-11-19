using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTagBD.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetTag)
            .IsRequired()
            .HasMaxLength(100);

        // The tag is the QR payload - unique per tenant, not globally, so two
        // companies can independently use the same human-friendly tag.
        builder.HasIndex(a => new { a.TenantId, a.AssetTag })
            .IsUnique();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Category)
            .IsRequired();

        builder.Property(a => a.SerialNumber)
            .HasMaxLength(100);

        builder.Property(a => a.PurchaseDate)
            .IsRequired();

        builder.Property(a => a.WarrantyExpiryDate);

        builder.Property(a => a.Status)
            .IsRequired();

        builder.HasIndex(a => a.Status);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.HasIndex(a => a.TenantId);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.DeletedAt);

        // History rows belong to their asset, so remove them with it.
        builder.HasMany(a => a.Assignments)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.MaintenanceLogs)
            .WithOne(m => m.Asset)
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
