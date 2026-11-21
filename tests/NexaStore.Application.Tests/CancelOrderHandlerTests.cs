
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Orders.CancelOrder;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;

namespace NexaStore.Application.Tests;

public class CancelOrderHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private CancelOrderHandler CreateHandler()
    {
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

        return new CancelOrderHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenOrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(orderId, customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.NotFound", result.Error!.Code);

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenOrderBelongsToAnotherCustomer()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var anotherCustomerId = Guid.NewGuid();
        var order = new Order(anotherCustomerId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = CreateHandler();

        var result = await handler.Handle(orderId, customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.NotFound", result.Error!.Code);

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCancelOrder_WhenNoActiveReservationsExist()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var order = new Order(customerId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryRepositoryMock
            .Setup(x => x.GetActiveReservationsByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockReservation>)Array.Empty<StockReservation>());

        var handler = CreateHandler();

        var result = await handler.Handle(orderId, customerId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(OrderStatus.Cancelled, order.Status);

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRestoreStockAndReleaseReservation_WhenReservationExists()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var order = new Order(customerId);
        var reservation = new StockReservation(
            orderId,
            productId,
            2,
            DateTime.UtcNow.AddMinutes(10));

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryRepositoryMock
            .Setup(x => x.GetActiveReservationsByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockReservation>)[reservation]);

        _inventoryRepositoryMock
            .Setup(x => x.TryIncreaseStockAsync(
                productId,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int StockBefore, int StockAfter)?)(5, 7));

        var handler = CreateHandler();

        var result = await handler.Handle(orderId, customerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(StockReservationStatus.Released, reservation.Status);

        _inventoryRepositoryMock.Verify(
            x => x.AddMovementAsync(
                It.Is<StockMovement>(movement =>
                    movement.ProductId == productId &&
                    movement.OrderId == orderId &&
                    movement.ReservationId == reservation.Id &&
                    movement.Type == StockMovementType.ReservationReleased &&
                    movement.Quantity == 2 &&
                    movement.StockBefore == 5 &&
                    movement.StockAfter == 7),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRollback_WhenReservedProductCannotBeRestored()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var order = new Order(customerId);
        var reservation = new StockReservation(
            orderId,
            productId,
            2,
            DateTime.UtcNow.AddMinutes(10));

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryRepositoryMock
            .Setup(x => x.GetActiveReservationsByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockReservation>)[reservation]);

        _inventoryRepositoryMock
            .Setup(x => x.TryIncreaseStockAsync(
                productId,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int StockBefore, int StockAfter)?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(orderId, customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.CancellationFailed", result.Error!.Code);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(StockReservationStatus.Active, reservation.Status);

        _unitOfWorkMock.Verify(
            x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
