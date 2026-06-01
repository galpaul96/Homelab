using Homelab.Api.Ef.EntityConfigurations;
using Homelab.Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homelab.Api.Ef.EntityConfigurations.Licensing
{
    internal class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> configuration)
        {
            configuration.ConfigureAuditedEntity("Products");

            configuration.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(256);

            configuration.Property(o => o.Type).HasMaxLength(128);
            configuration.Property(o => o.HostedOn).HasMaxLength(256);

            configuration.HasIndex(o => o.ClientId);
            configuration.HasIndex(o => o.Name);

            configuration.HasOne(o => o.Client)
                .WithMany(o => o.Products)
                .HasForeignKey(o => o.ClientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
