using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class AssetRepository : Repository<Asset>, IAssetRepository
{
    public AssetRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Asset?> GetByTagAsync(string assetTag, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(a => a.Assignments.Where(x => x.ReturnedDate == null))
                .ThenInclude(x => x.AssignedToUser)
            .FirstOrDefaultAsync(a => a.AssetTag == assetTag, cancellationToken);

    public async Task<Asset?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(a => a.Assignments)
                .ThenInclude(x => x.AssignedToUser)
            .Include(a => a.MaintenanceLogs)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Asset?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set
            .Include(a => a.Assignments.Where(x => x.ReturnedDate == null))
                .ThenInclude(x => x.AssignedToUser)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<PagedResult<Asset>> SearchAsync(
        AssetCategory? category,
        AssetStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(a => a.Assignments.Where(x => x.ReturnedDate == null))
                .ThenInclude(x => x.AssignedToUser)
            .AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(a => a.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.Name.ToLower().Contains(term) ||
                a.AssetTag.ToLower().Contains(term) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
            "tag" => descending ? query.OrderByDescending(a => a.AssetTag) : query.OrderBy(a => a.AssetTag),
            "purchase" => descending ? query.OrderByDescending(a => a.PurchaseDate) : query.OrderBy(a => a.PurchaseDate),
            "status" => descending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => descending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        // Clamp the offset so extreme page numbers cannot overflow int.
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Asset>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<Asset>> GetByTagsAsync(
        IReadOnlyList<string> assetTags,
        CancellationToken cancellationToken = default)
    {
        if (assetTags.Count == 0) return Array.Empty<Asset>();

        var tags = assetTags.Select(tag => tag.Trim().ToUpperInvariant()).Distinct().ToList();

        return await Set.AsNoTracking()
            .Where(asset => tags.Contains(asset.AssetTag))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetAssignedToUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(a => a.Assignments.Where(x => x.AssignedToUserId == userId && x.ReturnedDate == null))
                .ThenInclude(x => x.AssignedToUser)
            .Where(a => a.Assignments.Any(x => x.AssignedToUserId == userId && x.ReturnedDate == null))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> TagExistsAsync(string assetTag, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(a => a.AssetTag == assetTag, cancellationToken);

    public Task<int> CountAllAsync(CancellationToken cancellationToken = default) =>
        Set.CountAsync(cancellationToken);

    public Task<int> CountByStatusAsync(AssetStatus status, CancellationToken cancellationToken = default) =>
        Set.CountAsync(a => a.Status == status, cancellationToken);
}
