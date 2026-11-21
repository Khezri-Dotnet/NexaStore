using NexaStore.Domain.Entities;

namespace NexaStore.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task AddReservationAsync(
        StockReservation reservation,
        CancellationToken cancellationToken = default);

    Task AddMovementAsync(
        StockMovement movement,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockReservation>> GetActiveReservationsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<(int StockBefore, int StockAfter)?> TryIncreaseStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);        
}