
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Products.DTOs;
using NexaStore.Application.Features.Products.UpdateProduct;
using NexaStore.Domain.Entities;
using Xunit;

namespace NexaStore.Application.Tests;

public class UpdateProductHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private UpdateProductHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Categories)
            .Returns(_categoryRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new UpdateProductHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            productId,
            new UpdateProductRequest(
                "Updated Product",
                "Description",
                200m,
                Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error?.Code);

        _categoryRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Original Product",
            "Original Description",
            100m,
            5,
            categoryId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            productId,
            new UpdateProductRequest(
                "Updated Product",
                "Description",
                200m,
                categoryId));

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NotFound", result.Error?.Code);

        Assert.Equal("Original Product", product.Name);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPriceIsInvalid()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Original Product",
            "Original Description",
            100m,
            5,
            categoryId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Category", "Description"));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            productId,
            new UpdateProductRequest(
                "Updated Product",
                "Description",
                0m,
                categoryId));

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidPrice", result.Error?.Code);

        Assert.Equal("Original Product", product.Name);
        Assert.Equal(100m, product.Price);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenNameIsInvalid()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Original Product",
            "Original Description",
            100m,
            5,
            categoryId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Category", "Description"));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            productId,
            new UpdateProductRequest(
                " ",
                "Description",
                200m,
                categoryId));

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidData", result.Error?.Code);

        Assert.Equal("Original Product", product.Name);
        Assert.Equal(100m, product.Price);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenProductIsDeleted()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Original Product",
            "Original Description",
            100m,
            5,
            categoryId);

        product.SoftDelete();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Category", "Description"));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            productId,
            new UpdateProductRequest(
                "Updated Product",
                "Description",
                200m,
                categoryId));

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidOperation", result.Error?.Code);

        Assert.Equal("Original Product", product.Name);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateProductAndInvalidateCache_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Original Product",
            "Original Description",
            100m,
            5,
            categoryId);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Category", "Description"));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            productId,
            new UpdateProductRequest(
                "Updated Product",
                "Updated Description",
                250m,
                categoryId));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal("Updated Product", result.Value.Name);
        Assert.Equal("Updated Description", result.Value.Description);
        Assert.Equal(250m, result.Value.Price);
        Assert.Equal(5, result.Value.StockQuantity);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                "products:version",
                It.IsAny<string>(),
                TimeSpan.FromDays(365),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
