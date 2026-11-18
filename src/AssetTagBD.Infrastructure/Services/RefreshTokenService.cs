using System.Security.Cryptography;
using System.Text;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;

namespace AssetTagBD.Infrastructure.Services;

/// <summary>
/// Rotating refresh tokens with hash-only storage. Rotation revokes the old
/// token with a single conditional UPDATE, so two concurrent requests cannot
/// both succeed with the same token.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int RefreshTokenDays = 7;

    private readonly IRefreshTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenService(
        IRefreshTokenRepository tokens,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _tokens = tokens;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> IssueAsync(User user, CancellationToken cancellationToken = default)
    {
        var (rawToken, entity) = CreateToken(user);
        await _tokens.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    public async Task<(User User, string NewToken)?> RotateAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(rawToken);
        var existing = await _tokens.GetByHashAsync(tokenHash, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            return null;
        }

        var user = await _users.GetByIdForAuthAsync(existing.UserId, cancellationToken);
        if (user is null || user.Tenant is null || !user.Tenant.IsActive)
        {
            return null;
        }

        var (newRawToken, newEntity) = CreateToken(user);

        // Conditional UPDATE: only the first caller to revoke the old token wins.
        var revoked = await _tokens.TryRevokeAsync(
            tokenHash, Hash(newRawToken), DateTime.UtcNow, cancellationToken);
        if (!revoked)
        {
            return null;
        }

        await _tokens.AddAsync(newEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (user, newRawToken);
    }

    public async Task<bool> RevokeAsync(string rawToken, CancellationToken cancellationToken = default) =>
        await _tokens.TryRevokeAsync(Hash(rawToken), string.Empty, DateTime.UtcNow, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var active = await _tokens.GetActiveForUserAsync(userId, cancellationToken);
        if (active.Count == 0) return;

        foreach (var token in active)
        {
            token.RevokedAt = DateTime.UtcNow;
            _tokens.Update(token);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static (string RawToken, RefreshToken Entity) CreateToken(User user)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays)
        };
        return (rawToken, entity);
    }

    internal static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
