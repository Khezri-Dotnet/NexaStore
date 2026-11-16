
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;

namespace NexaStore.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(
            id,
            cancellationToken
        );

        if (category is null)
            throw new KeyNotFoundException("Category was not found.");

        category.UpdateDetails(request.Name, request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
