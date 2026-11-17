
namespace NexaStore.Application.Features.Carts.DTOs;

public sealed record CartItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);
