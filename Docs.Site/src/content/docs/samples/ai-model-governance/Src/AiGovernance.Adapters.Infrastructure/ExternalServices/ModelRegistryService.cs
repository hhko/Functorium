using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Observabilities;
using AiGovernance.Domain.AggregateRoots.Models;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace AiGovernance.Adapters.Infrastructure.ExternalServices;

/// <summary>
/// 모델 레지스트리 조회 결과 레코드
/// </summary>
public sealed record ModelRegistryEntry(
    string ModelName,
    string Version,
    string Framework,
    string Checksum,
    DateTimeOffset RegisteredAt);

/// <summary>
/// 레지스트리 세션 (Bracket 패턴에서 acquire/release 대상)
/// </summary>
public sealed class RegistrySession : IDisposable
{
    public string SessionId { get; }
    public bool IsOpen { get; private set; }

    public RegistrySession(string sessionId)
    {
        SessionId = sessionId;
        IsOpen = true;
    }

    public void Dispose()
    {
        IsOpen = false;
    }
}

/// <summary>
/// 모델 레지스트리 외부 서비스 Port.
/// Infrastructure Adapter에서 구현합니다.
/// </summary>
public interface IModelRegistryService : IObservablePort
{
    FinT<IO, ModelRegistryEntry> LookupModel(AIModelId modelId);
}

/// <summary>
/// 모델 레지스트리 외부 서비스 구현.
/// IO.Bracket 패턴을 보여줍니다:
///   AcquireSession.Bracket(Use: session → 조회, Release: session.Dispose) → result
/// </summary>
[GenerateObservablePort]
public class ModelRegistryService : IModelRegistryService
{
    #region Error Types

    public sealed record RegistryLookupFailed : AdapterErrorType.Custom;
    public sealed record SessionAcquisitionFailed : AdapterErrorType.Custom;
    public sealed record ModelNotFoundInRegistry : AdapterErrorType.Custom;

    #endregion

    private static readonly Random _random = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "ExternalService";

    public virtual FinT<IO, ModelRegistryEntry> LookupModel(AIModelId modelId)
    {
        // Acquire: 레지스트리 세션 획득
        var acquireSession = IO.liftAsync<RegistrySession>(async env =>
        {
            // 세션 획득 지연 시뮬레이션 (50~150ms)
            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(50, 150)), env.Token);

            // 간헐적 세션 획득 실패 (5% 확률)
            if (_random.Next(100) < 5)
                throw new InvalidOperationException("Failed to acquire registry session: connection pool exhausted");

            return new RegistrySession(Guid.NewGuid().ToString("N")[..8]);
        });

        var io = acquireSession.Bracket(
            Use: session => IO.liftAsync<ModelRegistryEntry>(async env =>
            {
                // 레지스트리 조회 지연 시뮬레이션 (100~400ms)
                await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 400)), env.Token);

                // 간헐적 조회 실패 (5% 확률)
                if (_random.Next(100) < 5)
                    throw new InvalidOperationException(
                        $"Registry lookup failed for model {modelId} in session {session.SessionId}");

                // Mock 데이터 반환
                return new ModelRegistryEntry(
                    ModelName: $"model-{modelId.ToString()[..8]}",
                    Version: $"{_random.Next(1, 5)}.{_random.Next(0, 10)}.{_random.Next(0, 100)}",
                    Framework: _random.Next(2) == 0 ? "PyTorch" : "TensorFlow",
                    Checksum: Guid.NewGuid().ToString("N"),
                    RegisteredAt: DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 365)));
            }),
            Fin: session => IO.lift(() =>
            {
                // Release: 세션 해제 (성공/실패 무관)
                session.Dispose();
                return unit;
            }));

        var result = io.Catch(
            e => e.IsExceptional,
            e => IO.fail<ModelRegistryEntry>(
                AdapterError.FromException<ModelRegistryService>(
                    new RegistryLookupFailed(),
                    e.ToException())));

        return new FinT<IO, ModelRegistryEntry>(result.Map(Fin.Succ));
    }
}
