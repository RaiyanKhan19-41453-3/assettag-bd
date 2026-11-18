using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context for AssetTag BD. Every tenant-owned entity has a
/// global query filter, so a user can never read another organisation's data —
/// even if a developer forgets to add a Where clause in a handler.
/// </summary>
public sealed class AppDbContext : DbContext, IUnitOfWork
{
    private readonly Guid? _currentTenantId;

    // Used by `dotnet ef` at design time. No tenant is available then.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Used by dependency injection at runtime.
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentTenantId = currentUser.TenantId;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // The filter captures this context instance, so EF evaluates _currentTenantId
        // per request rather than baking the first request's tenant into the model.
        modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Asset>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Assignment>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<MaintenanceLog>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<PasswordResetToken>().HasQueryFilter(e => e.TenantId == _currentTenantId);
    }
}
