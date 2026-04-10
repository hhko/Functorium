using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Abstractions.Observabilities;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace AiGovernance.Adapters.Infrastructure.ExternalServices;

/// <summary>
/// 모델 모니터링 외부 서비스 구현.
/// IO.Retry + Schedule 패턴을 보여줍니다:
///   IO.liftAsync → .Retry(Schedule.exponential | jitter | recurs) → .Catch(final error)
/// </summary>
[GenerateObservablePort]
public class ModelMonitoringService : IModelMonitoringService
{
    #region Error Types

    public sealed record MonitoringFailed : AdapterErrorType.Custom;

    #endregion

    private static readonly Random _random = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "ExternalService";

    /// <summary>
    /// 지수 백오프 + 지터 + 최대 3회 재시도 스케줄
    /// </summary>
    private static readonly Schedule RetrySchedule =
        Schedule.exponential(TimeSpan.FromMilliseconds(100))
        | Schedule.jitter(0.3)
        | Schedule.recurs(3)
        | Schedule.maxDelay(TimeSpan.FromSeconds(5));

    public virtual FinT<IO, DriftReport> GetDriftReport(ModelDeploymentId deploymentId)
    {
        // 호출 횟수를 추적하여 재시도 시뮬레이션에 활용
        var attemptCount = 0;

        var io = IO.liftAsync<DriftReport>(async env =>
            {
                Interlocked.Increment(ref attemptCount);

                // 네트워크 지연 시뮬레이션 (50~200ms)
                await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(50, 200)), env.Token);

                // 간헐적 실패 시뮬레이션 (처음 두 번은 60% 확률로 실패, 이후는 10%)
                var failRate = attemptCount <= 2 ? 60 : 10;
                if (_random.Next(100) < failRate)
                    throw new InvalidOperationException(
                        $"Monitoring service temporarily unavailable (attempt {attemptCount})");

                var drift = (decimal)(_random.NextDouble() * 0.5); // 0.0 ~ 0.5
                var threshold = 0.3m;
                return new DriftReport(
                    CurrentDrift: drift,
                    Threshold: threshold,
                    IsDrifting: drift > threshold,
                    ReportedAt: DateTimeOffset.UtcNow);
            })
            .Retry(RetrySchedule)
            .Catch(
                e => e.IsExceptional,
                e => IO.fail<DriftReport>(
                    AdapterError.FromException<ModelMonitoringService>(
                        new MonitoringFailed(),
                        e.ToException())));

        return new FinT<IO, DriftReport>(io.Map(Fin.Succ));
    }
}
