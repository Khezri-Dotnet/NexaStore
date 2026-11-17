
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Orders.DTOs;

namespace NexaStore.Application.Features.Orders.GetMyOrders;

public sealed class GetMyOrdersHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyOrdersHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Orders.GetByCustomerIdAsync(
            customerId,
            cancellationToken);

        var response = orders.Select(order => new OrderResponse(
            order.Id,
            order.OrderDate,
            order.Status,
            order.TotalAmount,
            order.Items.Select(item => new OrderItemResponse(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal
            )).ToList()
        )).ToList();

        return Result<IReadOnlyList<OrderResponse>>.Success(response);
    }
}
