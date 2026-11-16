
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
}
