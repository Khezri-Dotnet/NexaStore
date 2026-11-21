
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Products.DTOs;
using NexaStore.Application.Features.Products.GetProductById;
using NexaStore.Domain.Entities;
using Xunit;

namespace NexaStore.Application.Tests;

public class GetProductByIdHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetProductByIdHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        return new GetProductByIdHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCachedProduct_WhenCacheContainsProduct()
    {
        var productId = Guid.NewGuid();
        const string version = "test-version";

        var cachedResponse = new ProductResponse(
            productId,
            "Cached Product",
            "Cached Description",
            150m,
            5,
            Guid.NewGuid());

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "products:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<ProductResponse>(
                $"products:{version}:{productId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(productId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Cached Product", result.Value.Name);
        Assert.Equal(150m, result.Value.Price);

        _productRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        const string version = "test-version";

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "products:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<ProductResponse>(
                $"products:{version}:{productId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(productId);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error?.Code);

        _productRepositoryMock.Verify(
            x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnProductAndCacheResponse_WhenProductExists()
    {
        var productId = Guid.NewGuid();
        const string version = "test-version";

        var product = new Product(
            "Test Product",
            "Description",
            200m,
            8,
            Guid.NewGuid());

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "products:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<ProductResponse>(
                $"products:{version}:{productId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(productId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(product.Name, result.Value.Name);
        Assert.Equal(product.Price, result.Value.Price);
        Assert.Equal(product.StockQuantity, result.Value.StockQuantity);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                $"products:{version}:{productId}",
                It.IsAny<ProductResponse>(),
                TimeSpan.FromMinutes(10),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
