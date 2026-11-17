
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    IProductRepository Products { get; }

    ICategoryRepository Categories { get; }
    ICartRepository Carts { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}
