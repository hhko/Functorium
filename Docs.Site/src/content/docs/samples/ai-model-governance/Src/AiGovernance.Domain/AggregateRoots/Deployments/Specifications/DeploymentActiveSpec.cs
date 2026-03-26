using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Deployments.Specifications;

/// <summary>
/// 활성 배포 검색 Specification.
/// </summary>
public sealed class DeploymentActiveSpec : ExpressionSpecification<ModelDeployment>
{
    public override Expression<Func<ModelDeployment, bool>> ToExpression()
    {
        string activeStr = DeploymentStatus.Active;
        return deployment => (string)deployment.Status == activeStr;
    }
}
