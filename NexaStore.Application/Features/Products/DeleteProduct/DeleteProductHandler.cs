
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;

namespace NexaStore.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(
            productId,
            cancellationToken
        );

        if (product is null)
        {
            return Result<bool>.Failure(
                new Error(
                    "Product.NotFound",
                    "Product was not found."
                )
            );
        }

        try
        {
            product.SoftDelete();
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(
                new Error(
                    "Product.InvalidOperation",
                    ex.Message
                )
            );
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
