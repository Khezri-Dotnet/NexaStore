
namespace NexaStore.Application.Features.Products.DTOs;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);
