using AssetTagBD.Domain.Common;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// A maintenance ticket raised against an asset (e.g. after a scan shows a fault).
/// </summary>
public class MaintenanceLog : BaseEntity
{
    public Guid AssetId { get; set; }

    public Guid ReportedByUserId { get; set; }

    public string IssueDescription { get; set; } = string.Empty;

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Reported;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    /// <summary>Denormalised from the asset so query filters apply to maintenance rows too.</summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public Asset Asset { get; set; } = null!;

    public User ReportedByUser { get; set; } = null!;
}
