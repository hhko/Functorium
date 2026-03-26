using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Domain.AggregateRoots.Assessments.Specifications;

/// <summary>
/// 모델 ID 기반 평가 검색 Specification.
/// </summary>
public sealed class AssessmentByModelSpec : ExpressionSpecification<ComplianceAssessment>
{
    public AIModelId ModelId { get; }

    public AssessmentByModelSpec(AIModelId modelId) => ModelId = modelId;

    public override Expression<Func<ComplianceAssessment, bool>> ToExpression()
    {
        string modelIdStr = ModelId.ToString();
        return assessment => assessment.ModelId.ToString() == modelIdStr;
    }
}
