using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Infrastructure.Configuration;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTagBD.Infrastructure.Persistence;

/// <summary>
/// Idempotent seeding. Ensures a default tenant and SuperAdmin exist so a fresh
/// database is usable. Sample data is opt-in via Seed:SampleData.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var db = scopedProvider.GetRequiredService<AppDbContext>();
        var configuration = scopedProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scopedProvider.GetRequiredService<IPasswordHasher>();

        // The seeder runs outside a request, so the tenant query filter is null.
        // IgnoreQueryFilters() is required to inspect existing data.
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == "assettagbd-default");

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = "AssetTag BD",
                Slug = "assettagbd-default"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var adminEmail = (configuration["Seed:AdminEmail"] ?? "admin@assettagbd.local").ToLowerInvariant();

        if (!await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            db.Users.Add(new User
            {
                FullName = "System Administrator",
                Email = adminEmail,
                PasswordHash = passwordHasher.Hash(
                    SecretReader.Read(configuration, "Seed:AdminPassword") ?? "Admin@123"),
                Role = UserRole.SuperAdmin,
                Department = "IT",
                TenantId = tenant.Id
            });
            await db.SaveChangesAsync();
        }

        var seedSampleData = configuration.GetValue<bool>("Seed:SampleData");
        if (seedSampleData && !await db.Assets.IgnoreQueryFilters().AnyAsync())
        {
            await SeedSampleDataAsync(db, passwordHasher, tenant.Id, adminEmail);
        }
    }

    private static async Task SeedSampleDataAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        Guid tenantId,
        string adminEmail)
    {
        var admin = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == adminEmail);

        var manager = new User
        {
            FullName = "Rahim Ahmed",
            Email = "rahim@assettagbd.local",
            PasswordHash = hasher.Hash("Manager@123"),
            Role = UserRole.ITManager,
            Department = "IT",
            TenantId = tenantId
        };

        var employees = new[]
        {
            new User { FullName = "Sadia Islam", Email = "sadia@assettagbd.local", PasswordHash = hasher.Hash("Employee@123"), Role = UserRole.Employee, Department = "Finance", TenantId = tenantId },
            new User { FullName = "Karim Hassan", Email = "karim@assettagbd.local", PasswordHash = hasher.Hash("Employee@123"), Role = UserRole.Employee, Department = "Sales", TenantId = tenantId },
            new User { FullName = "Nusrat Jahan", Email = "nusrat@assettagbd.local", PasswordHash = hasher.Hash("Employee@123"), Role = UserRole.Employee, Department = "HR", TenantId = tenantId }
        };

        db.Users.Add(manager);
        db.Users.AddRange(employees);

        var assets = new[]
        {
            new Asset { AssetTag = "ATBD-LAP-2026-000001", Name = "Dell Latitude 5440", Category = AssetCategory.Laptop, SerialNumber = "DL5440-001", PurchaseDate = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc), WarrantyExpiryDate = new DateTime(2028, 2, 10, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Assigned, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-LAP-2026-000002", Name = "Lenovo ThinkPad E14", Category = AssetCategory.Laptop, SerialNumber = "LTE14-002", PurchaseDate = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc), WarrantyExpiryDate = new DateTime(2028, 3, 5, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Available, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-MON-2026-000003", Name = "LG UltraWide 29\"", Category = AssetCategory.Monitor, SerialNumber = "LG29-003", PurchaseDate = new DateTime(2024, 11, 20, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Assigned, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-MON-2026-000004", Name = "Samsung 24\" LED", Category = AssetCategory.Monitor, SerialNumber = "SAM24-004", PurchaseDate = new DateTime(2024, 11, 20, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Available, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-PHO-2026-000005", Name = "iPhone 13", Category = AssetCategory.Phone, SerialNumber = "IP13-005", PurchaseDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), WarrantyExpiryDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.UnderMaintenance, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-PHO-2026-000006", Name = "Samsung Galaxy A54", Category = AssetCategory.Phone, SerialNumber = "SGA54-006", PurchaseDate = new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Available, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-FUR-2026-000007", Name = "Herman Miller Chair", Category = AssetCategory.Furniture, PurchaseDate = new DateTime(2023, 9, 12, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Assigned, TenantId = tenantId },
            new Asset { AssetTag = "ATBD-OTH-2026-000008", Name = "Logitech MX Master 3S", Category = AssetCategory.Other, PurchaseDate = new DateTime(2025, 1, 8, 0, 0, 0, DateTimeKind.Utc), Status = AssetStatus.Retired, TenantId = tenantId }
        };

        db.Assets.AddRange(assets);

        db.Assignments.AddRange(
            new Assignment { Asset = assets[0], AssignedToUser = employees[0], AssignedByUser = admin, AssignedDate = DateTime.UtcNow.AddDays(-45), ConditionNotes = "Issued at onboarding", TenantId = tenantId },
            new Assignment { Asset = assets[2], AssignedToUser = employees[1], AssignedByUser = manager, AssignedDate = DateTime.UtcNow.AddDays(-20), TenantId = tenantId },
            new Assignment { Asset = assets[6], AssignedToUser = employees[2], AssignedByUser = manager, AssignedDate = DateTime.UtcNow.AddDays(-120), ReturnedDate = DateTime.UtcNow.AddDays(-10), ConditionNotes = "Returned in good condition", TenantId = tenantId });

        db.MaintenanceLogs.AddRange(
            new MaintenanceLog { Asset = assets[4], ReportedByUser = manager, IssueDescription = "Battery drains within two hours", Status = MaintenanceStatus.InProgress, CreatedAt = DateTime.UtcNow.AddDays(-3), TenantId = tenantId },
            new MaintenanceLog { Asset = assets[7], ReportedByUser = admin, IssueDescription = "Scroll wheel stopped working", Status = MaintenanceStatus.Resolved, CreatedAt = DateTime.UtcNow.AddDays(-30), ResolvedAt = DateTime.UtcNow.AddDays(-25), TenantId = tenantId });

        await db.SaveChangesAsync();
    }
}
