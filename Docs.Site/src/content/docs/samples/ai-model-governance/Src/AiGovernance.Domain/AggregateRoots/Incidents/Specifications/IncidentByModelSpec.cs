using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Domain.AggregateRoots.Incidents.Specifications;

/// <summary>
/// 모델 ID 기반 인시던트 검색 Specification.
/// </summary>
public sealed class IncidentByModelSpec : ExpressionSpecification<ModelIncident>
{
    public AIModelId ModelId { get; }

    public IncidentByModelSpec(AIModelId modelId) => ModelId = modelId;

    public override Expression<Func<ModelIncident, bool>> ToExpression()
    {
        string modelIdStr = ModelId.ToString();
        return incident => incident.ModelId.ToString() == modelIdStr;
    }
}
