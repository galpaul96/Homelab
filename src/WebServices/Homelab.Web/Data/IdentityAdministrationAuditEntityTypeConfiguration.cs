using Homelab.Domain.Entities.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homelab.Web.Data;

internal sealed class IdentityAdministrationAuditEntityTypeConfiguration : IEntityTypeConfiguration<IdentityAdministrationAudit>
{
    public void Configure(EntityTypeBuilder<IdentityAdministrationAudit> builder)
    {
        builder.ToTable("IdentityAdministrationAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.Property(x => x.ActorUserId).HasMaxLength(450);
        builder.Property(x => x.TargetUserId).HasMaxLength(450);
        builder.Property(x => x.TargetRoleId).HasMaxLength(450);
        builder.Property(x => x.Detail).HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.OccurredUtc).IsRequired();
        builder.HasIndex(x => x.OccurredUtc);
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredUtc });
        builder.HasIndex(x => new { x.TargetUserId, x.OccurredUtc });
        builder.HasIndex(x => x.CorrelationId);
    }
}
