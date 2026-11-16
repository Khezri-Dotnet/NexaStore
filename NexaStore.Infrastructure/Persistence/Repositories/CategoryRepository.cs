using Microsoft.EntityFrameworkCore;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Domain.Entities;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly NexaStoreDbContext _context;

    public CategoryRepository(NexaStoreDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(
            category,
            cancellationToken
        );
    }
    public void Remove(Category category)
    {
        _context.Categories.Remove(category);
    }

    public async Task<bool> ExistsByNameAsync(
    string name,
    CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<bool> HasProductsAsync(
    Guid categoryId,
    CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(x => x.CategoryId == categoryId, cancellationToken);
    }
    
}