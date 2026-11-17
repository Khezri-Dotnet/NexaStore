using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Carts.DTOs;

public static class CartResponseMapper
{
    public static CartResponse Map(Cart cart)
    {
        var items = cart.Items.Select(x => new CartItemResponse(
            x.Id,
            x.ProductId,
            x.Product?.Name ?? "Unavailable product",
            x.Quantity,
            x.UnitPrice,
            x.TotalPrice
        )).ToList();

        return new CartResponse(
            cart.Id,
            items,
            items.Sum(x => x.TotalPrice));
    }
}