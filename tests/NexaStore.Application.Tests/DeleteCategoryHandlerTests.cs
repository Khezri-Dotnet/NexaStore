
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Categories.DeleteCategory;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Tests;

public class DeleteCategoryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private DeleteCategoryHandler CreateHandler()
    {
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

        return new DeleteCategoryHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(id);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NotFound", result.Error!.Code);

        _categoryRepositoryMock.Verify(
            x => x.HasProductsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Category>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCategoryHasProducts()
    {
        var id = Guid.NewGuid();
        var category = new Category("Electronics", "Electronic products");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.HasProductsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(id);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.HasProducts", result.Error!.Code);

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Category>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDeleteCategory_WhenCategoryHasNoProducts()
    {
        var id = Guid.NewGuid();
        var category = new Category("Electronics", "Electronic products");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.HasProductsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var result = await handler.Handle(id);

        Assert.True(result.IsSuccess);

        _categoryRepositoryMock.Verify(
            x => x.Remove(category),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                "categories:version",
                It.IsAny<string>(),
                TimeSpan.FromDays(365),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
