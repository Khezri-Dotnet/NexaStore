
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Orders.CreateOrder;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;

namespace NexaStore.Application.Tests;

public class CreateOrderHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private CreateOrderHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Carts)
            .Returns(_cartRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Orders)
            .Returns(_orderRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Inventory)
            .Returns(_inventoryRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _cacheServiceMock
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new CreateOrderHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCartDoesNotExist()
    {
        var customerId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.EmptyCart", result.Error!.Code);

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCartIsEmpty()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart(customerId);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.EmptyCart", result.Error!.Code);

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrderAndReserveStock_WhenCartIsValid()
    {
        var customerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Laptop",
            "Test laptop",
            1000m,
            10,
            categoryId);

        var cart = new Cart(customerId);
        cart.AddItem(product, 2);

        Order? addedOrder = null;
        StockReservation? addedReservation = null;
        StockMovement? addedMovement = null;

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _productRepositoryMock
            .Setup(x => x.TryDecreaseStockAsync(
                product.Id,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int StockBefore, int StockAfter)?)(10, 8));

        _orderRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>(
                (order, _) => addedOrder = order)
            .Returns(Task.CompletedTask);

        _inventoryRepositoryMock
            .Setup(x => x.AddReservationAsync(
                It.IsAny<StockReservation>(),
                It.IsAny<CancellationToken>()))
            .Callback<StockReservation, CancellationToken>(
                (reservation, _) => addedReservation = reservation)
            .Returns(Task.CompletedTask);

        _inventoryRepositoryMock
            .Setup(x => x.AddMovementAsync(
                It.IsAny<StockMovement>(),
                It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>(
                (movement, _) => addedMovement = movement)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        Assert.NotNull(addedOrder);
        Assert.Equal(customerId, addedOrder.CustomerId);
        Assert.Equal(OrderStatus.Pending, addedOrder.Status);
        Assert.Single(addedOrder.Items);
        Assert.Equal(2000m, addedOrder.TotalAmount);

        Assert.NotNull(addedReservation);
        Assert.Equal(addedOrder.Id, addedReservation.OrderId);
        Assert.Equal(product.Id, addedReservation.ProductId);
        Assert.Equal(2, addedReservation.Quantity);
        Assert.Equal(StockReservationStatus.Active, addedReservation.Status);

        Assert.NotNull(addedMovement);
        Assert.Equal(StockMovementType.Reserved, addedMovement.Type);
        Assert.Equal(10, addedMovement.StockBefore);
        Assert.Equal(8, addedMovement.StockAfter);

        Assert.Empty(cart.Items);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _inventoryRepositoryMock.Verify(
            x => x.AddReservationAsync(
                It.IsAny<StockReservation>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _inventoryRepositoryMock.Verify(
            x => x.AddMovementAsync(
                It.IsAny<StockMovement>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRollback_WhenStockIsInsufficient()
    {
        var customerId = Guid.NewGuid();

        var product = new Product(
            "Laptop",
            "Test laptop",
            1000m,
            10,
            Guid.NewGuid());

        var cart = new Cart(customerId);
        cart.AddItem(product, 2);

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _productRepositoryMock
            .Setup(x => x.TryDecreaseStockAsync(
                product.Id,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int StockBefore, int StockAfter)?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.CreationFailed", result.Error!.Code);

        _unitOfWorkMock.Verify(
            x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
