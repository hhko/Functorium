using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Domain.AggregateRoots.Assessments.Specifications;

/// <summary>
/// 배포 ID 기반 평가 검색 Specification.
/// </summary>
public sealed class AssessmentByDeploymentSpec : ExpressionSpecification<ComplianceAssessment>
{
    public ModelDeploymentId DeploymentId { get; }

    public AssessmentByDeploymentSpec(ModelDeploymentId deploymentId) => DeploymentId = deploymentId;

    public override Expression<Func<ComplianceAssessment, bool>> ToExpression()
    {
        string deploymentIdStr = DeploymentId.ToString();
        return assessment => assessment.DeploymentId.ToString() == deploymentIdStr;
    }
}
