using Moq;
using NexaStore.Application.Abstractions.Identity;
using NexaStore.Application.Abstractions.Authentication;
using NexaStore.Application.Features.Authentication.Login;

namespace NexaStore.Application.Tests.Features.Authentication.Login;

public sealed class LoginHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _sut = new LoginHandler(
            _identityServiceMock.Object,
            _jwtTokenGeneratorMock.Object,
            _refreshTokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsAuthResponse()
    {
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var roles = new List<string> { "Customer" };
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var command = new LoginCommand(email, "StrongPassword123!");
        using var cts = new CancellationTokenSource();

        _identityServiceMock
            .Setup(x => x.ValidateUserAsync(email, command.Password, cts.Token))
            .ReturnsAsync(userId);

        _identityServiceMock
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync(email);

        _identityServiceMock
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(roles);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(userId, email, roles))
            .Returns(accessToken);

        _refreshTokenServiceMock
            .Setup(x => x.CreateAsync(userId, cts.Token))
            .ReturnsAsync(refreshToken);

        var result = await _sut.Handle(command, cts.Token);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(email, result.Email);
        Assert.Equal(accessToken, result.Token);
        Assert.Equal(refreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreInvalid_ThrowsUnauthorizedAccessException()
    {
        var command = new LoginCommand(
            "user@example.com",
            "WrongPassword123!");

        _identityServiceMock
            .Setup(x => x.ValidateUserAsync(
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid email or password.", exception.Message);

        _identityServiceMock.Verify(
            x => x.GetUserEmailAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenUserEmailIsUnavailable_ThrowsInvalidOperationException(
        string? unavailableEmail)
    {
        var userId = Guid.NewGuid();
        var command = new LoginCommand(
            "user@example.com",
            "StrongPassword123!");

        _identityServiceMock
            .Setup(x => x.ValidateUserAsync(
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _identityServiceMock
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync(unavailableEmail);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Authenticated user email is unavailable.",
            exception.Message);

        _identityServiceMock.Verify(
            x => x.GetUserRolesAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLoginSucceeds_GeneratesTokenWithUserRoles()
    {
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        IList<string> roles = new List<string> { "Customer", "Admin" };
        var command = new LoginCommand(email, "StrongPassword123!");

        _identityServiceMock
            .Setup(x => x.ValidateUserAsync(
                email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _identityServiceMock
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync(email);

        _identityServiceMock
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(roles);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(userId, email, roles))
            .Returns("access-token");

        _refreshTokenServiceMock
            .Setup(x => x.CreateAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        await _sut.Handle(command, CancellationToken.None);

        _jwtTokenGeneratorMock.Verify(
            x => x.GenerateToken(userId, email, roles),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenCreationFails_PropagatesException()
    {
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var roles = new List<string> { "Customer" };
        var command = new LoginCommand(email, "StrongPassword123!");
        var expected = new InvalidOperationException(
            "Refresh token creation failed.");

        _identityServiceMock
            .Setup(x => x.ValidateUserAsync(
                email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _identityServiceMock
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync(email);

        _identityServiceMock
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(roles);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(userId, email, roles))
            .Returns("access-token");

        _refreshTokenServiceMock
            .Setup(x => x.CreateAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(command, CancellationToken.None));

        Assert.Same(expected, actual);
    }
}