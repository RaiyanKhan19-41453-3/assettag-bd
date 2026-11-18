using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Application.DTOs.Maintenance;

public sealed record MaintenanceLogDto(
    Guid Id,
    Guid AssetId,
    string AssetTag,
    Guid ReportedByUserId,
    string ReportedByName,
    string IssueDescription,
    MaintenanceStatus Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt)
{
    public static MaintenanceLogDto FromEntity(MaintenanceLog log) =>
        new(
            log.Id,
            log.AssetId,
            log.Asset.AssetTag,
            log.ReportedByUserId,
            log.ReportedByUser.FullName,
            log.IssueDescription,
            log.Status,
            log.CreatedAt,
            log.ResolvedAt);
}
