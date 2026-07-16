using Homelab.Domain.Entities.Web;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Homelab.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<IdentityAdministrationAudit> IdentityAdministrationAudits => Set<IdentityAdministrationAudit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new UserNotificationEntityTypeConfiguration());
        builder.ApplyConfiguration(new IdentityAdministrationAuditEntityTypeConfiguration());
    }
}
internal class EfContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ApplicationName = "Homelab.Ef";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        string connectionString = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("ConnectionStrings__WebDatabase")
                ?? throw new InvalidOperationException("Pass the web database connection string as an EF argument or ConnectionStrings__WebDatabase.");

        string applicationConnectionString = $"Application Name={ApplicationName};{connectionString}";

        optionsBuilder.UseNpgsql(applicationConnectionString, x =>
        {
            x.EnableRetryOnFailure();
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
