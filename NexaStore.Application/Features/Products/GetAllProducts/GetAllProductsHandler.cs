
using System.Text.Json;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.Application.Features.Products.GetAllProducts;

public sealed class GetAllProductsHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public GetAllProductsHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<PagedResult<ProductResponse>>> HandleAsync(
        GetAllProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var version = await _cacheService.GetAsync<string>(
        "products:version",
        cancellationToken);

        if (version is null)
        {
            version = Guid.NewGuid().ToString("N");

            await _cacheService.SetAsync(
                "products:version",
                version,
                TimeSpan.FromDays(365),
                cancellationToken);
        }

        var cacheKey = $"products:list:{version}:{JsonSerializer.Serialize(new
        {
            query.PageNumber,
            query.PageSize,
            query.Search,
            query.MinPrice,
            query.MaxPrice
        })}";

        var cachedResponse = await _cacheService
            .GetAsync<PagedResult<ProductResponse>>(
                cacheKey,
                cancellationToken);

        if (cachedResponse is not null)
            return Result<PagedResult<ProductResponse>>.Success(cachedResponse);

        var products = await _unitOfWork.Products.GetPagedAsync(
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

        await _cacheService.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return Result<PagedResult<ProductResponse>>.Success(response);
    }
}
