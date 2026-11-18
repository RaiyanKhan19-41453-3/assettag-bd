using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using AssetTagBD.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public abstract class IntegrationTestBase
{
    protected readonly TestFixture Fixture;

    protected IntegrationTestBase(TestFixture fixture)
    {
        Fixture = fixture;
    }

    protected async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var scope = Fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    protected async Task<(Guid TenantId, Guid AdminId)> SeedTenantAsync(string name, string slug, string adminEmail)
    {
        using var scope = Fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var tenant = new Tenant { Name = name, Slug = slug };
        var admin = new User
        {
            FullName = $"{name} Admin",
            Email = adminEmail,
            PasswordHash = hasher.Hash("Admin@123"),
            Role = UserRole.SuperAdmin,
            TenantId = tenant.Id,
            Tenant = tenant
        };

        db.Tenants.Add(tenant);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        return (tenant.Id, admin.Id);
    }

    protected async Task<Guid> SeedUserAsync(Guid tenantId, UserRole role, string email)
    {
        using var scope = Fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            FullName = email.Split('@')[0],
            Email = email,
            PasswordHash = hasher.Hash("Employee@123"),
            Role = role,
            TenantId = tenantId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    protected async Task<int> CountAssetsAsync()
    {
        using var scope = Fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Assets.CountAsync();
    }

    protected void ActAs(Guid userId, Guid tenantId, UserRole role, string email) =>
        Fixture.CurrentUser.ActAs(userId, tenantId, role.ToString(), email);
}
