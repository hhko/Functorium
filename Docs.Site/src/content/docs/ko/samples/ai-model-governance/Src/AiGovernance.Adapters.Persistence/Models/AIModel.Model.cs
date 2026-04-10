using Functorium.Adapters.Repositories;

namespace AiGovernance.Adapters.Persistence.Models;

public class AIModelModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Purpose { get; set; } = default!;
    public string RiskTier { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
