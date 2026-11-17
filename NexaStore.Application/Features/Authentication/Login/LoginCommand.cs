using NexaStore.Application.Abstractions.Identity;
using NexaStore.Application.Abstractions.Authentication;
using NexaStore.Application.Features.Authentication.DTOs;

namespace NexaStore.Application.Features.Authentication.Login;

public sealed record LoginCommand(string Email, string Password);

public sealed class LoginHandler
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await _identityService.ValidateUserAsync(
            command.Email,
            command.Password,
            cancellationToken);

        if (userId is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = await _identityService.GetUserRolesAsync(userId.Value);

        var token = _jwtTokenGenerator.GenerateToken(
            userId.Value,
            command.Email,
            roles);

        return new AuthResponse(
            userId.Value,
            command.Email,
            token);
    }
}