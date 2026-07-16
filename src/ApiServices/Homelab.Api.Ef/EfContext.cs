using Homelab.Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace Homelab.Api.Ef
{
    internal class EfContext : DbContext
    {
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Product> Products => Set<Product>();

        public EfContext(DbContextOptions<EfContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp");

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetAssembly(typeof(EfContext))!);
        }
    }

    internal class EfContextFactory : IDesignTimeDbContextFactory<EfContext>
    {
        private const string ApplicationName = "Homelab.Ef";

        public EfContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EfContext>();
            string connectionString;
            if (args != null && args.Any())
            {
                connectionString = args[0];
            }
            else
            {
                connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ApiDatabase")
                    ?? throw new InvalidOperationException("Pass the API database connection string as an EF argument or ConnectionStrings__ApiDatabase.");
            }
            string applicationConnectionString = $"Application Name={ApplicationName};{connectionString}";

            optionsBuilder.UseNpgsql(applicationConnectionString, x =>
            {
                x.EnableRetryOnFailure();
            });

            return new EfContext(optionsBuilder.Options);
        }
    }
}
