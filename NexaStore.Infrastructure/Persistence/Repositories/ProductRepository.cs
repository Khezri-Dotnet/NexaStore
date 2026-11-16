using Microsoft.EntityFrameworkCore;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Domain.Entities;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly NexaStoreDbContext _context;

    public ProductRepository(NexaStoreDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(
                x => x.Id == productId,
                cancellationToken
            );
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}