
using Microsoft.AspNetCore.Identity;

namespace NexaStore.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        string adminEmail)
    {
        string[] roles = ["Admin", "Customer"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>(role));
            }
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            throw new InvalidOperationException(
                $"Admin user '{adminEmail}' was not found. Register this user first.");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(admin, "Admin");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (await userManager.IsInRoleAsync(admin, "Customer"))
        {
            await userManager.RemoveFromRoleAsync(admin, "Customer");
        }
    }
}
