
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Carts.DTOs;

namespace NexaStore.Application.Features.Carts.RemoveCartItem;

public sealed class RemoveCartItemHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCartItemHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponse>> HandleAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(
            userId, cancellationToken);

        if (cart is null)
            return Result<CartResponse>.Failure(
                new Error("Cart.NotFound", "Cart was not found."));

        try
        {
            cart.RemoveItem(itemId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<CartResponse>.Failure(
                new Error("Cart.ItemNotFound", ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CartResponse>.Success(
            CartResponseMapper.Map(cart));
    }
}
