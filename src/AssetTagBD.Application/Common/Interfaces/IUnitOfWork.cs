namespace AssetTagBD.Application.Common.Interfaces;

/// <summary>
/// Unit of Work abstraction. Repositories and this service share the same
/// scoped DbContext, so SaveChangesAsync commits all tracked modifications.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
