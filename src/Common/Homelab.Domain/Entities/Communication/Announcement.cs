using Homelab.Domain;
using Homelab.Domain.Entities.Academic;
using Homelab.Domain.Entities.Enums;

namespace Homelab.Domain.Entities.Communication;

public class Announcement : Audit
{
    public Guid ExternalId { get; set; }
    public Guid ModuleOfferingId { get; set; }
    public ModuleOffering? ModuleOffering { get; set; }
    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public CommunicationAudience Audience { get; set; }
    public MessagePriority Priority { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
}

