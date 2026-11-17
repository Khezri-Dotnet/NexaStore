using NexaStore.Application.Abstractions.Identity;

namespace NexaStore.Application.Features.Authentication.Register;

public sealed record RegisterCommand(string Email, string Password);

public sealed class RegisterHandler
{
    private readonly IIdentityService _identityService;

    public RegisterHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Guid> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await _identityService.RegisterAsync(
            command.Email,
            command.Password,
            cancellationToken);

        if (userId is null)
            throw new InvalidOperationException("User already exists.");

        return userId.Value;
    }
}