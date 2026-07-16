namespace Homelab.Domain.Entities.Web;

public sealed class IdentityAdministrationAudit
{
    public long Id { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public Guid CorrelationId { get; set; }
    public required string Action { get; set; }
    public required string Outcome { get; set; }
    public string? ErrorCode { get; set; }
    public string? ActorUserId { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetRoleId { get; set; }
    public string? Detail { get; set; }
}
