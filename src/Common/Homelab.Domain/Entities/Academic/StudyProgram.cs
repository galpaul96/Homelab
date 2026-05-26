using Homelab.Domain;
using Homelab.Domain.Entities.Enums;
using Homelab.Domain.Entities.Resources;

namespace Homelab.Domain.Entities.Academic;

public class StudyProgram : Audit
{
    public Guid ExternalId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AcademicLevel Level { get; set; }
    public string Language { get; set; } = string.Empty;
    public decimal CreditValue { get; set; }
    public int NominalStudyHours { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ProgramModule> Modules { get; set; } = [];
    public List<ProgramEnrollment> Enrollments { get; set; } = [];
    public List<Cohort> Cohorts { get; set; } = [];
    public List<DownloadDocument> Documents { get; set; } = [];
}

