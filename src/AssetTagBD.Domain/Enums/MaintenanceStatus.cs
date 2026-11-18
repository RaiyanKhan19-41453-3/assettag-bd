namespace AssetTagBD.Domain.Enums;

/// <summary>
/// Progress state of a maintenance ticket raised against an asset.
/// </summary>
public enum MaintenanceStatus
{
    Reported = 1,
    InProgress = 2,
    Resolved = 3
}
