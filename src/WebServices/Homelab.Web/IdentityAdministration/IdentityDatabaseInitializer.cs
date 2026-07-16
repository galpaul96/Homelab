using Homelab.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Homelab.Web.IdentityAdministration;

public sealed class IdentityDatabaseInitializer(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityAdministrationOptions> options,
    ILogger<IdentityDatabaseInitializer> logger) : IIdentityDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var settings = options.Value;
        if (!settings.SeedDefaultAdministrator)
        {
            return;
        }

        var role = await roleManager.FindByNameAsync(settings.AdminRoleName);
        if (role is null)
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(settings.AdminRoleName));
            EnsureSucceeded(roleResult, "create administrator role");
        }

        var user = await userManager.FindByEmailAsync(settings.DefaultAdminEmail);
        if (user is not null)
        {
            logger.LogInformation("Configured identity administrator already exists; preserving existing account state.");
            return;
        }

        user = new ApplicationUser
        {
            UserName = settings.DefaultAdminEmail,
            Email = settings.DefaultAdminEmail,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var userResult = await userManager.CreateAsync(user, settings.DefaultAdminPassword);
        EnsureSucceeded(userResult, "create configured identity administrator");
        EnsureSucceeded(await userManager.AddToRoleAsync(user, settings.AdminRoleName), "assign administrator role");
        EnsureSucceeded(await userManager.AddClaimAsync(user,
            new Claim(IdentityAdministrationConstants.MustChangePasswordClaimType,
                IdentityAdministrationConstants.MustChangePasswordClaimValue)), "set first-login password change claim");

        logger.LogWarning("Created the configured template administrator account. It must change its password immediately.");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var codes = string.Join(",", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"Identity bootstrap failed during {operation}. Error codes: {codes}");
    }
}
