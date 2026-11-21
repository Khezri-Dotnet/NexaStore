using Microsoft.EntityFrameworkCore.Storage;
using NexaStore.Application.Abstractions.Persistence;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NexaStoreDbContext _context;
    private IDbContextTransaction? _transaction;

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public ICartRepository Carts { get; }
    public IOrderRepository Orders { get; }
    public IInventoryRepository Inventory { get; }

    public UnitOfWork(
        NexaStoreDbContext context,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ICartRepository cartRepository,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository)
    {
        _context = context;
        Products = productRepository;
        Categories = categoryRepository;
        Carts = cartRepository;
        Orders = orderRepository;
        Inventory = inventoryRepository;
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "A transaction is already active.");

        _transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException(
                "No active transaction exists.");

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}