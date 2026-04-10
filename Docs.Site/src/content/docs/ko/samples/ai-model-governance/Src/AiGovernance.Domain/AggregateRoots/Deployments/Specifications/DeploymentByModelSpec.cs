using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Domain.AggregateRoots.Deployments.Specifications;

/// <summary>
/// 모델 ID 기반 배포 검색 Specification.
/// </summary>
public sealed class DeploymentByModelSpec : ExpressionSpecification<ModelDeployment>
{
    public AIModelId ModelId { get; }

    public DeploymentByModelSpec(AIModelId modelId) => ModelId = modelId;

    public override Expression<Func<ModelDeployment, bool>> ToExpression()
    {
        string modelIdStr = ModelId.ToString();
        return deployment => deployment.ModelId.ToString() == modelIdStr;
    }
}
