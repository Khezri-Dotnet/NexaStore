
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.Application.Features.Products.GetAllProducts;

public sealed class GetAllProductsHandler
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyList<ProductResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        // 1. Retrieve all products
        var products = await _productRepository.GetAllAsync(
            cancellationToken
        );

        // 2. Map entities to response DTOs
        var response = products
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CategoryId
            ))
            .ToList();

        // 3. Return successful result
        return Result<IReadOnlyList<ProductResponse>>.Success(
            response
        );
    }
}
