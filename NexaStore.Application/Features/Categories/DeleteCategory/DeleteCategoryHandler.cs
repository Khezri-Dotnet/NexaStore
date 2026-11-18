
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Common.Interfaces;

namespace NexaStore.Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public DeleteCategoryHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(
            id,
            cancellationToken
        );

        if (category is null)
        {
            return Result.Failure(
                new Error(
                    "Category.NotFound",
                    "Category was not found."
                )
            );
        }

        var hasProducts = await _unitOfWork.Categories.HasProductsAsync(
            id,
            cancellationToken
        );

        if (hasProducts)
        {
            return Result.Failure(
                new Error(
                    "Category.HasProducts",
                    "Cannot delete a category that contains products."
                )
            );
        }

        _unitOfWork.Categories.Remove(category);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.SetAsync(
        "categories:version",
        Guid.NewGuid().ToString("N"),
        TimeSpan.FromDays(365),
        cancellationToken);

        return Result.Success();
    }
}
