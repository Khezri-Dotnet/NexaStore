using FluentValidation;
using NexaStore.Application.Features.Categories.DTOs;

namespace NexaStore.Application.Features.Categories.Validators;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required.")
            .MaximumLength(100)
            .WithMessage("Category name cannot exceed 100 characters.");
    }
}