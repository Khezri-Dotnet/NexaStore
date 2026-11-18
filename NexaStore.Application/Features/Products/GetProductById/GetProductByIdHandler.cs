
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.Application.Features.Products.GetProductById;

public sealed class GetProductByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public GetProductByIdHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<ProductResponse>> HandleAsync(
        Guid productId,
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

        var cacheKey = $"products:{version}:{productId}";

        var cachedResponse = await _cacheService.GetAsync<ProductResponse>(
            cacheKey,
            cancellationToken);

        if (cachedResponse is not null)
        {
            return Result<ProductResponse>.Success(cachedResponse);
        }

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

        await _cacheService.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(10),
            cancellationToken);

        return Result<ProductResponse>.Success(response);
    }
}
