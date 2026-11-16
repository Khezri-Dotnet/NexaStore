
using NexaStore.Application.Features.Products.GetAllProducts;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default
    );

    Task<PagedResult<Product>> GetPagedAsync(
    int pageNumber,
    int pageSize,
    string? search,
    decimal? minPrice,
    decimal? maxPrice,
    CancellationToken cancellationToken = default
);
}
