
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Carts.DTOs;

namespace NexaStore.Application.Features.Carts.ClearCart;

public sealed class ClearCartHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public ClearCartHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(
            userId, cancellationToken);

        if (cart is null)
            return Result<CartResponse>.Failure(
                new Error("Cart.NotFound", "Cart was not found."));

        cart.Clear();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CartResponse>.Success(
            CartResponseMapper.Map(cart));
    }
}
