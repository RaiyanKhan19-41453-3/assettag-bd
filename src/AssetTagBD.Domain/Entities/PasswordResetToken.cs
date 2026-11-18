using AssetTagBD.Domain.Common;

namespace AssetTagBD.Domain.Entities;

/// <summary>A short-lived, single-use password reset token (stored hashed).</summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UsedAt { get; set; }

    public Guid TenantId { get; set; }

    public User User { get; set; } = null!;
}
