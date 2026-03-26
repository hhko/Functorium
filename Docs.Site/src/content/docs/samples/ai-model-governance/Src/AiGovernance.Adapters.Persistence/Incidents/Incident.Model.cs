using Functorium.Adapters.Repositories;

namespace AiGovernance.Adapters.Persistence.Incidents;

public class IncidentModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string DeploymentId { get; set; } = default!;
    public string ModelId { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? ResolutionNote { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
