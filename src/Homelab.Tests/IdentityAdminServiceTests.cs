using Homelab.Web.Components.Admin;
using Homelab.Web.Data;
using Homelab.Web.IdentityAdministration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Homelab.Tests;

public sealed class IdentityAdminServiceTests
{
    [Fact]
    public async Task RemovingAdminRoleRotatesTheUserSecurityStamp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"identity-admin-{Guid.NewGuid():N}"));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.True((await roleManager.CreateAsync(new IdentityRole(IdentityAdministrationConstants.AdminRole))).Succeeded);

        var target = new ApplicationUser { UserName = "target@example.test", Email = "target@example.test" };
        var peer = new ApplicationUser { UserName = "peer@example.test", Email = "peer@example.test" };
        Assert.True((await userManager.CreateAsync(target, "Valid-password-123!")).Succeeded);
        Assert.True((await userManager.CreateAsync(peer, "Valid-password-123!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(target, IdentityAdministrationConstants.AdminRole)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(peer, IdentityAdministrationConstants.AdminRole)).Succeeded);

        var originalStamp = target.SecurityStamp;
        var service = new IdentityAdminService(
            userManager,
            roleManager,
            dbContext,
            new TestAuthenticationStateProvider());

        var result = await service.RemoveRoleFromUserAsync(target.Id, IdentityAdministrationConstants.AdminRole);

        var updatedTarget = await userManager.FindByIdAsync(target.Id);
        Assert.True(result.Succeeded, result.Message);
        Assert.NotNull(updatedTarget);
        Assert.NotEqual(originalStamp, updatedTarget!.SecurityStamp);
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous =
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(Anonymous);
    }
}
