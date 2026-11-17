
namespace NexaStore.Domain.Entities;

public sealed class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public Cart Cart { get; private set; } = null!;

    public Product Product { get; private set; } = null!;

    private CartItem() { }

    internal CartItem(
    Guid cartId,
    Product product,
    int quantity,
    decimal unitPrice)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("Cart ID is required.");

        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        CartId = cartId;
        ProductId = product.Id;
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public void UpdateQuantity(int quantity, int availableStock)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > availableStock)
            throw new InvalidOperationException("Insufficient stock.");

        Quantity = quantity;
        MarkAsUpdated();
    }

    internal void IncreaseQuantity(int quantity, int availableStock)
    {
        var newQuantity = checked(Quantity + quantity);
        UpdateQuantity(newQuantity, availableStock);
    }

    public decimal TotalPrice => Quantity * UnitPrice;
}
