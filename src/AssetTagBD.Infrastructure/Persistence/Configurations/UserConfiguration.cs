using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTagBD.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired();

        builder.Property(u => u.Department)
            .HasMaxLength(150);

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.HasIndex(u => u.TenantId);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // A user who has ever been involved in an assignment or maintenance
        // ticket must not be deletable accidentally - keep an audit trail.
        builder.HasMany(u => u.AssignedAssets)
            .WithOne(a => a.AssignedToUser)
            .HasForeignKey(a => a.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.MadeAssignments)
            .WithOne(a => a.AssignedByUser)
            .HasForeignKey(a => a.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReportedMaintenanceLogs)
            .WithOne(m => m.ReportedByUser)
            .HasForeignKey(m => m.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
