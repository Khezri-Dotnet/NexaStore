
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.DTOs;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductResponse>> HandleAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check whether the category exists
        var category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken
        );

        if (category is null)
        {
            return Result<ProductResponse>.Failure(
                new Error(
                    "Category.NotFound",
                    "The requested category was not found."
                )
            );
        }

        // 2. Create the Product entity using Domain
        
Product product;

try
{
    product = new Product(
        request.Name,
        request.Description,
        request.Price,
        request.StockQuantity,
        request.CategoryId
    );
}
catch (ArgumentOutOfRangeException ex)
{
    var error = ex.ParamName switch
    {
        "price" => new Error(
            "Product.InvalidPrice",
            "Product price must be greater than zero."
        ),

        "stockQuantity" => new Error(
            "Product.InvalidStockQuantity",
            "Stock quantity cannot be negative."
        ),

        _ => new Error(
            "Product.InvalidData",
            "Product contains invalid data."
        )
    };

    return Result<ProductResponse>.Failure(error);
}
catch (ArgumentException ex)
{
    var error = ex.ParamName switch
    {
        "name" => new Error(
            "Product.InvalidName",
            "Product name is required."
        ),

        "categoryId" => new Error(
            "Product.InvalidCategoryId",
            "Category ID is required."
        ),

        _ => new Error(
            "Product.InvalidData",
            "Product contains invalid data."
        )
    };

    return Result<ProductResponse>.Failure(error);
}


        // 3. Add product to repository
        await _productRepository.AddAsync(
            product,
            cancellationToken
        );

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(
            cancellationToken
        );

        // 5. Map entity to response DTO
        var response = new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId
        );

        // 6. Return successful result
        return Result<ProductResponse>.Success(response);
    }
}
