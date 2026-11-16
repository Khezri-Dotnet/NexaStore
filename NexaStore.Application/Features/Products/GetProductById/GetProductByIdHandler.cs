
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.Application.Features.Products.GetProductById;

public sealed class GetProductByIdHandler
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductResponse>> HandleAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        // 1. Retrieve product from repository
        var product = await _productRepository.GetByIdAsync(
            productId,
            cancellationToken
        );

        // 2. Check whether product exists
        if (product is null)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Product.NotFound",
                    "Product was not found."
                )
            );
        }

        // 3. Map entity to response DTO
        var response = new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId
        );

        // 4. Return successful result
        return Result<ProductResponse>.Success(response);
    }
}
