using Functorium.Applications.Events;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Deployments.Specifications;
using AiGovernance.Domain.AggregateRoots.Incidents;

namespace AiGovernance.Application.Usecases.Incidents.EventHandlers;

/// <summary>
/// 심각한 인시던트 발생 시 배포 자동 격리 핸들러.
/// Critical 또는 High 심각도 인시던트 발생 시 해당 배포의 활성 배포를 격리합니다.
/// </summary>
public sealed class QuarantineDeploymentOnCriticalIncidentHandler(
    IDeploymentRepository deploymentRepository)
    : IDomainEventHandler<ModelIncident.ReportedEvent>
{
    private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;

    public async ValueTask Handle(ModelIncident.ReportedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Severity.RequiresQuarantine)
            return;

        var result = await _deploymentRepository.GetById(notification.DeploymentId)
            .Run().RunAsync();

        if (result.IsFail)
            return;

        var deployment = result.Unwrap();
        var quarantineResult = deployment.Quarantine(
            $"Auto-quarantined due to {notification.Severity} incident: {notification.IncidentId}");

        if (quarantineResult.IsSucc)
        {
            await _deploymentRepository.Update(deployment).Run().RunAsync();
            // 실패 시 관측성 레이어(ObservableDomainEventNotificationPublisher)가 로깅
        }
    }
}
