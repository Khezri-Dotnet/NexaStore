
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default
    );
    void Remove(Category category);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasProductsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    );
}
