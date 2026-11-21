using Microsoft.AspNetCore.Mvc;
using Moq;
using NexaStore.API.Controllers;
using NexaStore.Application.Abstractions.Authentication;
using NexaStore.Application.Abstractions.Identity;
using NexaStore.Application.Features.Authentication.DTOs;
using NexaStore.Application.Features.Authentication.Register;
using NexaStore.Application.Features.Authentication.Login;

namespace NexaStore.Application.Tests.Features.Authentication;

public sealed class AuthenticationRefreshTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();

private AuthenticationController CreateSut()
{
    var registerHandler = new RegisterHandler(_identityService.Object);

    var loginHandler = new LoginHandler(
        _identityService.Object,
        _jwtTokenGenerator.Object,
        _refreshTokenService.Object);

    return new AuthenticationController(
        registerHandler,
        loginHandler,
        _refreshTokenService.Object,
        _identityService.Object,
        _jwtTokenGenerator.Object);
}

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ReturnsNewAuthResponse()
    {
        var userId = Guid.NewGuid();
        const string oldToken = "old-refresh-token";
        const string newToken = "new-refresh-token";
        const string email = "user@example.com";
        const string accessToken = "new-access-token";

        _refreshTokenService
            .Setup(x => x.RotateAsync(oldToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid UserId, string RefreshToken)?)(userId, newToken));

        _identityService
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync(email);

        _identityService
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(new List<string> { "Customer" });

        _jwtTokenGenerator
            .Setup(x => x.GenerateToken(
                userId,
                email,
                It.IsAny<IList<string>>()))
            .Returns(accessToken);

        var sut = CreateSut();

        var result = await sut.Refresh(
            new RefreshTokenRequest(oldToken),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.Equal(userId, response.UserId);
        Assert.Equal(email, response.Email);
        Assert.Equal(accessToken, response.Token);
        Assert.Equal(newToken, response.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsInvalid_ReturnsUnauthorized()
    {
        const string token = "invalid-refresh-token";

        _refreshTokenService
            .Setup(x => x.RotateAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValueTuple<Guid, string>?)null);

        var sut = CreateSut();

        var result = await sut.Refresh(
            new RefreshTokenRequest(token),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);

        _identityService.Verify(
            x => x.GetUserEmailAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenEmailIsUnavailable_RevokesNewTokenAndReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        const string oldToken = "old-refresh-token";
        const string newToken = "new-refresh-token";

        _refreshTokenService
            .Setup(x => x.RotateAsync(oldToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid UserId, string RefreshToken)?)(userId, newToken));

        _identityService
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync((string?)null);

        var sut = CreateSut();

        var result = await sut.Refresh(
            new RefreshTokenRequest(oldToken),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);

        _refreshTokenService.Verify(
            x => x.RevokeAsync(newToken, It.IsAny<CancellationToken>()),
            Times.Once);

        _identityService.Verify(
            x => x.GetUserRolesAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenEmailIsEmpty_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        const string oldToken = "old-refresh-token";
        const string newToken = "new-refresh-token";

        _refreshTokenService
            .Setup(x => x.RotateAsync(oldToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid UserId, string RefreshToken)?)(userId, newToken));

        _identityService
            .Setup(x => x.GetUserEmailAsync(userId))
            .ReturnsAsync(" ");

        var sut = CreateSut();

        var result = await sut.Refresh(
            new RefreshTokenRequest(oldToken),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);

        _refreshTokenService.Verify(
            x => x.RevokeAsync(newToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}