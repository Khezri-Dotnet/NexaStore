using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;
using NexaStore.Application.Common.Interfaces;

namespace NexaStore.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler
{
    private static readonly TimeSpan ReservationDuration =
        TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    public CreateOrderHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
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
                new Error("Order.EmptyCart", "Shopping cart is empty."));
        }

        var transactionStarted = false;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            var order = new Order(customerId);
            var expiresAt = DateTime.UtcNow.Add(ReservationDuration);

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Product;

                order.AddItem(product, cartItem.Quantity);

                var stockChange =
                    await _unitOfWork.Products.TryDecreaseStockAsync(
                        product.Id,
                        cartItem.Quantity,
                        cancellationToken);

                if (stockChange is null)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {product.Id}.");
                }

                var reservation = new StockReservation(
                    order.Id,
                    product.Id,
                    cartItem.Quantity,
                    expiresAt);

                var movement = new StockMovement(
                    product.Id,
                    StockMovementType.Reserved,
                    cartItem.Quantity,
                    stockChange.Value.StockBefore,
                    stockChange.Value.StockAfter,
                    order.Id,
                    reservation.Id);

                await _unitOfWork.Inventory.AddReservationAsync(
                    reservation,
                    cancellationToken);

                await _unitOfWork.Inventory.AddMovementAsync(
                    movement,
                    cancellationToken);
            }

            await _unitOfWork.Orders.AddAsync(
                order,
                cancellationToken);

            cart.Clear();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            try
            {
                await _cacheService.SetAsync(
                    "products:version",
                    Guid.NewGuid().ToString("N"),
                    TimeSpan.FromDays(365),
                    cancellationToken);
            }
            catch
            {
                // Cache invalidation failure must not fail a committed order.
            }
            transactionStarted = false;

            return Result<Guid>.Success(order.Id);
        }
        catch (InvalidOperationException ex)
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<Guid>.Failure(
                new Error("Order.CreationFailed", ex.Message));
        }
        catch (ArgumentException ex)
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<Guid>.Failure(
                new Error("Order.InvalidData", ex.Message));
        }
        catch
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }
}