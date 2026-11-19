namespace AssetTagBD.Application.Common.Interfaces;

/// <summary>A database transaction without leaking EF Core types into Application.</summary>
public interface IWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work abstraction. Repositories and this service share the same
/// scoped DbContext, so SaveChangesAsync commits all tracked modifications.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
