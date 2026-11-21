
using FluentValidation;
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Categories.CreateCategory;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Application.Features.Categories.Validators;
using NexaStore.Domain.Entities;
using Xunit;

namespace NexaStore.Application.Tests;

public class CreateCategoryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private CreateCategoryHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Categories)
            .Returns(_categoryRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new CreateCategoryHandler(
            _unitOfWorkMock.Object,
            new CreateCategoryValidator(),
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        var handler = CreateHandler();

        var request = new CreateCategoryRequest(
            " ",
            "Description");

        var result = await handler.Handle(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error?.Code);

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Category>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCategoryAlreadyExists()
    {
        _categoryRepositoryMock
            .Setup(x => x.ExistsByNameAsync(
                "Electronics",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var request = new CreateCategoryRequest(
            " Electronics ",
            "Electronic products");

        var result = await handler.Handle(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.Duplicate", result.Error?.Code);

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Category>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateCategory_WhenRequestIsValid()
    {
        _categoryRepositoryMock
            .Setup(x => x.ExistsByNameAsync(
                "Electronics",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Category? addedCategory = null;

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<Category>(),
                It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>(
                (category, _) => addedCategory = category)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        var request = new CreateCategoryRequest(
            " Electronics ",
            "Electronic products");

        var result = await handler.Handle(request);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        Assert.NotNull(addedCategory);
        Assert.Equal("Electronics", addedCategory.Name);
        Assert.Equal("Electronic products", addedCategory.Description);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
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
