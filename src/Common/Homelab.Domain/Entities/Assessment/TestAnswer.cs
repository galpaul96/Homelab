using Homelab.Domain;

namespace Homelab.Domain.Entities.Assessment;

public class TestAnswer : Audit
{
    public Guid ExternalId { get; set; }
    public Guid TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }
    public Guid TestQuestionId { get; set; }
    public TestQuestion? TestQuestion { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public TestOption? SelectedOption { get; set; }

    public string? ResponseText { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? PointsAwarded { get; set; }
}

