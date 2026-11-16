
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;

namespace NexaStore.Application.Features.Categories.GetCategoryById;

public sealed class GetCategoryByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoryByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryResponse>> Handle(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(
            id,
            cancellationToken
        );

        if (category is null)
        {
            // خطای NotFound را در مرحله بعد با الگوی Error پروژه هماهنگ می‌کنیم.
            throw new KeyNotFoundException("Category was not found.");
        }

        var response = new CategoryResponse(
            category.Id,
            category.Name,
            category.Description
        );

        return Result<CategoryResponse>.Success(response);
    }
}
