
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Features.Carts.AddCartItem;
using NexaStore.Application.Features.Carts.DTOs;
using NexaStore.Domain.Entities;
using Xunit;
using NexaStore.Application.Features.Carts.UpdateCartItem;
using NexaStore.Application.Features.Carts.RemoveCartItem;
using NexaStore.Application.Features.Carts.ClearCart;
using NexaStore.Application.Features.Carts.GetCart;

namespace NexaStore.Application.Tests;

public class AddCartItemHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();

    private AddCartItemHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Carts)
            .Returns(_cartRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new AddCartItemHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateCartAndAddItem_WhenCartDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            categoryId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var handler = CreateHandler();

        var request = new AddCartItemRequest(product.Id, 2);

        // Act
        var result = await handler.HandleAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.Items[0].Quantity);
        Assert.Equal(200m, result.Value.TotalPrice);

        _cartRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Cart>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = CreateHandler();

        var request = new AddCartItemRequest(productId, 1);

        // Act
        var result = await handler.HandleAsync(userId, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error?.Code);

        _cartRepositoryMock.Verify(
            x => x.GetByUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenQuantityIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            categoryId);

        var cart = new Cart(userId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = CreateHandler();

        var request = new AddCartItemRequest(product.Id, 0);

        // Act
        var result = await handler.HandleAsync(userId, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Cart.InvalidQuantity", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenStockIsInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            2,
            categoryId);

        var cart = new Cart(userId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = CreateHandler();

        var request = new AddCartItemRequest(product.Id, 3);

        // Act
        var result = await handler.HandleAsync(userId, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Cart.OperationFailed", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncreaseQuantity_WhenProductAlreadyExistsInCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            categoryId);

        var cart = new Cart(userId);
        cart.AddItem(product, 2);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                product.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = CreateHandler();

        var request = new AddCartItemRequest(product.Id, 3);

        // Act
        var result = await handler.HandleAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(5, result.Value.Items[0].Quantity);
        Assert.Equal(500m, result.Value.TotalPrice);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCartItem_ShouldReturnFailure_WhenCartDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        _ = CreateHandler();

        var handler = new UpdateCartItemHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(
            userId,
            Guid.NewGuid(),
            new UpdateCartItemRequest(5));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.NotFound", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateCartItem_ShouldReturnFailure_WhenQuantityIsInvalid()
    {
        var userId = Guid.NewGuid();
        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            Guid.NewGuid());

        var cart = new Cart(userId);
        cart.AddItem(product, 2);

        var itemId = cart.Items.Single().Id;

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);


        _ = CreateHandler();

        var handler = new UpdateCartItemHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(
            userId,
            itemId,
            new UpdateCartItemRequest(0));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.InvalidQuantity", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateCartItem_ShouldUpdateQuantity_WhenRequestIsValid()
    {
        var userId = Guid.NewGuid();
        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            Guid.NewGuid());

        var cart = new Cart(userId);
        cart.AddItem(product, 2);

        var itemId = cart.Items.Single().Id;

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);


        _ = CreateHandler();

        var handler = new UpdateCartItemHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(
            userId,
            itemId,
            new UpdateCartItemRequest(5));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(5, result.Value.Items[0].Quantity);
        Assert.Equal(500m, result.Value.TotalPrice);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveCartItem_ShouldReturnFailure_WhenCartDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        _ = CreateHandler();

        var handler = new RemoveCartItemHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.NotFound", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveCartItem_ShouldReturnFailure_WhenItemDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(userId);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _ = CreateHandler();

        var handler = new RemoveCartItemHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.ItemNotFound", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveCartItem_ShouldRemoveItem_WhenItemExists()
    {
        var userId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            Guid.NewGuid());

        var cart = new Cart(userId);
        cart.AddItem(product, 2);

        var itemId = cart.Items.Single().Id;

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _ = CreateHandler();

        var handler = new RemoveCartItemHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId, itemId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0m, result.Value.TotalPrice);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClearCart_ShouldReturnFailure_WhenCartDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        _ = CreateHandler();

        var handler = new ClearCartHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId);

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.NotFound", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ClearCart_ShouldRemoveAllItems_WhenCartExists()
    {
        var userId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            Guid.NewGuid());

        var cart = new Cart(userId);
        cart.AddItem(product, 2);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _ = CreateHandler();

        var handler = new ClearCartHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0m, result.Value.TotalPrice);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCart_ShouldCreateNewCart_WhenCartDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        _ = CreateHandler();

        var handler = new GetCartHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0m, result.Value.TotalPrice);

        _cartRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<Cart>(c => c.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCart_ShouldReturnExistingCart_WithoutSavingChanges()
    {
        var userId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            10,
            Guid.NewGuid());

        var cart = new Cart(userId);
        cart.AddItem(product, 3);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _ = CreateHandler();

        var handler = new GetCartHandler(_unitOfWorkMock.Object);

        var result = await handler.HandleAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(3, result.Value.Items[0].Quantity);
        Assert.Equal(300m, result.Value.TotalPrice);

        _cartRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Cart>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
