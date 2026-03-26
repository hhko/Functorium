using System.Collections.Concurrent;
using AiGovernance.Domain.AggregateRoots.Assessments;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;

namespace AiGovernance.Adapters.Persistence.Assessments.Repositories;

/// <summary>
/// 메모리 기반 컴플라이언스 평가 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class AssessmentRepositoryInMemory
    : InMemoryRepositoryBase<ComplianceAssessment, ComplianceAssessmentId>, IAssessmentRepository
{
    internal static readonly ConcurrentDictionary<ComplianceAssessmentId, ComplianceAssessment> Assessments = new();
    protected override ConcurrentDictionary<ComplianceAssessmentId, ComplianceAssessment> Store => Assessments;

    public AssessmentRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── Assessment 고유 메서드 ──────────────────────

    public virtual FinT<IO, bool> Exists(Specification<ComplianceAssessment> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Assessments.Values.Any(a => spec.IsSatisfiedBy(a));
            return Fin.Succ(exists);
        });
    }

    public virtual FinT<IO, Seq<ComplianceAssessment>> Find(Specification<ComplianceAssessment> spec)
    {
        return IO.lift(() =>
        {
            var result = Assessments.Values
                .Where(a => spec.IsSatisfiedBy(a))
                .ToList();
            return Fin.Succ(toSeq(result));
        });
    }
}
