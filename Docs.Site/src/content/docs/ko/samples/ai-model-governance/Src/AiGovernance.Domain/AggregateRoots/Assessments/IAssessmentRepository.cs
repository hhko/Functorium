using Functorium.Domains.Repositories;

namespace AiGovernance.Domain.AggregateRoots.Assessments;

/// <summary>
/// 컴플라이언스 평가 리포지토리 인터페이스 (Command 전용)
/// </summary>
public interface IAssessmentRepository : IRepository<ComplianceAssessment, ComplianceAssessmentId>
{
    /// <summary>
    /// Specification 기반 조회.
    /// </summary>
    FinT<IO, Seq<ComplianceAssessment>> Find(Specification<ComplianceAssessment> spec);
}
