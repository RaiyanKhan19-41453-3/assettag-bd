using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly AppDbContext _db;

    public AuditRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditEntry>> SearchAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AuditEntries.AsNoTracking().OrderByDescending(e => e.OccurredAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<AuditEntry>.Create(items, pageNumber, pageSize, total);
    }
}
