using AssetTagBD.Application.Common.Models;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Application.Common.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}

public interface IAssetRepository : IRepository<Asset>
{
    Task<Asset?> GetByTagAsync(string assetTag, CancellationToken cancellationToken = default);

    Task<Asset?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tracking query with assignments loaded, for update handlers.</summary>
    Task<Asset?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Asset>> SearchAsync(
        AssetCategory? category,
        AssetStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        string? sortBy,
        bool descending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> GetAssignedToUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> GetByTagsAsync(
        IReadOnlyList<string> assetTags,
        CancellationToken cancellationToken = default);

    Task<bool> TagExistsAsync(string assetTag, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(AssetStatus status, CancellationToken cancellationToken = default);
}

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Auth lookups run before a tenant claim exists, so filters must be bypassed.</summary>
    Task<User?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Emails must be unique across all tenants because login uses email alone.</summary>
    Task<bool> EmailExistsGloballyAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Password reset runs before a tenant claim exists.</summary>
    Task<User?> GetByIdForAuthAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Atomically revokes a still-active token. Returns false if it was already used.</summary>
    Task<bool> TryRevokeAsync(string tokenHash, string replacementHash, DateTime revokedAt, CancellationToken cancellationToken = default);
}

public interface IPasswordResetRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Invalidates earlier outstanding links when a new one is issued.</summary>
    Task RevokeUnusedForUserAsync(Guid userId, DateTime usedAt, CancellationToken cancellationToken = default);

    /// <summary>Atomically marks an unexpired, unused token as used. Returns false if it was already consumed.</summary>
    Task<bool> TryConsumeAsync(string tokenHash, DateTime usedAt, CancellationToken cancellationToken = default);
}

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<Assignment?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Assignment>> GetHistoryByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Assignment>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);
}

public interface IMaintenanceLogRepository : IRepository<MaintenanceLog>
{
    /// <summary>Tracking query with asset and reporter loaded, for resolve handlers.</summary>
    Task<MaintenanceLog?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceLog>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(MaintenanceStatus status, CancellationToken cancellationToken = default);
}
