using Moq;
using NexaStore.Application.Abstractions.Identity;
using NexaStore.Application.Features.Authentication.Register;

namespace NexaStore.Application.Tests.Features.Authentication.Register;

public sealed class RegisterHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly RegisterHandler _sut;

    public RegisterHandlerTests()
    {
        _sut = new RegisterHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var command = new RegisterCommand("user@example.com", "StrongPassword123!");
        using var cts = new CancellationTokenSource();

        _identityServiceMock
            .Setup(x => x.RegisterAsync(command.Email, command.Password, cts.Token))
            .ReturnsAsync(userId);

        var result = await _sut.Handle(command, cts.Token);

        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ThrowsInvalidOperationException()
    {
        var command = new RegisterCommand("user@example.com", "StrongPassword123!");

        _identityServiceMock
            .Setup(x => x.RegisterAsync(
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(command, CancellationToken.None));

        Assert.Equal("User already exists.", exception.Message);
    }

    [Fact]
    public async Task Handle_ForwardsArgumentsToIdentityService()
    {
        var command = new RegisterCommand("user@example.com", "StrongPassword123!");
        using var cts = new CancellationTokenSource();

        _identityServiceMock
            .Setup(x => x.RegisterAsync(command.Email, command.Password, cts.Token))
            .ReturnsAsync(Guid.NewGuid());

        await _sut.Handle(command, cts.Token);

        _identityServiceMock.Verify(
            x => x.RegisterAsync(command.Email, command.Password, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdentityServiceThrows_PropagatesException()
    {
        var command = new RegisterCommand("user@example.com", "StrongPassword123!");
        var expected = new InvalidOperationException("Registration failed.");

        _identityServiceMock
            .Setup(x => x.RegisterAsync(
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(command, CancellationToken.None));

        Assert.Same(expected, actual);
    }
}