using Functorium.Domains.Observabilities;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Application.Usecases.Deployments.Ports;

/// <summary>
/// 모델 모니터링 외부 서비스 Port.
/// Infrastructure Adapter에서 구현합니다.
/// </summary>
public interface IModelMonitoringService : IObservablePort
{
    FinT<IO, DriftReport> GetDriftReport(ModelDeploymentId deploymentId);
}

public sealed record DriftReport(
    decimal CurrentDrift,
    decimal Threshold,
    bool IsDrifting,
    DateTimeOffset ReportedAt);
