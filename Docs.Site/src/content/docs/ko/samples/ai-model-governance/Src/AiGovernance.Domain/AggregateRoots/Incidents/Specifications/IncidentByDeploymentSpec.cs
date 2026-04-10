using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Domain.AggregateRoots.Incidents.Specifications;

/// <summary>
/// 배포 ID 기반 인시던트 검색 Specification.
/// </summary>
public sealed class IncidentByDeploymentSpec : ExpressionSpecification<ModelIncident>
{
    public ModelDeploymentId DeploymentId { get; }

    public IncidentByDeploymentSpec(ModelDeploymentId deploymentId) => DeploymentId = deploymentId;

    public override Expression<Func<ModelIncident, bool>> ToExpression()
    {
        string deploymentIdStr = DeploymentId.ToString();
        return incident => incident.DeploymentId.ToString() == deploymentIdStr;
    }
}
