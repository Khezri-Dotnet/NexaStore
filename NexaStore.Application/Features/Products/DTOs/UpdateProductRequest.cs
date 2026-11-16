
namespace NexaStore.Application.Features.Products.DTOs;

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId
);
