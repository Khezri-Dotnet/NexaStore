using NexaStore.Domain.Entities;

namespace NexaStore.Domain.Tests;

public class CartTests
{
    private static Product CreateProduct(int stock = 10)
        => new("Test Product", "Description", 100m, stock, Guid.NewGuid());

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrow()
        => Assert.Throws<ArgumentException>(() => new Cart(Guid.Empty));

    [Fact]
    public void AddItem_ShouldAddProductToCart()
    {
        var cart = new Cart(Guid.NewGuid());
        var product = CreateProduct();
        cart.AddItem(product, 2);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items.Single().Quantity);
    }

    [Fact]
    public void AddItem_WithExistingProduct_ShouldIncreaseQuantity()
    {
        var cart = new Cart(Guid.NewGuid());
        var product = CreateProduct();
        cart.AddItem(product, 2);
        cart.AddItem(product, 3);
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items.Single().Quantity);
    }

    [Fact]
    public void AddItem_WithInsufficientStock_ShouldThrow()
    {
        var cart = new Cart(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => cart.AddItem(CreateProduct(2), 3));
    }

    [Fact]
    public void AddItem_WithInvalidQuantity_ShouldThrow()
    {
        var cart = new Cart(Guid.NewGuid());
        Assert.Throws<ArgumentOutOfRangeException>(() => cart.AddItem(CreateProduct(), 0));
    }

    [Fact]
    public void UpdateItemQuantity_ShouldUpdateQuantity()
    {
        var cart = new Cart(Guid.NewGuid());
        var product = CreateProduct();
        cart.AddItem(product, 2);
        cart.UpdateItemQuantity(cart.Items.Single().Id, 5);
        Assert.Equal(5, cart.Items.Single().Quantity);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveProduct()
    {
        var cart = new Cart(Guid.NewGuid());
        cart.AddItem(CreateProduct(), 2);
        cart.RemoveItem(cart.Items.Single().Id);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        var cart = new Cart(Guid.NewGuid());
        cart.AddItem(CreateProduct(), 1);
        cart.AddItem(CreateProduct(), 2);
        cart.Clear();
        Assert.Empty(cart.Items);
    }
}