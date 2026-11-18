
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;

namespace NexaStore.Application.Features.Categories.GetAllCategories;

public sealed class GetAllCategoriesHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public GetAllCategoriesHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(
        CancellationToken cancellationToken = default)
    {
        var version = await _cacheService.GetAsync<string>(
            "categories:version",
            cancellationToken);

        if (version is null)
        {
            version = Guid.NewGuid().ToString("N");

            await _cacheService.SetAsync(
                "categories:version",
                version,
                TimeSpan.FromDays(365),
                cancellationToken);
        }

        var cacheKey = $"categories:all:{version}";

        var cachedResponse =
            await _cacheService.GetAsync<IReadOnlyList<CategoryResponse>>(
                cacheKey,
                cancellationToken);

        if (cachedResponse is not null)
        {
            return Result<IReadOnlyList<CategoryResponse>>.Success(
                cachedResponse);
        }

        var categories = await _unitOfWork.Categories.GetAllAsync(
            cancellationToken);

        var response = categories
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description
            ))
            .ToList();

        await _cacheService.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return Result<IReadOnlyList<CategoryResponse>>.Success(response);
    }
}
