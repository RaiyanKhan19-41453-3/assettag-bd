using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    // Refresh endpoints are anonymous, so tenant filters must be bypassed.
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await Set.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await Set.IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryRevokeAsync(
        string tokenHash,
        string replacementHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var affected = await Set.IgnoreQueryFilters()
            .Where(t => t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, revokedAt)
                    .SetProperty(t => t.ReplacedByHash, replacementHash),
                cancellationToken);

        return affected == 1;
    }
}
