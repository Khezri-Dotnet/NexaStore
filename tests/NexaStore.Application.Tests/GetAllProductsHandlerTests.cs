
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Products.DTOs;
using NexaStore.Application.Features.Products.GetAllProducts;
using NexaStore.Domain.Entities;
using Xunit;

namespace NexaStore.Application.Tests;

public class GetAllProductsHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetAllProductsHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        return new GetAllProductsHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCachedProducts_WhenCacheContainsResult()
    {
        const string version = "test-version";

        var cachedResponse = new PagedResult<ProductResponse>
        {
            Items =
            [
                new ProductResponse(
                    Guid.NewGuid(),
                    "Cached Product",
                    "Description",
                    100m,
                    5,
                    Guid.NewGuid())
            ],
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "products:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<PagedResult<ProductResponse>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(new GetAllProductsQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("Cached Product", result.Value.Items[0].Name);

        _productRepositoryMock.Verify(
            x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPagedProducts_WhenCacheIsEmpty()
    {
        const string version = "test-version";

        var categoryId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            200m,
            8,
            categoryId);

        var query = new GetAllProductsQuery
        {
            PageNumber = 2,
            PageSize = 5,
            Search = "Test",
            MinPrice = 100m,
            MaxPrice = 500m
        };

        var pagedProducts = new PagedResult<Product>
        {
            Items = [product],
            PageNumber = 2,
            PageSize = 5,
            TotalCount = 11
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "products:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<PagedResult<ProductResponse>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<ProductResponse>?)null);

        _productRepositoryMock
            .Setup(x => x.GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedProducts);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Single(result.Value.Items);
        Assert.Equal("Test Product", result.Value.Items[0].Name);
        Assert.Equal(200m, result.Value.Items[0].Price);

        Assert.Equal(2, result.Value.PageNumber);
        Assert.Equal(5, result.Value.PageSize);
        Assert.Equal(11, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.True(result.Value.HasPreviousPage);
        Assert.True(result.Value.HasNextPage);

        _productRepositoryMock.Verify(
            x => x.GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PagedResult<ProductResponse>>(),
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
