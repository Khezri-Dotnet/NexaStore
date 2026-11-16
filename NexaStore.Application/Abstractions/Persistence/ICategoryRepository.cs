
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
}
