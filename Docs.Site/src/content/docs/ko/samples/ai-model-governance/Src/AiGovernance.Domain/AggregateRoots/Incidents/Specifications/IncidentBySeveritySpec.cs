using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Incidents.Specifications;

/// <summary>
/// 심각도 기반 인시던트 검색 Specification.
/// </summary>
public sealed class IncidentBySeveritySpec : ExpressionSpecification<ModelIncident>
{
    public IncidentSeverity Severity { get; }

    public IncidentBySeveritySpec(IncidentSeverity severity) => Severity = severity;

    public override Expression<Func<ModelIncident, bool>> ToExpression()
    {
        string severityStr = Severity;
        return incident => (string)incident.Severity == severityStr;
    }
}
