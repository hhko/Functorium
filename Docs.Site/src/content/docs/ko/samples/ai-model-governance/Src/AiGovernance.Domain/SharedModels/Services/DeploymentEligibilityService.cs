using Functorium.Domains.Errors;
using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Assessments.Specifications;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Incidents.Specifications;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;
using static Functorium.Domains.Errors.DomainErrorKind;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.SharedModels.Services;

/// <summary>
/// 배포 적격성 검증 도메인 서비스.
/// AI 모델이 배포 가능한 상태인지 교차 Aggregate 비즈니스 규칙을 검증합니다.
/// </summary>
public sealed class DeploymentEligibilityService : IDomainService
{
    #region Error Types

    public sealed record ProhibitedModel : DomainErrorKind.Custom;
    public sealed record ComplianceAssessmentRequired : DomainErrorKind.Custom;
    public sealed record OpenIncidentsExist : DomainErrorKind.Custom;

    #endregion

    /// <summary>
    /// AI 모델의 배포 적격성을 검증합니다.
    /// Check 1: 금지된 위험 등급 확인
    /// Check 2: 고위험 등급 시 컴플라이언스 평가 통과 확인
    /// Check 3: 미해결 인시던트 부재 확인
    /// </summary>
    public FinT<IO, Unit> ValidateEligibility(
        AIModel model,
        IAssessmentRepository assessmentRepository,
        IIncidentRepository incidentRepository)
    {
        return
            from _1 in CheckNotProhibited(model)
            from _2 in CheckComplianceAssessment(model, assessmentRepository)
            from _3 in CheckNoOpenIncidents(model, incidentRepository)
            select unit;
    }

    private static FinT<IO, Unit> CheckNotProhibited(AIModel model)
    {
        if (model.RiskTier.IsProhibited)
            return FinT.lift<IO, Unit>(
                Fin.Fail<Unit>(DomainError.For<DeploymentEligibilityService>(
                    new ProhibitedModel(),
                    model.Id.ToString(),
                    $"Model '{model.Name}' is classified as '{model.RiskTier}' and cannot be deployed")));

        return FinT.lift<IO, Unit>(Fin.Succ(unit));
    }

    private static FinT<IO, Unit> CheckComplianceAssessment(
        AIModel model,
        IAssessmentRepository assessmentRepository)
    {
        if (!model.RiskTier.RequiresComplianceAssessment)
            return FinT.lift<IO, Unit>(Fin.Succ(unit));

        return
            from assessments in assessmentRepository.Find(new AssessmentByModelSpec(model.Id))
            from result in EnsurePassedAssessmentExists(model, assessments)
            select result;
    }

    private static FinT<IO, Unit> EnsurePassedAssessmentExists(AIModel model, Seq<ComplianceAssessment> assessments)
    {
        var hasPassed = assessments.Any(a => a.Status == AssessmentStatus.Passed);
        if (!hasPassed)
            return FinT.lift<IO, Unit>(
                Fin.Fail<Unit>(DomainError.For<DeploymentEligibilityService>(
                    new ComplianceAssessmentRequired(),
                    model.Id.ToString(),
                    $"Model '{model.Name}' requires a passed compliance assessment for deployment")));

        return FinT.lift<IO, Unit>(Fin.Succ(unit));
    }

    private static FinT<IO, Unit> CheckNoOpenIncidents(
        AIModel model,
        IIncidentRepository incidentRepository)
    {
        return
            from incidents in incidentRepository.Find(new IncidentByModelSpec(model.Id))
            from result in EnsureNoOpenIncidents(model, incidents)
            select result;
    }

    private static FinT<IO, Unit> EnsureNoOpenIncidents(AIModel model, Seq<ModelIncident> incidents)
    {
        var openIncidents = incidents.Filter(i =>
            i.Status == IncidentStatus.Reported || i.Status == IncidentStatus.Investigating);

        if (openIncidents.Any())
            return FinT.lift<IO, Unit>(
                Fin.Fail<Unit>(DomainError.For<DeploymentEligibilityService, int>(
                    new OpenIncidentsExist(),
                    openIncidents.Count,
                    $"Model '{model.Name}' has {openIncidents.Count} open incidents")));

        return FinT.lift<IO, Unit>(Fin.Succ(unit));
    }
}
