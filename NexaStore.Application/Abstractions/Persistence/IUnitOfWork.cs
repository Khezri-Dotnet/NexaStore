
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    IProductRepository Products { get; }

    ICategoryRepository Categories { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}
