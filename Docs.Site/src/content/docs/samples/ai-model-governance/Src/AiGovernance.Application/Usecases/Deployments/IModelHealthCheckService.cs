using Functorium.Domains.Observabilities;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Application.Usecases.Deployments.Ports;

/// <summary>
/// 모델 헬스 체크 외부 서비스 Port.
/// Infrastructure Adapter에서 구현합니다.
/// </summary>
public interface IModelHealthCheckService : IObservablePort
{
    FinT<IO, HealthCheckResult> CheckHealth(ModelDeploymentId deploymentId);
}

public sealed record HealthCheckResult(
    bool IsHealthy,
    string Status,
    Option<string> ErrorMessage,
    DateTimeOffset CheckedAt);
