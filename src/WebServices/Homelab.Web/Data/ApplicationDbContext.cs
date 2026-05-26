using Homelab.Domain.Entities.Web;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Homelab.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new UserNotificationEntityTypeConfiguration());
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
            : "Host=localhost;Port=5432;Database=Homelab.Web;Username=postgres;Password=postgres";

        string applicationConnectionString = $"Application Name={ApplicationName};{connectionString}";

        optionsBuilder.UseNpgsql(applicationConnectionString, x =>
        {
            x.EnableRetryOnFailure();
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
