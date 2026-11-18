using AssetTagBD.Domain.Common;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// A rotating refresh token. Only the SHA-256 hash is stored in the database;
/// the raw token is returned to the client once and cannot be recovered.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByHash { get; set; }

    public Guid TenantId { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public User User { get; set; } = null!;
}
