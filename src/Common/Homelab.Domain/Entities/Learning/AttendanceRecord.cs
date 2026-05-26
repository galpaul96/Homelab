using Homelab.Domain;
using Homelab.Domain.Entities.Enums;

namespace Homelab.Domain.Entities.Learning;

public class AttendanceRecord : Audit
{
    public Guid ExternalId { get; set; }
    public Guid StudentId { get; set; }
    public Guid MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public AttendanceStatus Status { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }
    public string? Notes { get; set; }
}

