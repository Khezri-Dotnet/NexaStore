
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
