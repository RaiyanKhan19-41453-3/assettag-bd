using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetRepository : Repository<PasswordResetToken>, IPasswordResetRepository
{
    public PasswordResetRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PasswordResetToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        await Set.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeUnusedForUserAsync(
        Guid userId,
        DateTime usedAt,
        CancellationToken cancellationToken = default) =>
        await Set.IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UsedAt, usedAt),
                cancellationToken);

    public async Task<bool> TryConsumeAsync(
        string tokenHash,
        DateTime usedAt,
        CancellationToken cancellationToken = default)
    {
        var affected = await Set.IgnoreQueryFilters()
            .Where(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UsedAt, usedAt),
                cancellationToken);

        return affected == 1;
    }
}
