
using FluentValidation;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _validator;

    public CreateCategoryHandler(
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<Guid>> Handle(
        CreateCategoryRequest request,
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

            return Result<Guid>.Failure(
                new Error("Validation.Failed", message)
            );
        }

        var categoryName = request.Name.Trim();

        var exists = await _unitOfWork.Categories.ExistsByNameAsync(
            categoryName,
            cancellationToken
        );

        if (exists)
        {
            return Result<Guid>.Failure(
                new Error(
                    "Category.Duplicate",
                    "A category with this name already exists."
                )
            );
        }

        var category = new Category(
            categoryName,
            request.Description
        );

        await _unitOfWork.Categories.AddAsync(
            category,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
