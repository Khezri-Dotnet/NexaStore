
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _unitOfWork.Carts.GetByUserIdAsync(
            customerId,
            cancellationToken);

        if (cart is null || !cart.Items.Any())
        {
            return Result<Guid>.Failure(
                new Error(
                    "Order.EmptyCart",
                    "Shopping cart is empty."));
        }

        try
        {
            var order = new Order(customerId);

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Product;

                order.AddItem(product, cartItem.Quantity);

                product.DecreaseStock(cartItem.Quantity);
            }

            await _unitOfWork.Orders.AddAsync(
                order,
                cancellationToken);

            cart.Clear();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(order.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(
                new Error(
                    "Order.CreationFailed",
                    ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(
                new Error(
                    "Order.InvalidData",
                    ex.Message));
        }
    }
}
