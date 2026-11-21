using Microsoft.EntityFrameworkCore;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;

namespace NexaStore.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly NexaStoreDbContext _context;

    public InventoryRepository(NexaStoreDbContext context)
    {
        _context = context;
    }

    public async Task AddReservationAsync(
        StockReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await _context.StockReservations.AddAsync(reservation, cancellationToken);
    }

    public async Task AddMovementAsync(
        StockMovement movement,
        CancellationToken cancellationToken = default)
    {
        await _context.StockMovements.AddAsync(movement, cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetActiveReservationsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StockReservations
            .FromSqlInterpolated($"""
                SELECT *
                FROM "StockReservations"
                WHERE "OrderId" = {orderId}
                AND "Status" = {(int)StockReservationStatus.Active}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
    }


    public async Task<(int StockBefore, int StockAfter)?> TryIncreaseStockAsync(
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
                SET "StockQuantity" = "StockQuantity" + {quantity},
                    "UpdatedAt" = {now}
                WHERE "Id" = {productId}
                AND NOT "IsDeleted"
                RETURNING "StockQuantity" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        if (stockValues.Count == 0)
            return null;

        var stockAfter = stockValues[0];

        return (stockAfter - quantity, stockAfter);
    }
}