
using Microsoft.EntityFrameworkCore;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Domain.Entities;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly NexaStoreDbContext _context;

    public CartRepository(NexaStoreDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    public void Remove(Cart cart)
    {
        _context.Carts.Remove(cart);
    }
}
