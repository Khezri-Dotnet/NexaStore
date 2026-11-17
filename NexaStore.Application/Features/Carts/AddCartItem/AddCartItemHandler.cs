
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Carts.DTOs;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Carts.AddCartItem;

public sealed class AddCartItemHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public AddCartItemHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponse>> HandleAsync(
        Guid userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(
            request.ProductId, cancellationToken);

        if (product is null)
            return Result<CartResponse>.Failure(
                new Error("Product.NotFound", "Product was not found."));

        var cart = await _unitOfWork.Carts.GetByUserIdAsync(
            userId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart(userId);
            await _unitOfWork.Carts.AddAsync(cart, cancellationToken);
        }

        try
        {
            cart.AddItem(product, request.Quantity);
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
