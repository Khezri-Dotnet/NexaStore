
using Microsoft.EntityFrameworkCore;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Features.Products.GetAllProducts;
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

    public async Task<PagedResult<Product>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<(int StockBefore, int StockAfter)?> TryDecreaseStockAsync(
    Guid productId,
    int quantity,
    CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var now = DateTime.UtcNow;

        var stockValues = await _context.Database
            .SqlQuery<int>($"""
                UPDATE "Products"
                SET "StockQuantity" = "StockQuantity" - {quantity},
                    "UpdatedAt" = {now}
                WHERE "Id" = {productId}
                AND "StockQuantity" >= {quantity}
                AND NOT "IsDeleted"
                RETURNING "StockQuantity" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        if (stockValues.Count == 0)
            return null;

        var stockAfter = stockValues[0];

        return (stockAfter + quantity, stockAfter);
    }
}
