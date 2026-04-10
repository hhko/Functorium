using Functorium.Adapters.Repositories;

namespace AiGovernance.Adapters.Persistence.Deployments;

public class DeploymentModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string ModelId { get; set; } = default!;
    public string EndpointUrl { get; set; } = default!;
    public string Environment { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal DriftThreshold { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public DateTime DeployedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
