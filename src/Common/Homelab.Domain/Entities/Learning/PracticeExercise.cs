using Homelab.Domain;
using Homelab.Domain.Entities.Academic;

namespace Homelab.Domain.Entities.Learning;

public class PracticeExercise : Audit
{
    public Guid ExternalId { get; set; }
    public Guid ProgramModuleId { get; set; }
    public ProgramModule? ProgramModule { get; set; }
    public Guid? MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string? DifficultyLevel { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsOptional { get; set; } = true;
    public string? ResourceUrl { get; set; }
    public string? SolutionUrl { get; set; }
}

