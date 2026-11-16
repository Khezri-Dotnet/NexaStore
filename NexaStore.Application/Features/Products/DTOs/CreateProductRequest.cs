
namespace NexaStore.Application.Features.Products.DTOs;

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);
