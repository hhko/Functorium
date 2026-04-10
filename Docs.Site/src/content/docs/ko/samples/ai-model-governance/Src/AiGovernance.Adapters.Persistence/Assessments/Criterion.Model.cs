namespace AiGovernance.Adapters.Persistence.Assessments;

public class CriterionModel
{
    public string Id { get; set; } = default!;
    public string AssessmentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? Result { get; set; }
    public string? Notes { get; set; }
    public DateTime? EvaluatedAt { get; set; }
}
