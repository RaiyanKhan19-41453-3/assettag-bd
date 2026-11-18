using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Application.Features.Assignments;
using AssetTagBD.Domain.Enums;
using FluentValidation;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class PaginationAndMyAssetsTests : IntegrationTestBase
{
    public PaginationAndMyAssetsTests(TestFixture fixture) : base(fixture)
    {
    }

    private async Task<(Guid TenantId, Guid AdminId, string AdminEmail)> ArrangeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"page-{suffix}@test.local";
        var (tenantId, adminId) = await SeedTenantAsync($"Page Co {suffix}", $"page-{suffix}", email);
        ActAs(adminId, tenantId, UserRole.SuperAdmin, email);
        return (tenantId, adminId, email);
    }

    [Fact]
    public async Task GetAssets_PaginatesAndSorts()
    {
        await ArrangeAsync();
        foreach (var name in new[] { "Zebra Chair", "Alpha Laptop", "Monitor 24", "Phone X", "Desk Lamp" })
        {
            await Send(new CreateAssetCommand(null, name, AssetCategory.Other, null, DateTime.UtcNow, null));
        }

        var page2 = await Send(new GetAssetsQuery(null, null, null, 2, 2, "name", false));

        Assert.True(page2.IsSuccess);
        Assert.Equal(2, page2.Value!.Items.Count);
        Assert.Equal(5, page2.Value.TotalCount);
        Assert.Equal(3, page2.Value.TotalPages);
        Assert.Equal("Monitor 24", page2.Value.Items[0].Name);
    }

    [Fact]
    public async Task GetAssets_ExtremePageNumber_DoesNotFail()
    {
        await ArrangeAsync();

        var threw = false;
        try
        {
            await Send(new GetAssetsQuery(null, null, null, int.MaxValue, 100));
        }
        catch (ValidationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public async Task GetMyAssets_ReturnsOnlyActiveAssignmentsForCurrentUser()
    {
        var (tenantId, adminId, adminEmail) = await ArrangeAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var employeeA = await SeedUserAsync(tenantId, UserRole.Employee, $"emp-a-{suffix}@test.local");
        var employeeB = await SeedUserAsync(tenantId, UserRole.Employee, $"emp-b-{suffix}@test.local");

        var asset = await Send(new CreateAssetCommand(null, "Personal Laptop", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employeeA, null));

        ActAs(employeeA, tenantId, UserRole.Employee, $"emp-a-{suffix}@test.local");
        var mineA = await Send(new GetMyAssetsQuery());
        Assert.Single(mineA.Value!);
        Assert.False(string.IsNullOrWhiteSpace(mineA.Value![0].QrCodeBase64));

        ActAs(employeeB, tenantId, UserRole.Employee, $"emp-b-{suffix}@test.local");
        var mineB = await Send(new GetMyAssetsQuery());
        Assert.Empty(mineB.Value!);

        ActAs(adminId, tenantId, UserRole.SuperAdmin, adminEmail);
        await Send(new ReturnAssetCommand(asset.Value.Id, null));

        ActAs(employeeA, tenantId, UserRole.Employee, $"emp-a-{suffix}@test.local");
        var afterReturn = await Send(new GetMyAssetsQuery());
        Assert.Empty(afterReturn.Value!);
    }
}
