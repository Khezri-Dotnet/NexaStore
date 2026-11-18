
using FluentValidation;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Application.Common.Interfaces;

namespace NexaStore.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCategoryRequest> _validator;
    private readonly ICacheService _cacheService;

    public UpdateCategoryHandler(
    IUnitOfWork unitOfWork,
    IValidator<UpdateCategoryRequest> validator,
    ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken
        );

        if (!validationResult.IsValid)
        {
            var message = string.Join(
                " ",
                validationResult.Errors.Select(x => x.ErrorMessage)
            );

            return Result.Failure(
                new Error("Validation.Failed", message)
            );
        }

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

        var categoryName = request.Name.Trim();

        var exists = await _unitOfWork.Categories.ExistsByNameAsync(
            categoryName,
            cancellationToken
        );

        if (exists && !string.Equals(
            category.Name,
            categoryName,
            StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                new Error(
                    "Category.Duplicate",
                    "A category with this name already exists."
                )
            );
        }

        category.UpdateDetails(categoryName, request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.SetAsync(
        "categories:version",
        Guid.NewGuid().ToString("N"),
        TimeSpan.FromDays(365),
        cancellationToken);

        return Result.Success();
    }
}
