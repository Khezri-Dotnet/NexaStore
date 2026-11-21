
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Products.CreateProduct;
using NexaStore.Application.Features.Products.DTOs;
using NexaStore.Domain.Entities;
using Xunit;

namespace NexaStore.Application.Tests;

public class CreateProductHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private CreateProductHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Categories)
            .Returns(_categoryRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _cacheServiceMock
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new CreateProductHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var handler = CreateHandler();

        var request = new CreateProductRequest(
            "Test Product",
            "Description",
            100m,
            10,
            categoryId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NotFound", result.Error?.Code);

        _productRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Product>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPriceIsInvalid()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category("Electronics", "Devices");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = CreateHandler();

        var request = new CreateProductRequest(
            "Test Product",
            "Description",
            0m,
            10,
            categoryId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidPrice", result.Error?.Code);

        _productRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Product>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateProduct_WhenRequestIsValid()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category("Electronics", "Devices");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = CreateHandler();

        var request = new CreateProductRequest(
            "Test Product",
            "Description",
            100m,
            10,
            categoryId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Product", result.Value.Name);
        Assert.Equal(100m, result.Value.Price);
        Assert.Equal(10, result.Value.StockQuantity);
        Assert.Equal(categoryId, result.Value.CategoryId);

        _productRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<Product>(p =>
                    p.Name == "Test Product" &&
                    p.Price == 100m &&
                    p.StockQuantity == 10),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                "products:version",
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
