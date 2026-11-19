using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Infrastructure.Persistence;

namespace AssetTagBD.Infrastructure.Services;

public sealed class AuditWriter : IAuditWriter
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditWriter(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            TenantId = _currentUser.TenantId ?? Guid.Empty,
            ActorUserId = _currentUser.UserId,
            Actor = _currentUser.Email ?? _currentUser.UserId?.ToString() ?? "System",
            Action = record.Action,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            Details = record.Details,
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
