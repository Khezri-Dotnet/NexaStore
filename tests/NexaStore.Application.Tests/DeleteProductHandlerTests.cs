
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Products.DeleteProduct;
using NexaStore.Domain.Entities;
using Xunit;

namespace NexaStore.Application.Tests;

public class DeleteProductHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private DeleteProductHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Products)
            .Returns(_productRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new DeleteProductHandler(
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

        var result = await handler.HandleAsync(productId);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error?.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSoftDeleteProductAndInvalidateCache_WhenProductExists()
    {
        var productId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            5,
            Guid.NewGuid());

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(productId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.True(product.IsDeleted);

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

    [Fact]
    public async Task HandleAsync_ShouldRemainSuccessful_WhenProductIsAlreadyDeleted()
    {
        var productId = Guid.NewGuid();

        var product = new Product(
            "Test Product",
            "Description",
            100m,
            5,
            Guid.NewGuid());

        product.SoftDelete();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(productId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.True(product.IsDeleted);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
