
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default);

    void Remove(Cart cart);
}
