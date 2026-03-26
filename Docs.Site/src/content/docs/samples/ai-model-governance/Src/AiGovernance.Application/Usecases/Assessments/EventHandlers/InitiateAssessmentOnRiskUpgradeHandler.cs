using Functorium.Applications.Events;
using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Deployments.Specifications;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Application.Usecases.Assessments.EventHandlers;

/// <summary>
/// 위험 등급 상향 시 컴플라이언스 평가 자동 개시 핸들러.
/// 새 위험 등급이 컴플라이언스 평가를 요구하면 활성 배포에 대해 평가를 생성합니다.
/// </summary>
public sealed class InitiateAssessmentOnRiskUpgradeHandler(
    IDeploymentRepository deploymentRepository,
    IAssessmentRepository assessmentRepository)
    : IDomainEventHandler<AIModel.RiskClassifiedEvent>
{
    private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;
    private readonly IAssessmentRepository _assessmentRepository = assessmentRepository;

    public async ValueTask Handle(AIModel.RiskClassifiedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.NewRiskTier.RequiresComplianceAssessment)
            return;

        var spec = new DeploymentByModelSpec(notification.ModelId) & new DeploymentActiveSpec();
        var deploymentsResult = await _deploymentRepository.Find(spec)
            .Run().RunAsync();

        if (deploymentsResult.IsFail)
            return;

        var deployments = deploymentsResult.Unwrap();
        foreach (var deployment in deployments)
        {
            var assessment = ComplianceAssessment.Create(
                notification.ModelId,
                deployment.Id,
                notification.NewRiskTier);

            await _assessmentRepository.Create(assessment).Run().RunAsync();
            // 실패 시 관측성 레이어(ObservableDomainEventNotificationPublisher)가 로깅
        }
    }
}
