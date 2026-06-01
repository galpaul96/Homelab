using Homelab.Api.Ef.EntityConfigurations;
using Homelab.Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homelab.Api.Ef.EntityConfigurations.Licensing
{
    internal class ClientEntityTypeConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> configuration)
        {
            configuration.ConfigureAuditedEntity("Clients");

            configuration.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(256);

            configuration.HasIndex(o => o.Name);
        }
    }
}
