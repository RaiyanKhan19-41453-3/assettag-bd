using AssetTagBD.Domain.Common;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// A physical item tracked by the organisation. <see cref="AssetTag"/> is the
/// value encoded into the QR code and is used to look up an asset when scanned.
/// </summary>
public class Asset : BaseEntity
{
    /// <summary>Human-friendly, globally unique tag, e.g. "ATBD-LAP-000123". Encoded in the QR code.</summary>
    public string AssetTag { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public AssetCategory Category { get; set; }

    public string? SerialNumber { get; set; }

    public DateTime PurchaseDate { get; set; }

    public DateTime? WarrantyExpiryDate { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    /// <summary>The organisation that owns this asset.</summary>
    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Soft delete marker — history is retained for audit purposes.</summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
}
