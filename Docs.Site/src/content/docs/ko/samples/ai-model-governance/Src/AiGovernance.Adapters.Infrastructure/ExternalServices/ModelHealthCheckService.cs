using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Abstractions.Observabilities;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace AiGovernance.Adapters.Infrastructure.ExternalServices;

/// <summary>
/// 모델 헬스 체크 외부 서비스 구현.
/// IO.Timeout + Catch 패턴을 보여줍니다:
///   IO.liftAsync → .Timeout(10s) → .Catch(timeout → fallback) → .Catch(exception → error)
/// </summary>
[GenerateObservablePort]
public class ModelHealthCheckService : IModelHealthCheckService
{
    #region Error Types

    public sealed record HealthCheckFailed : AdapterErrorType.Custom;
    public sealed record HealthCheckTimedOut : AdapterErrorType.Custom;

    #endregion

    private static readonly Random _random = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "ExternalService";

    public virtual FinT<IO, HealthCheckResult> CheckHealth(ModelDeploymentId deploymentId)
    {
        var io = IO.liftAsync<HealthCheckResult>(async env =>
            {
                // 네트워크 지연 시뮬레이션 (50~300ms, 간헐적으로 12초 → 타임아웃 유도)
                var delay = _random.Next(100) < 10
                    ? TimeSpan.FromSeconds(12) // 10% 확률로 타임아웃 유도
                    : TimeSpan.FromMilliseconds(_random.Next(50, 300));
                await Task.Delay(delay, env.Token);

                // 간헐적 실패 시뮬레이션 (5% 확률)
                if (_random.Next(100) < 5)
                    throw new InvalidOperationException("Health check endpoint returned 503");

                var isHealthy = _random.Next(100) < 85; // 85% 확률로 healthy
                return new HealthCheckResult(
                    IsHealthy: isHealthy,
                    Status: isHealthy ? "Healthy" : "Degraded",
                    ErrorMessage: isHealthy ? Option<string>.None : Some("Model response latency exceeds threshold"),
                    CheckedAt: DateTimeOffset.UtcNow);
            })
            .Timeout(TimeSpan.FromSeconds(10))
            .Catch(
                e => e.Is(Errors.TimedOut),
                _ => IO.pure(new HealthCheckResult(
                    IsHealthy: false,
                    Status: "TimedOut",
                    ErrorMessage: Some("Health check timed out after 10 seconds"),
                    CheckedAt: DateTimeOffset.UtcNow)))
            .Catch(
                e => e.IsExceptional,
                e => IO.fail<HealthCheckResult>(
                    AdapterError.FromException<ModelHealthCheckService>(
                        new HealthCheckFailed(),
                        e.ToException())));

        return new FinT<IO, HealthCheckResult>(io.Map(Fin.Succ));
    }
}
