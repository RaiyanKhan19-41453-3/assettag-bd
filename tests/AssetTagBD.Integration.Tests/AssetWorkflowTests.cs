using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Application.Features.Assignments;
using AssetTagBD.Application.Features.Audit;
using AssetTagBD.Application.Features.Maintenance;
using AssetTagBD.Domain.Enums;
using AssetTagBD.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AssetWorkflowTests : IntegrationTestBase
{
    public AssetWorkflowTests(TestFixture fixture) : base(fixture)
    {
    }

    private async Task<(Guid TenantId, Guid AdminId, Guid EmployeeId, string Email)> ArrangeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin-{suffix}@test.local";
        var (tenantId, adminId) = await SeedTenantAsync($"Workflow {suffix}", $"workflow-{suffix}", email);
        var employeeId = await SeedUserAsync(tenantId, UserRole.Employee, $"emp-{suffix}@test.local");
        ActAs(adminId, tenantId, UserRole.SuperAdmin, email);
        return (tenantId, adminId, employeeId, email);
    }

    [Fact]
    public async Task CreateAsset_GeneratesUniqueTagAndPngQrCode()
    {
        await ArrangeAsync();

        var result = await Send(new CreateAssetCommand(null, "Dell Latitude", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("ATBD-LAP-", result.Value!.AssetTag);
        var png = Convert.FromBase64String(result.Value.QrCodeBase64!);
        Assert.Equal((byte)0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
    }

    [Fact]
    public async Task Assign_ThenReturn_RestoresAvailableStatus()
    {
        var (_, _, employeeId, _) = await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Assignable Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        var assign = await Send(new AssignAssetCommand(asset.Value!.Id, employeeId, "Good condition"));
        Assert.True(assign.IsSuccess);

        var afterAssign = await Send(new GetAssetByIdQuery(asset.Value.Id));
        Assert.Equal(AssetStatus.Assigned, afterAssign.Value!.Status);
        Assert.NotNull(afterAssign.Value.CurrentAssigneeName);

        var returned = await Send(new ReturnAssetCommand(asset.Value.Id, "Returned fine"));
        Assert.True(returned.IsSuccess);
        Assert.NotNull(returned.Value!.ReturnedDate);

        var afterReturn = await Send(new GetAssetByIdQuery(asset.Value.Id));
        Assert.Equal(AssetStatus.Available, afterReturn.Value!.Status);
        Assert.Null(afterReturn.Value.CurrentAssigneeName);
    }

    [Fact]
    public async Task Assign_AlreadyAssignedAsset_ReturnsConflict()
    {
        var (_, _, employeeId, _) = await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Busy Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employeeId, null));

        var second = await Send(new AssignAssetCommand(asset.Value.Id, employeeId, null));

        Assert.True(second.IsFailure);
        Assert.Equal("Conflict", second.Error.Code);
    }

    [Fact]
    public async Task Maintenance_ReportAndResolve_RestoresStatus()
    {
        await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Repairable Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        var report = await Send(new CreateMaintenanceLogCommand(asset.Value!.Id, "Screen flickering"));
        Assert.True(report.IsSuccess);

        var underMaintenance = await Send(new GetAssetByIdQuery(asset.Value.Id));
        Assert.Equal(AssetStatus.UnderMaintenance, underMaintenance.Value!.Status);

        var resolved = await Send(new ResolveMaintenanceLogCommand(report.Value!.Id));
        Assert.True(resolved.IsSuccess);
        Assert.NotNull(resolved.Value!.ResolvedAt);

        var afterResolve = await Send(new GetAssetByIdQuery(asset.Value.Id));
        Assert.Equal(AssetStatus.Available, afterResolve.Value!.Status);
    }

    [Fact]
    public async Task SuccessfulCommand_WritesAuditEntry()
    {
        var (tenantId, _, _, _) = await ArrangeAsync();
        var asset = await Send(new CreateAssetCommand(null, "Audited", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        var audit = await Send(new GetAuditEntriesQuery());

        Assert.True(audit.IsSuccess);
        Assert.Contains(audit.Value!.Items, entry =>
            entry.Action == "CreateAsset" && entry.EntityId == asset.Value!.Id);
    }

    [Fact]
    public async Task DeleteAsset_SoftDeletesButKeepsHistory()
    {
        var (tenantId, _, _, _) = await ArrangeAsync();
        var employee = await SeedUserAsync(tenantId, UserRole.Employee, $"softdel-{Guid.NewGuid():N}@test.local");
        var asset = await Send(new CreateAssetCommand(null, "Keep History", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employee, null));
        var returned = await Send(new ReturnAssetCommand(asset.Value.Id, "Returned before deletion"));
        Assert.True(returned.IsSuccess, returned.Error.Message);

        var deleted = await Send(new DeleteAssetCommand(asset.Value.Id));
        Assert.True(deleted.IsSuccess, deleted.Error.Message);

        var detail = await Send(new GetAssetByIdQuery(asset.Value.Id));
        Assert.True(detail.IsFailure);
        Assert.Equal("NotFound", detail.Error.Code);

        var list = await Send(new GetAssetsQuery(null, null, null));
        Assert.DoesNotContain(list.Value!.Items, item => item.Id == asset.Value.Id);

        // History rows still exist in the database, hidden by the soft-delete filter.
        using var scope = Fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Assignments.IgnoreQueryFilters()
            .AnyAsync(a => a.AssetId == asset.Value.Id));
    }

    [Fact]
    public async Task ConcurrentAssign_OnlyOneSucceeds()
    {
        var (tenantId, _, _, _) = await ArrangeAsync();
        var employee = await SeedUserAsync(tenantId, UserRole.Employee, $"race-{Guid.NewGuid():N}@test.local");
        var asset = await Send(new CreateAssetCommand(null, "Race Asset", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        async Task<bool> TryAssign(Guid assetId, Guid employeeId)
        {
            try
            {
                return (await Send(new AssignAssetCommand(assetId, employeeId, null))).IsSuccess;
            }
            catch
            {
                // Database unique-violation on the active-assignment index.
                return false;
            }
        }

        var outcomes = await Task.WhenAll(
            TryAssign(asset.Value!.Id, employee),
            TryAssign(asset.Value.Id, employee));

        Assert.Single(outcomes, success => success);

        var history = await Send(new GetAssignmentHistoryQuery(asset.Value.Id));
        Assert.Single(history.Value!);
    }

    [Fact]
    public async Task GetAssetsByTags_ReturnsOnlyMatchingAssetsWithQr()
    {
        await ArrangeAsync();
        var first = await Send(new CreateAssetCommand("BATCH-A", "First", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        var second = await Send(new CreateAssetCommand("BATCH-B", "Second", AssetCategory.Laptop, null, DateTime.UtcNow, null));

        var result = await Send(new GetAssetsByTagsQuery(
            [first.Value!.AssetTag, second.Value!.AssetTag, "NOT-A-TAG"]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, asset => Assert.False(string.IsNullOrWhiteSpace(asset.QrCodeBase64)));
    }

    [Fact]
    public async Task Validation_RejectsInvalidCreateBeforeHandlerRuns()
    {
        await ArrangeAsync();

        var threw = false;
        try
        {
            await Send(new CreateAssetCommand(null, string.Empty, AssetCategory.Laptop, null, default, null));
        }
        catch (ValidationException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(0, await CountAssetsAsync());
    }
}
