
namespace NexaStore.Application.Abstractions.Authentication;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Guid?> ValidateAsync(
        string token,
        CancellationToken cancellationToken);

    Task RevokeAsync(
        string token,
        CancellationToken cancellationToken);

    Task<(Guid UserId, string RefreshToken)?> RotateAsync(
    string token,
    CancellationToken cancellationToken);
}
