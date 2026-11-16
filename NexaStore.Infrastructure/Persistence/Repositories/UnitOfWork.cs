using NexaStore.Application.Abstractions.Persistence;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NexaStoreDbContext _context;

    public UnitOfWork(NexaStoreDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}