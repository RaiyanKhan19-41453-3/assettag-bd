using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Dashboard;
using AssetTagBD.Domain.Enums;
using MediatR;

namespace AssetTagBD.Application.Features.Dashboard;

public sealed record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>;

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private const int RecentActivityLimit = 8;

    private readonly IAssetRepository _assets;
    private readonly IAssignmentRepository _assignments;
    private readonly IMaintenanceLogRepository _maintenanceLogs;

    public GetDashboardSummaryQueryHandler(
        IAssetRepository assets,
        IAssignmentRepository assignments,
        IMaintenanceLogRepository maintenanceLogs)
    {
        _assets = assets;
        _assignments = assignments;
        _maintenanceLogs = maintenanceLogs;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Queries are awaited one at a time: all repositories share the same
        // scoped DbContext, which does not support concurrent operations.
        var totalAssets = await _assets.CountAllAsync(cancellationToken);
        var available = await _assets.CountByStatusAsync(AssetStatus.Available, cancellationToken);
        var assigned = await _assets.CountByStatusAsync(AssetStatus.Assigned, cancellationToken);
        var underMaintenance = await _assets.CountByStatusAsync(AssetStatus.UnderMaintenance, cancellationToken);
        var retired = await _assets.CountByStatusAsync(AssetStatus.Retired, cancellationToken);
        var activeAssignments = await _assignments.CountActiveAsync(cancellationToken);

        var reported = await _maintenanceLogs.CountByStatusAsync(MaintenanceStatus.Reported, cancellationToken);
        var inProgress = await _maintenanceLogs.CountByStatusAsync(MaintenanceStatus.InProgress, cancellationToken);

        var recentAssignments = await _assignments.GetRecentAsync(RecentActivityLimit, cancellationToken);
        var recentLogs = await _maintenanceLogs.GetRecentAsync(RecentActivityLimit, cancellationToken);

        var activity = recentAssignments
            .Select(a => new ActivityDto(
                a.Id,
                a.ReturnedDate.HasValue ? "Return" : "Assignment",
                a.ReturnedDate.HasValue
                    ? $"{a.AssignedToUser.FullName} returned {a.Asset.Name}"
                    : $"{a.AssignedToUser.FullName} was assigned {a.Asset.Name}",
                a.ReturnedDate ?? a.AssignedDate))
            .Concat(recentLogs.Select(m => new ActivityDto(
                m.Id,
                "Maintenance",
                $"Maintenance reported for {m.Asset.Name}: {m.IssueDescription}",
                m.CreatedAt)))
            .OrderByDescending(a => a.OccurredAt)
            .Take(RecentActivityLimit)
            .ToList();

        var summary = new DashboardSummaryDto(
            totalAssets,
            available,
            assigned,
            underMaintenance,
            retired,
            activeAssignments,
            reported + inProgress,
            activity);

        return Result.Success(summary);
    }
}
