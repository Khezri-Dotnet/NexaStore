
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Features.Orders.GetMyOrders;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;

namespace NexaStore.Application.Tests;

public class GetMyOrdersHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();

    private GetMyOrdersHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Orders)
            .Returns(_orderRepositoryMock.Object);

        return new GetMyOrdersHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenCustomerHasNoOrders()
    {
        var customerId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.GetByCustomerIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Order>());

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);

        _orderRepositoryMock.Verify(
            x => x.GetByCustomerIdAsync(
                customerId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderDetails_WhenCustomerHasOrders()
    {
        var customerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Laptop",
            "Test laptop",
            1000m,
            10,
            categoryId);

        var order = new Order(customerId);
        order.AddItem(product, 2);

        _orderRepositoryMock
            .Setup(x => x.GetByCustomerIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsSuccess);

        var response = Assert.Single(result.Value!);

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
    }

    [Fact]
    public async Task Handle_ShouldReturnAllOrders_WhenCustomerHasMultipleOrders()
    {
        var customerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var laptop = new Product(
            "Laptop",
            "Test laptop",
            1000m,
            10,
            categoryId);

        var phone = new Product(
            "Phone",
            "Test phone",
            500m,
            20,
            categoryId);

        var firstOrder = new Order(customerId);
        firstOrder.AddItem(laptop, 1);

        var secondOrder = new Order(customerId);
        secondOrder.AddItem(phone, 2);

        _orderRepositoryMock
            .Setup(x => x.GetByCustomerIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>
            {
                firstOrder,
                secondOrder
            });

        var handler = CreateHandler();

        var result = await handler.Handle(customerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(firstOrder.Id, result.Value[0].Id);
        Assert.Equal(1000m, result.Value[0].TotalAmount);

        Assert.Equal(secondOrder.Id, result.Value[1].Id);
        Assert.Equal(1000m, result.Value[1].TotalAmount);

        _orderRepositoryMock.Verify(
            x => x.GetByCustomerIdAsync(
                customerId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
