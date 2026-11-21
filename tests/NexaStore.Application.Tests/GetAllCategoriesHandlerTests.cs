
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Application.Features.Categories.GetAllCategories;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Tests;

public class GetAllCategoriesHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetAllCategoriesHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Categories)
            .Returns(_categoryRepositoryMock.Object);

        return new GetAllCategoriesHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedCategories_WhenCacheExists()
    {
        var version = "test-version";

        IReadOnlyList<CategoryResponse> cachedCategories =
        [
            new CategoryResponse(Guid.NewGuid(), "Electronics", "Electronic products")
        ];

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "categories:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<CategoryResponse>>(
                $"categories:all:{version}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedCategories);

        var handler = CreateHandler();

        var result = await handler.Handle();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Same(cachedCategories, result.Value);

        _categoryRepositoryMock.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldGenerateVersion_WhenVersionIsMissing()
    {
        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "categories:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<CategoryResponse>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<CategoryResponse>?)null);

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Category>)Array.Empty<Category>());

        var handler = CreateHandler();

        var result = await handler.Handle();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                "categories:version",
                It.IsAny<string>(),
                TimeSpan.FromDays(365),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLoadAndCacheCategories_WhenCacheIsEmpty()
    {
        const string version = "test-version";

        var category = new Category(
            "Electronics",
            "Electronic products");

        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                "categories:version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _cacheServiceMock
            .Setup(x => x.GetAsync<IReadOnlyList<CategoryResponse>>(
                $"categories:all:{version}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<CategoryResponse>?)null);

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Category>)[category]);

        var handler = CreateHandler();

        var result = await handler.Handle();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var response = Assert.Single(result.Value);

        Assert.Equal(category.Id, response.Id);
        Assert.Equal("Electronics", response.Name);
        Assert.Equal("Electronic products", response.Description);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                $"categories:all:{version}",
                It.IsAny<List<CategoryResponse>>(),
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
