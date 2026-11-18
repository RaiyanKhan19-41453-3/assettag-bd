using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTagBD.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default) =>
        await Set.IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsGloballyAsync(string email, CancellationToken cancellationToken = default) =>
        Set.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByIdForAuthAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.IgnoreQueryFilters().Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}
