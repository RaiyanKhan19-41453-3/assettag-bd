namespace AssetTagBD.Domain.Enums;

/// <summary>
/// Current lifecycle state of an asset.
/// </summary>
public enum AssetStatus
{
    Available = 1,
    Assigned = 2,
    UnderMaintenance = 3,
    Retired = 4
}
