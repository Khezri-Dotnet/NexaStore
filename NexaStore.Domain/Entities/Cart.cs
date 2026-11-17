
namespace NexaStore.Domain.Entities;

public sealed class Cart : BaseEntity
{
    public Guid UserId { get; private set; }

    public ICollection<CartItem> Items { get; private set; }
        = new List<CartItem>();

    private Cart() { }

    public Cart(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.");

        UserId = userId;
    }

    public void AddItem(Product product, int quantity)
    {
        if (product is null)
            throw new ArgumentNullException(nameof(product));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (product.IsDeleted)
            throw new InvalidOperationException("Product is deleted.");

        if (product.StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock.");

        var existingItem = Items.FirstOrDefault(
            x => x.ProductId == product.Id);

        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity, product.StockQuantity);
            MarkAsUpdated();
            return;
        }

        // Items.Add(new CartItem(Id, product.Id, quantity, product.Price));
        Items.Add(new CartItem(Id, product, quantity, product.Price));
        MarkAsUpdated();
    }

    public void RemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(x => x.Id == itemId);

        if (item is null)
            throw new InvalidOperationException("Cart item was not found.");

        Items.Remove(item);
        MarkAsUpdated();
    }

    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        var item = Items.FirstOrDefault(x => x.Id == itemId);

        if (item is null)
            throw new InvalidOperationException("Cart item was not found.");

        if (item.Product is null || item.Product.IsDeleted)
            throw new InvalidOperationException("Product is unavailable.");

        item.UpdateQuantity(quantity, item.Product.StockQuantity);
        MarkAsUpdated();
    }

    public void Clear()
    {
        Items.Clear();
        MarkAsUpdated();
    }
}
