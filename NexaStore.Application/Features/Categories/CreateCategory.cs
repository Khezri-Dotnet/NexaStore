
using FluentValidation;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Domain.Entities;
using NexaStore.Application.Common.Interfaces;

namespace NexaStore.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _validator;
    private readonly ICacheService _cacheService;

    public CreateCategoryHandler(
    IUnitOfWork unitOfWork,
    IValidator<CreateCategoryRequest> validator,
    ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _cacheService = cacheService;
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

        await _cacheService.SetAsync(
        "categories:version",
        Guid.NewGuid().ToString("N"),
        TimeSpan.FromDays(365),
        cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
