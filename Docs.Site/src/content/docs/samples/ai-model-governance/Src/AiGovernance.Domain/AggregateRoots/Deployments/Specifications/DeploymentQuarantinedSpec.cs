using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Deployments.Specifications;

/// <summary>
/// 격리된 배포 검색 Specification.
/// </summary>
public sealed class DeploymentQuarantinedSpec : ExpressionSpecification<ModelDeployment>
{
    public override Expression<Func<ModelDeployment, bool>> ToExpression()
    {
        string quarantinedStr = DeploymentStatus.Quarantined;
        return deployment => (string)deployment.Status == quarantinedStr;
    }
}
