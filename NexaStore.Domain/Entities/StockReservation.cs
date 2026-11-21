using NexaStore.Domain.Enums;

namespace NexaStore.Domain.Entities;

public class StockReservation : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public StockReservationStatus Status { get; private set; }

    private StockReservation() { }

    public StockReservation(
        Guid orderId,
        Guid productId,
        int quantity,
        DateTime expiresAt)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.");

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration must be in the future.");

        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
        Status = StockReservationStatus.Active;
    }

    public void Consume()
    {
        EnsureActive();
        Status = StockReservationStatus.Consumed;
        MarkAsUpdated();
    }

    public void Release()
    {
        EnsureActive();
        Status = StockReservationStatus.Released;
        MarkAsUpdated();
    }

    public void Expire()
    {
        EnsureActive();

        if (ExpiresAt > DateTime.UtcNow)
            throw new InvalidOperationException("Reservation has not expired.");

        Status = StockReservationStatus.Expired;
        MarkAsUpdated();
    }

    private void EnsureActive()
    {
        if (Status != StockReservationStatus.Active)
            throw new InvalidOperationException(
                "Only active reservations can change status.");
    }
}