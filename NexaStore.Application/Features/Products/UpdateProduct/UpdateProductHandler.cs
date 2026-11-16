
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductResponse>> HandleAsync(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Find product
        var product = await _productRepository.GetByIdAsync(
            productId,
            cancellationToken
        );

        if (product is null)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Product.NotFound",
                    "Product was not found."
                )
            );
        }

        // 2. Check category
        var category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken
        );

        if (category is null)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Category.NotFound",
                    "Category was not found."
                )
            );
        }

       // 3. Update product using atomic Domain method
        try
        {
            product.Update(
                request.Name,
                request.Description,
                request.Price,
                request.CategoryId
            );
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Product.InvalidPrice",
                    "Product price must be greater than zero."
                )
            );
        }
        catch (ArgumentException ex)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Product.InvalidData",
                    ex.Message
                )
            );
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Product.InvalidOperation",
                    ex.Message
                )
            );
        }

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(
            cancellationToken
        );

        // 5. Map to response
        var response = new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId
        );

        return Result<ProductResponse>.Success(response);
    }
}
