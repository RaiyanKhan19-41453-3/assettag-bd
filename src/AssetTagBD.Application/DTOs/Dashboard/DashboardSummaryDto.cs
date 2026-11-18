namespace AssetTagBD.Application.DTOs.Dashboard;

public sealed record ActivityDto(
    Guid Id,
    string Type,
    string Description,
    DateTime OccurredAt);

public sealed record DashboardSummaryDto(
    int TotalAssets,
    int AvailableAssets,
    int AssignedAssets,
    int UnderMaintenanceAssets,
    int RetiredAssets,
    int ActiveAssignments,
    int OpenMaintenanceTickets,
    IReadOnlyList<ActivityDto> RecentActivity);
