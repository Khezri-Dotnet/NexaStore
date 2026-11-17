
using NexaStore.Domain.Enums;

namespace NexaStore.Application.Features.Orders.DTOs;

public sealed record OrderResponse(
    Guid Id,
    DateTime OrderDate,
    OrderStatus Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
