using Microsoft.AspNetCore.Identity;
using NexaStore.Application.Abstractions.Identity;
using NexaStore.Application.Common.Exceptions;

namespace NexaStore.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

public async Task<Guid?> RegisterAsync(
    string email,
    string password,
    CancellationToken cancellationToken)
{
    var existingUser = await _userManager.FindByEmailAsync(email);

    if (existingUser is not null)
        return null;

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email
    };

    var result = await _userManager.CreateAsync(user, password);

    // if (!result.Succeeded)
    //     throw new InvalidOperationException(
    //         string.Join(", ", result.Errors.Select(e => e.Description)));

    if (!result.Succeeded)
    throw new InvalidOperationException(
        "User registration failed.");

    var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

    // if (!roleResult.Succeeded)
    // {
    //     await _userManager.DeleteAsync(user);

    //     throw new InvalidOperationException(
    //         string.Join(", ", roleResult.Errors.Select(e => e.Description)));
    // }
    if (!roleResult.Succeeded)
    {
        await _userManager.DeleteAsync(user);

        throw new InvalidOperationException(
            "User role assignment failed.");
    }

    return user.Id;
}

    public async Task<Guid?> ValidateUserAsync(
    string email,
    string password,
    CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            return null;

        if (await _userManager.IsLockedOutAsync(user))
            return null;

        var valid = await _userManager.CheckPasswordAsync(
            user,
            password);

        if (!valid)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return user.Id;
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return [];

        return await _userManager.GetRolesAsync(user);
    }
    
    public async Task<string?> GetUserEmailAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        var user = await _userManager.FindByIdAsync(userId.ToString());

        return user?.Email;
    }
}