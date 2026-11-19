using AssetTagBD.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AssetTagBD.Infrastructure.Persistence;

internal sealed class WorkTransaction : IWorkTransaction
{
    private readonly IDbContextTransaction _transaction;

    public WorkTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
