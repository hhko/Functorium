using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Incidents.Specifications;

/// <summary>
/// 미해결 인시던트 검색 Specification.
/// Reported 또는 Investigating 상태인 인시던트를 만족합니다.
/// </summary>
public sealed class IncidentOpenSpec : ExpressionSpecification<ModelIncident>
{
    public override Expression<Func<ModelIncident, bool>> ToExpression()
    {
        string reportedStr = IncidentStatus.Reported;
        string investigatingStr = IncidentStatus.Investigating;
        return incident => (string)incident.Status == reportedStr
                        || (string)incident.Status == investigatingStr;
    }
}
