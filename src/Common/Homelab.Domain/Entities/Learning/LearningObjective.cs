using Homelab.Domain;
using Homelab.Domain.Entities.Academic;

namespace Homelab.Domain.Entities.Learning;

public class LearningObjective : Audit
{
    public Guid ExternalId { get; set; }
    public Guid ProgramModuleId { get; set; }
    public ProgramModule? ProgramModule { get; set; }
    public Guid? MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? BloomLevel { get; set; }
    public bool IsAssessed { get; set; }
}

