
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Carts.DTOs;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Carts.GetCart;

public sealed class GetCartHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCartHandler(IUnitOfWork unitOfWork)
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
        {
            cart = new Cart(userId);
            await _unitOfWork.Carts.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<CartResponse>.Success(
            CartResponseMapper.Map(cart));
    }
}
