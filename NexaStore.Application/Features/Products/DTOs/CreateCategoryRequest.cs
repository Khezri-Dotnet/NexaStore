
namespace NexaStore.Application.Features.Categories.DTOs;

public sealed record CreateCategoryRequest(
    string Name,
    string Description
);
