
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Orders.DTOs;

namespace NexaStore.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOrderByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderResponse>> Handle(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.CustomerId != customerId)
        {
            return Result<OrderResponse>.Failure(
                new Error(
                    "Order.NotFound",
                    "Order was not found."));
        }

        var response = new OrderResponse(
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
        );

        return Result<OrderResponse>.Success(response);
    }
}
