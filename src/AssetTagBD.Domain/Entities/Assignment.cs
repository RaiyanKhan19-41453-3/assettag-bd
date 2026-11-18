using AssetTagBD.Domain.Common;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// Records who an asset was handed to and, when <see cref="ReturnedDate"/> is set,
/// that it was handed back. An asset is considered Assigned while it has an
/// assignment with no ReturnedDate.
/// </summary>
public class Assignment : BaseEntity
{
    public Guid AssetId { get; set; }

    public Guid AssignedToUserId { get; set; }

    public Guid AssignedByUserId { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ReturnedDate { get; set; }

    public string? ConditionNotes { get; set; }

    /// <summary>Denormalised from the asset so query filters apply to history rows too.</summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public Asset Asset { get; set; } = null!;

    public User AssignedToUser { get; set; } = null!;

    public User AssignedByUser { get; set; } = null!;
}
