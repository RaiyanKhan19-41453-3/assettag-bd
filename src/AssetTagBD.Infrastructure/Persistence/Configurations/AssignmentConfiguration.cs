using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTagBD.Infrastructure.Persistence.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssignedDate)
            .IsRequired();

        builder.Property(a => a.ReturnedDate);

        builder.Property(a => a.ConditionNotes)
            .HasMaxLength(1000);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.HasIndex(a => a.TenantId);

        builder.HasOne(a => a.Asset)
            .WithMany(asset => asset.Assignments)
            .HasForeignKey(a => a.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.AssignedToUser)
            .WithMany(user => user.AssignedAssets)
            .HasForeignKey(a => a.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedByUser)
            .WithMany(user => user.MadeAssignments)
            .HasForeignKey(a => a.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.AssetId);

        // Database-level guard: an asset can have at most one active assignment,
        // even if two assign requests race past the handler's status check.
        builder.HasIndex(a => a.AssetId)
            .IsUnique()
            .HasFilter("\"ReturnedDate\" IS NULL")
            .HasDatabaseName("IX_Assignments_AssetId_Active");

        builder.HasIndex(a => a.AssignedToUserId);
    }
}
