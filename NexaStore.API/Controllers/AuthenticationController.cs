
using Microsoft.AspNetCore.Mvc;
using NexaStore.Application.Features.Authentication.Register;
using NexaStore.Application.Features.Authentication.Login;
using NexaStore.Application.Features.Authentication.DTOs;
using NexaStore.Application.Abstractions.Authentication;
using NexaStore.Application.Abstractions.Identity;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly RegisterHandler _registerHandler;
    private readonly LoginHandler _loginHandler;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthenticationController(
        RegisterHandler registerHandler,
        LoginHandler loginHandler,
        IRefreshTokenService refreshTokenService,
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshTokenService = refreshTokenService;
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await _registerHandler.Handle(
            new RegisterCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(new { UserId = userId });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _loginHandler.Handle(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);

        if (result is null)
            return Unauthorized();

        var (userId, newRefreshToken) = result.Value;

        var email = await _identityService.GetUserEmailAsync(userId);

        if (string.IsNullOrWhiteSpace(email))
        {
            await _refreshTokenService.RevokeAsync(
                newRefreshToken,
                cancellationToken);

            return Unauthorized();
        }

        var roles = await _identityService.GetUserRolesAsync(userId);

        var accessToken = _jwtTokenGenerator.GenerateToken(
            userId,
            email,
            roles);

        return Ok(new AuthResponse(
            userId,
            email,
            accessToken,
            newRefreshToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest();

        await _refreshTokenService.RevokeAsync(
            request.RefreshToken,
            cancellationToken);

        return NoContent();
    }
}
