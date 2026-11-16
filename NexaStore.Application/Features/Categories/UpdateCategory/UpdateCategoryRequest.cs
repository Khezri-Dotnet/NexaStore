namespace NexaStore.Application.Features.Categories.DTOs;

public sealed record UpdateCategoryRequest(
    string Name,
    string Description
);