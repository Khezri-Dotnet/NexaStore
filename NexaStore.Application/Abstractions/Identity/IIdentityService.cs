namespace NexaStore.Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<Guid?> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Guid?> ValidateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<IList<string>> GetUserRolesAsync(Guid userId);
}