
using FluentValidation;
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Common.Interfaces;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Application.Features.Categories.UpdateCategory;
using NexaStore.Application.Features.Categories.Validators;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Tests;

public class UpdateCategoryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private UpdateCategoryHandler CreateHandler()
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

        return new UpdateCategoryHandler(
            _unitOfWorkMock.Object,
            new UpdateCategoryValidator(),
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNameIsEmpty()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            Guid.NewGuid(),
            new UpdateCategoryRequest("", "Description"));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error!.Code);

        _categoryRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            id,
            new UpdateCategoryRequest("Updated", "Description"));

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NotFound", result.Error!.Code);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAnotherCategoryHasSameName()
    {
        var id = Guid.NewGuid();
        var category = new Category("Old Name", "Old Description");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.ExistsByNameAsync("Existing Name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            id,
            new UpdateCategoryRequest(" Existing Name ", "New Description"));

        Assert.True(result.IsFailure);
        Assert.Equal("Category.Duplicate", result.Error!.Code);

        Assert.Equal("Old Name", category.Name);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCategory_WhenRequestIsValid()
    {
        var id = Guid.NewGuid();
        var category = new Category("Old Name", "Old Description");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.ExistsByNameAsync("New Name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var result = await handler.Handle(
            id,
            new UpdateCategoryRequest(" New Name ", " New Description "));

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", category.Name);
        Assert.Equal("New Description", category.Description);

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

    [Fact]
    public async Task Handle_ShouldSucceed_WhenCategoryKeepsItsOwnName()
    {
        var id = Guid.NewGuid();
        var category = new Category("Existing Name", "Old Description");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.ExistsByNameAsync("Existing Name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            id,
            new UpdateCategoryRequest(" Existing Name ", "Updated Description"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Existing Name", category.Name);
        Assert.Equal("Updated Description", category.Description);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
