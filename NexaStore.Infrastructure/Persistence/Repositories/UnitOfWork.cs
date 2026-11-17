
using NexaStore.Application.Abstractions.Persistence;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NexaStoreDbContext _context;

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public ICartRepository Carts { get; }

    public UnitOfWork(
        NexaStoreDbContext context,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ICartRepository cartRepository)
    {
        _context = context;
        Products = productRepository;
        Categories = categoryRepository;
        Carts = cartRepository;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
