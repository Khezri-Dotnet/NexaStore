
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;

namespace NexaStore.Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        return Result.Success();
    }
}
