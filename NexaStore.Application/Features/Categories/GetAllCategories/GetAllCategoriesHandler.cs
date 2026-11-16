
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;

namespace NexaStore.Application.Features.Categories.GetAllCategories;

public sealed class GetAllCategoriesHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCategoriesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(
        CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(
            cancellationToken
        );

        var response = categories
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description
            ))
            .ToList();

        return Result<IReadOnlyList<CategoryResponse>>.Success(response);
    }
}
