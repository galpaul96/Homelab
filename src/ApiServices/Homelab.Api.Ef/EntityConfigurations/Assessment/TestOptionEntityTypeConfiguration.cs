using Homelab.Api.Ef.EntityConfigurations;
using Homelab.Domain.Entities.Assessment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homelab.Api.Ef.EntityConfigurations.Assessment
{
    internal class TestOptionEntityTypeConfiguration : IEntityTypeConfiguration<TestOption>
    {
        public void Configure(EntityTypeBuilder<TestOption> configuration)
        {
            configuration.ConfigureAuditedEntity("TestOptions");

            configuration.HasIndex(o => new { o.TestQuestionId, o.SortOrder });

            configuration.HasOne(o => o.TestQuestion)
                .WithMany(o => o.Options)
                .HasForeignKey(o => o.TestQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
