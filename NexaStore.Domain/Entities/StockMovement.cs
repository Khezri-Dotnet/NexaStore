using NexaStore.Domain.Enums;

namespace NexaStore.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public int StockBefore { get; private set; }
    public int StockAfter { get; private set; }

    private StockMovement() { }

    public StockMovement(
        Guid productId,
        StockMovementType type,
        int quantity,
        int stockBefore,
        int stockAfter,
        Guid? orderId = null,
        Guid? reservationId = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (stockBefore < 0 || stockAfter < 0)
            throw new ArgumentOutOfRangeException("Stock values cannot be negative.");

        ProductId = productId;
        Type = type;
        Quantity = quantity;
        StockBefore = stockBefore;
        StockAfter = stockAfter;
        OrderId = orderId;
        ReservationId = reservationId;
    }
}