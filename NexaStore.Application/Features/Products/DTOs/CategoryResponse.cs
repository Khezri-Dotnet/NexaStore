
namespace NexaStore.Application.Features.Categories.DTOs;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Description
);
