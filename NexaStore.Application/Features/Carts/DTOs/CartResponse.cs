
namespace NexaStore.Application.Features.Carts.DTOs;

public sealed record CartResponse(
    Guid Id,
    IReadOnlyList<CartItemResponse> Items,
    decimal TotalPrice
);
