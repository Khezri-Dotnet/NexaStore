
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.Application.Features.Products.GetProductById;

public sealed class GetProductByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductResponse>> HandleAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(
            productId,
            cancellationToken
        );

        if (product is null)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Product.NotFound",
                    "Product was not found."
                )
            );
        }

        var response = new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId
        );

        return Result<ProductResponse>.Success(response);
    }
}
