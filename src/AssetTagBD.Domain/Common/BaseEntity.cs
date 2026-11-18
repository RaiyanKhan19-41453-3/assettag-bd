namespace AssetTagBD.Domain.Common;

/// <summary>
/// Base class for all persisted domain entities.
/// Identifiers are generated client-side so entities can be created without
/// a round-trip to the database (safe for distributed / offline scenarios).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
