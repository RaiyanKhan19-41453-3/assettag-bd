using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceLogRepository : Repository<MaintenanceLog>, IMaintenanceLogRepository
{
    public MaintenanceLogRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>Tracking query: the resolve handler modifies the returned entity and maps its navigation properties.</summary>
    public async Task<MaintenanceLog?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set
            .Include(m => m.Asset)
            .Include(m => m.ReportedByUser)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceLog>> GetByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(m => m.Asset)
            .Include(m => m.ReportedByUser)
            .Where(m => m.AssetId == assetId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MaintenanceLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(m => m.Asset)
            .Include(m => m.ReportedByUser)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<int> CountByStatusAsync(MaintenanceStatus status, CancellationToken cancellationToken = default) =>
        Set.CountAsync(m => m.Status == status, cancellationToken);
}
