using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>Tracking query: callers update the returned assignment when returning an asset.</summary>
    public async Task<Assignment?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default) =>
        await Set
            .Include(a => a.Asset)
            .Include(a => a.AssignedToUser)
            .Include(a => a.AssignedByUser)
            .FirstOrDefaultAsync(a => a.AssetId == assetId && a.ReturnedDate == null, cancellationToken);

    public async Task<IReadOnlyList<Assignment>> GetHistoryByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(a => a.Asset)
            .Include(a => a.AssignedToUser)
            .Include(a => a.AssignedByUser)
            .Where(a => a.AssetId == assetId)
            .OrderByDescending(a => a.AssignedDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Assignment>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(a => a.Asset)
            .Include(a => a.AssignedToUser)
            .OrderByDescending(a => a.AssignedDate)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken = default) =>
        Set.CountAsync(a => a.ReturnedDate == null, cancellationToken);
}
