using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Application.Common.Interfaces;

/// <summary>Provides the identity of the caller for audit fields and authorization.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? Role { get; }

    /// <summary>Tenant resolved from the JWT claim; null for anonymous/design-time callers.</summary>
    Guid? TenantId { get; }

    bool IsAuthenticated { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}

public interface IQrCodeService
{
    /// <summary>Returns a Base64-encoded PNG of the QR code for the given payload.</summary>
    string GenerateQrCodeBase64(string payload);
}

/// <summary>A row read from an uploaded spreadsheet. Values stay textual so the
/// Application layer can validate and report precise row-level errors.</summary>
public sealed record AssetImportRow(
    int RowNumber,
    string? AssetTag,
    string Name,
    string? Category,
    string? SerialNumber,
    string? PurchaseDate,
    string? WarrantyExpiryDate);

public interface IExcelAssetReader
{
    IReadOnlyList<AssetImportRow> Read(byte[] fileContent);
}

public interface IAssetTagGenerator
{
    Task<string> GenerateAsync(AssetCategory category, CancellationToken cancellationToken = default);
}

/// <summary>Issues and rotates hashed refresh tokens stored in the database.</summary>
public interface IRefreshTokenService
{
    Task<string> IssueAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Validates, revokes and replaces a token. Returns null when invalid.</summary>
    Task<(User User, string NewToken)?> RotateAsync(string rawToken, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(string rawToken, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
}

/// <summary>Auth-related feature switches supplied by configuration.</summary>
public interface IAuthSettings
{
    bool ExposeResetTokenInResponse { get; }
}
