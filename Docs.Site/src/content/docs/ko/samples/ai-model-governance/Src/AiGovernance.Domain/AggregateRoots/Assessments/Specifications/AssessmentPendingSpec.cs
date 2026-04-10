using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Assessments.Specifications;

/// <summary>
/// 미완료 평가 검색 Specification.
/// Initiated 또는 InProgress 상태인 평가를 만족합니다.
/// </summary>
public sealed class AssessmentPendingSpec : ExpressionSpecification<ComplianceAssessment>
{
    public override Expression<Func<ComplianceAssessment, bool>> ToExpression()
    {
        string initiatedStr = AssessmentStatus.Initiated;
        string inProgressStr = AssessmentStatus.InProgress;
        return assessment => (string)assessment.Status == initiatedStr
                          || (string)assessment.Status == inProgressStr;
    }
}
