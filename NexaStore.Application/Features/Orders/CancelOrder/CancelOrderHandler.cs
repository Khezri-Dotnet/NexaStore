using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;
using NexaStore.Application.Common.Interfaces;

namespace NexaStore.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public CancelOrderHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<bool>> Handle(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.CustomerId != customerId)
        {
            return Result<bool>.Failure(
                new Error("Order.NotFound", "Order not found."));
        }

        var transactionStarted = false;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            var reservations =
                await _unitOfWork.Inventory.GetActiveReservationsByOrderIdAsync(
                    orderId,
                    cancellationToken);

            foreach (var reservation in reservations)
            {
                var stockChange =
                    await _unitOfWork.Inventory.TryIncreaseStockAsync(
                        reservation.ProductId,
                        reservation.Quantity,
                        cancellationToken);

                if (stockChange is null)
                {
                    throw new InvalidOperationException(
                        $"Product {reservation.ProductId} could not be found.");
                }

                reservation.Release();

                var movement = new StockMovement(
                    reservation.ProductId,
                    StockMovementType.ReservationReleased,
                    reservation.Quantity,
                    stockChange.Value.StockBefore,
                    stockChange.Value.StockAfter,
                    orderId,
                    reservation.Id);

                await _unitOfWork.Inventory.AddMovementAsync(
                    movement,
                    cancellationToken);
            }

            order.Cancel();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

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
                // Cache invalidation failure must not fail a committed cancellation.
            }
            transactionStarted = false;

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<bool>.Failure(
                new Error("Order.CancellationFailed", ex.Message));
        }
        catch (ArgumentException ex)
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<bool>.Failure(
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