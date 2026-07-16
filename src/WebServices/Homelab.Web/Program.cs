using Homelab.Web.Components;
using Homelab.ServiceDefaults;
using Homelab.Web.Components.Account;
using Homelab.Web.Components.Admin;
using Homelab.Web.Components.User;
using Homelab.Web.Data;
using Homelab.Web.IdentityAdministration;
using Homelab.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Homelab.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.ConfigureServices(builder.Configuration);
        builder.Services.AddOptions<IdentityAdministrationOptions>()
            .Bind(builder.Configuration.GetSection(IdentityAdministrationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !options.SeedDefaultAdministrator || options.DefaultAdminPassword.Length >= 12,
                "DefaultAdminPassword must contain at least 12 characters when administrator seeding is enabled.")
            .ValidateOnStart();
        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });

        builder.Services.Configure<CircuitOptions>(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment();
        });

        builder.AddServiceDefaults();
        builder.AddRedisOutputCache("cache");

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<IdentityAdminService>();
        builder.Services.AddScoped<IIdentityDatabaseInitializer, IdentityDatabaseInitializer>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddSingleton<NotificationUpdateDispatcher>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        builder.Services.AddBlazorBootstrap();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        var connectionString = builder.Configuration.GetConnectionString("WebDatabase")
            ?? throw new InvalidOperationException("Connection string 'WebDatabase' not found.");

        static void ConfigureWebDbContext(DbContextOptionsBuilder options, string connectionString)
        {
            options.UseNpgsql(
                connectionString,
                x => x.MigrationsAssembly("Homelab.Web"));
        }

        builder.Services.AddDbContext<ApplicationDbContext>(
            options => ConfigureWebDbContext(options, connectionString));

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        builder.Services.AddAuthorization(options =>
            options.AddPolicy(IdentityAdministrationConstants.AdministrationPolicy, policy =>
                policy.RequireAuthenticatedUser().RequireRole(IdentityAdministrationConstants.AdminRole)));

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IIdentityDatabaseInitializer>();
            await initializer.InitializeAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.UseOutputCache();
        app.MapDefaultEndpoints();
        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        app.Run();
    }
}
