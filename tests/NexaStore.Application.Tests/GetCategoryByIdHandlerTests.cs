
using Moq;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Application.Features.Categories.GetCategoryById;
using NexaStore.Domain.Entities;

namespace NexaStore.Application.Tests;

public class GetCategoryByIdHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();

    private GetCategoryByIdHandler CreateHandler()
    {
        _unitOfWorkMock
            .Setup(x => x.Categories)
            .Returns(_categoryRepositoryMock.Object);

        return new GetCategoryByIdHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCategory_WhenCategoryExists()
    {
        var id = Guid.NewGuid();
        var category = new Category(
            "Electronics",
            "Electronic products");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = CreateHandler();

        var result = await handler.Handle(id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(category.Id, result.Value.Id);
        Assert.Equal("Electronics", result.Value.Name);
        Assert.Equal("Electronic products", result.Value.Description);
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
        Assert.Null(result.Value);
    }
}
