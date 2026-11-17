
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Carts.DTOs;

namespace NexaStore.Application.Features.Carts.UpdateCartItem;

public sealed class UpdateCartItemHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCartItemHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponse>> HandleAsync(
        Guid userId,
        Guid itemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(
            userId, cancellationToken);

        if (cart is null)
            return Result<CartResponse>.Failure(
                new Error("Cart.NotFound", "Cart was not found."));

        try
        {
            cart.UpdateItemQuantity(itemId, request.Quantity);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result<CartResponse>.Failure(
                new Error("Cart.InvalidQuantity", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CartResponse>.Failure(
                new Error("Cart.OperationFailed", ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CartResponse>.Success(
            CartResponseMapper.Map(cart));
    }
}
