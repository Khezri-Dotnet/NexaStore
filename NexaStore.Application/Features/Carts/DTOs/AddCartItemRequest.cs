
namespace NexaStore.Application.Features.Carts.DTOs;

public sealed record AddCartItemRequest(
    Guid ProductId,
    int Quantity
);
