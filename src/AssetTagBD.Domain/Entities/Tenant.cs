using AssetTagBD.Domain.Common;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// A customer organisation. All data is isolated to a tenant: a user only ever
/// sees assets, assignments and logs belonging to their own tenant.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe unique identifier used during company sign-up.</summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}
