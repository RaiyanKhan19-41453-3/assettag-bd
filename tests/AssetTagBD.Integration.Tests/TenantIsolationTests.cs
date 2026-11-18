using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Application.Features.Users;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class TenantIsolationTests : IntegrationTestBase
{
    public TenantIsolationTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task List_OnlyReturnsAssetsFromCurrentTenant()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (tenantA, adminA) = await SeedTenantAsync($"Tenant A {suffix}", $"tenant-a-{suffix}", $"a-{suffix}@test.local");
        var (tenantB, adminB) = await SeedTenantAsync($"Tenant B {suffix}", $"tenant-b-{suffix}", $"b-{suffix}@test.local");

        ActAs(adminA, tenantA, UserRole.SuperAdmin, $"a-{suffix}@test.local");
        var createdA = await Send(new CreateAssetCommand(null, "A Secret Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        Assert.True(createdA.IsSuccess);

        ActAs(adminB, tenantB, UserRole.SuperAdmin, $"b-{suffix}@test.local");
        var listB = await Send(new GetAssetsQuery(null, null, null));
        Assert.True(listB.IsSuccess);
        Assert.DoesNotContain(listB.Value!.Items, asset => asset.Name == "A Secret Asset");

        var listA = await Send(new GetAssetsQuery(null, null, "A Secret Asset"));
        Assert.Empty(listA.Value!.Items);
    }

    [Fact]
    public async Task GetById_CrossTenant_ReturnsNotFound()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (tenantA, adminA) = await SeedTenantAsync($"Tenant C {suffix}", $"tenant-c-{suffix}", $"c-{suffix}@test.local");
        var (tenantB, adminB) = await SeedTenantAsync($"Tenant D {suffix}", $"tenant-d-{suffix}", $"d-{suffix}@test.local");

        ActAs(adminA, tenantA, UserRole.SuperAdmin, $"c-{suffix}@test.local");
        var created = await Send(new CreateAssetCommand(null, "Cross Tenant Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        ActAs(adminB, tenantB, UserRole.SuperAdmin, $"d-{suffix}@test.local");
        var result = await Send(new GetAssetByIdQuery(created.Value!.Id));

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetUsers_OnlyReturnsUsersFromCurrentTenant()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (tenantA, adminA) = await SeedTenantAsync($"Tenant E {suffix}", $"tenant-e-{suffix}", $"e-{suffix}@test.local");
        var (tenantB, _) = await SeedTenantAsync($"Tenant F {suffix}", $"tenant-f-{suffix}", $"f-{suffix}@test.local");
        await SeedUserAsync(tenantA, UserRole.Employee, $"emp-a-{suffix}@test.local");
        await SeedUserAsync(tenantB, UserRole.Employee, $"emp-b-{suffix}@test.local");

        ActAs(adminA, tenantA, UserRole.SuperAdmin, $"e-{suffix}@test.local");
        var result = await Send(new GetUsersQuery(UserRole.Employee));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Contains(result.Value!, user => user.Email == $"emp-a-{suffix}@test.local");
        Assert.DoesNotContain(result.Value!, user => user.Email == $"emp-b-{suffix}@test.local");
    }
}
