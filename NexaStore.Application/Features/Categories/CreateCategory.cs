using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = new Category(
            request.Name,
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