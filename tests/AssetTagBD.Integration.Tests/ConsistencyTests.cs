using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Application.Features.Auth;
using AssetTagBD.Application.Features.Assignments;
using AssetTagBD.Application.Features.Maintenance;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class ConsistencyTests : IntegrationTestBase
{
    public ConsistencyTests(TestFixture fixture) : base(fixture)
    {
    }

    private async Task<(Guid TenantId, Guid AdminId, string Email)> ArrangeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"consistency-{suffix}@test.local";
        var (tenantId, adminId) = await SeedTenantAsync($"Consistency {suffix}", $"consistency-{suffix}", email);
        ActAs(adminId, tenantId, UserRole.SuperAdmin, email);
        return (tenantId, adminId, email);
    }

    private static UpdateAssetCommand Update(
        Guid id,
        AssetStatus status,
        string name = "Updated Asset") =>
        new(id, name, AssetCategory.Laptop, null, DateTime.UtcNow, null, status);

    [Fact]
    public async Task UpdateAsset_AssignedWithoutAssignment_IsRejected()
    {
        await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Free Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        var result = await Send(Update(asset.Value!.Id, AssetStatus.Assigned));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsset_AvailableWithActiveAssignment_IsRejected()
    {
        var (tenantId, _, _) = await ArrangeAsync();
        var employee = await SeedUserAsync(tenantId, UserRole.Employee, $"emp-{Guid.NewGuid():N}@test.local");
        var asset = await Send(new CreateAssetCommand(null, "Held Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employee, null));

        var result = await Send(Update(asset.Value.Id, AssetStatus.Available));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsset_WithActiveAssignment_IsRejected()
    {
        var (tenantId, _, _) = await ArrangeAsync();
        var employee = await SeedUserAsync(tenantId, UserRole.Employee, $"del-{Guid.NewGuid():N}@test.local");
        var asset = await Send(new CreateAssetCommand(null, "Keep Me", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employee, null));

        var result = await Send(new DeleteAssetCommand(asset.Value.Id));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsset_UnderMaintenanceWithoutTicket_IsRejected()
    {
        await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Healthy", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        var result = await Send(Update(asset.Value!.Id, AssetStatus.UnderMaintenance));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task ReturnAsset_WithOpenTicket_KeepsUnderMaintenanceStatus()
    {
        var (tenantId, _, _) = await ArrangeAsync();
        var employee = await SeedUserAsync(tenantId, UserRole.Employee, $"ret-{Guid.NewGuid():N}@test.local");
        var asset = await Send(new CreateAssetCommand(null, "Damaged", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employee, null));
        await Send(new CreateMaintenanceLogCommand(asset.Value.Id, "Broken screen"));

        var returned = await Send(new ReturnAssetCommand(asset.Value.Id, null));
        Assert.True(returned.IsSuccess);

        var after = await Send(new GetAssetByIdQuery(asset.Value.Id));
        Assert.Equal(AssetStatus.UnderMaintenance, after.Value!.Status);
    }

    [Fact]
    public async Task UpdateAsset_AvailableWithOpenTicket_IsRejected()
    {
        await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Broken", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new CreateMaintenanceLogCommand(asset.Value!.Id, "Needs repair"));

        var result = await Send(Update(asset.Value.Id, AssetStatus.Available));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task ReportMaintenance_OnRetiredAsset_IsRejected()
    {
        await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Old Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(Update(asset.Value!.Id, AssetStatus.Retired));

        var result = await Send(new CreateMaintenanceLogCommand(asset.Value.Id, "Broken"));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task PasswordReset_TokenCannotBeUsedWithDifferentEmail()
    {
        var (_, _, email) = await ArrangeAsync();
        Fixture.CurrentUser.Clear();
        var forgot = await Send(new ForgotPasswordCommand(email));
        var token = forgot.Value!.DevToken!;

        var result = await Send(new ResetPasswordCommand(
            $"attacker-{Guid.NewGuid():N}@test.local", token, "Hacked@123"));

        Assert.True(result.IsFailure);
        Assert.Equal("BadRequest", result.Error.Code);

        var valid = await Send(new ResetPasswordCommand(email, token, "Legit@123"));
        Assert.True(valid.IsSuccess);
    }
}
