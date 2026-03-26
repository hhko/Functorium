using Functorium.Adapters.Repositories;

namespace AiGovernance.Adapters.Persistence.Assessments;

public class AssessmentModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string ModelId { get; set; } = default!;
    public string DeploymentId { get; set; } = default!;
    public int? OverallScore { get; set; }
    public string Status { get; set; } = default!;
    public DateTime AssessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<CriterionModel> Criteria { get; set; } = [];
}
