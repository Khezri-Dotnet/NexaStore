
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

    public async Task<Result<PagedResult<ProductResponse>>> HandleAsync(
        GetAllProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetPagedAsync(
            query.PageNumber,
            query.PageSize,
            query.Search,
            query.MinPrice,
            query.MaxPrice,
            cancellationToken
        );

        var response = new PagedResult<ProductResponse>
        {
            Items = products.Items
                .Select(product => new ProductResponse(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CategoryId
                ))
                .ToList(),

            PageNumber = products.PageNumber,
            PageSize = products.PageSize,
            TotalCount = products.TotalCount
        };

        return Result<PagedResult<ProductResponse>>.Success(response);
    }
}
