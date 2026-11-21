
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Features.Orders.GetOrderById;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;

namespace NexaStore.Application.Tests;

public class GetOrderByIdHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();

    private GetOrderByIdHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Orders)
            .Returns(_orderRepositoryMock.Object);

        return new GetOrderByIdHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrder_WhenOrderBelongsToCustomer()
    {
        var customerId = Guid.NewGuid();
        var product = new Product(
            "Laptop",
            "Test laptop",
            1000m,
            10,
            Guid.NewGuid());

        var order = new Order(customerId);
        order.AddItem(product, 2);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = CreateHandler();

        var result = await handler.Handle(order.Id, customerId);

        Assert.True(result.IsSuccess);

        var response = result.Value!;

        Assert.Equal(order.Id, response.Id);
        Assert.Equal(order.OrderDate, response.OrderDate);
        Assert.Equal(OrderStatus.Pending, response.Status);
        Assert.Equal(2000m, response.TotalAmount);

        var item = Assert.Single(response.Items);

        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal("Laptop", item.ProductName);
        Assert.Equal(1000m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(2000m, item.LineTotal);

        _orderRepositoryMock.Verify(
            x => x.GetByIdAsync(
                order.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenOrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(orderId, customerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.NotFound", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenOrderBelongsToAnotherCustomer()
    {
        var ownerId = Guid.NewGuid();
        var anotherCustomerId = Guid.NewGuid();

        var product = new Product(
            "Laptop",
            "Test laptop",
            1000m,
            10,
            Guid.NewGuid());

        var order = new Order(ownerId);
        order.AddItem(product, 1);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = CreateHandler();

        var result = await handler.Handle(
            order.Id,
            anotherCustomerId);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.NotFound", result.Error!.Code);
    }
}
