using AssetTagBD.Domain.Common;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// A person who uses the system. Employees can be assigned assets, IT Managers
/// manage assets/assignments and SuperAdmins administer the whole tenant.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Incremented on password reset so previously issued access tokens stop validating.</summary>
    public int TokenVersion { get; set; } = 1;

    public UserRole Role { get; set; } = UserRole.Employee;

    public string? Department { get; set; }

    /// <summary>The organisation this user belongs to.</summary>
    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Assignment> AssignedAssets { get; set; } = new List<Assignment>();

    public ICollection<Assignment> MadeAssignments { get; set; } = new List<Assignment>();

    public ICollection<MaintenanceLog> ReportedMaintenanceLogs { get; set; } = new List<MaintenanceLog>();
}
