using Microsoft.Extensions.Logging;

namespace Functorium.Abstractions.Observabilities;

/// <summary>
/// Adapter 구현 코드 내에서 운영 목적의 로그를 출력하는 정적 API.
/// Observable 래퍼가 설정한 공통 컨텍스트(layer, category, handler, method)를 자동으로 포함합니다.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pillar 범위</b>: Logging + 조건부 Tracing (Metrics 제외)
/// </para>
/// <list type="bullet">
/// <item>Debug → Logging만</item>
/// <item>Warning → Logging + Activity Event</item>
/// <item>Error → Logging + Activity Event</item>
/// </list>
/// <para>
/// <b>사용 예시</b>:
/// </para>
/// <code>
/// // Polly 재시도 시 Warning
/// ObservableSignal.Warning("Retry attempt {Attempt}/{MaxRetry} after {Delay}s delay",
///     ("adapter.retry.attempt", attempt),
///     ("adapter.retry.delay_ms", delay.TotalMilliseconds));
///
/// // 캐시 미스 시 Debug
/// ObservableSignal.Debug("Cache miss", ("adapter.cache.key", cacheKey));
///
/// // 재시도 소진 시 Error
/// ObservableSignal.Error(ex, "Database operation failed after exhausting retries",
///     ("adapter.db.retry.attempt", maxRetries));
/// </code>
/// </remarks>
public static class ObservableSignal
{
    private static IObservableSignalFactory _factory = NullObservableSignalFactory.Instance;

    /// <summary>
    /// 실제 로깅 구현을 등록합니다.
    /// Adapter 레이어 초기화 시 호출됩니다.
    /// </summary>
    public static void SetFactory(IObservableSignalFactory factory)
        => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <summary>
    /// Debug 수준 로그를 출력합니다. Logging만 사용합니다 (Tracing 제외).
    /// 고빈도 이벤트(캐시 미스, 쿼리 상세)에 적합합니다.
    /// </summary>
    public static void Debug(string message, params (string Key, object? Value)[] context)
        => _factory.Log(LogLevel.Debug, message, context, null);

    /// <summary>
    /// Warning 수준 로그를 출력합니다. Logging + Activity Event를 사용합니다.
    /// 자동 복구 가능한 열화(재시도, 폴백, rate limit, 느린 쿼리)에 적합합니다.
    /// </summary>
    public static void Warning(string message, params (string Key, object? Value)[] context)
        => _factory.Log(LogLevel.Warning, message, context, null);

    /// <summary>
    /// Error 수준 로그를 출력합니다. Logging + Activity Event를 사용합니다.
    /// 복구 불가 실패(재시도 소진, 서킷 오픈, DLQ 이동)에 적합합니다.
    /// </summary>
    public static void Error(string message, params (string Key, object? Value)[] context)
        => _factory.Log(LogLevel.Error, message, context, null);

    /// <summary>
    /// Error 수준 로그를 예외와 함께 출력합니다. Logging + Activity Event를 사용합니다.
    /// </summary>
    public static void Error(Exception exception, string message, params (string Key, object? Value)[] context)
        => _factory.Log(LogLevel.Error, message, context, exception);

    /// <summary>
    /// 팩토리 미등록 시 no-op 동작을 제공하는 기본 구현.
    /// </summary>
    private sealed class NullObservableSignalFactory : IObservableSignalFactory
    {
        public static readonly NullObservableSignalFactory Instance = new();
        public void Log(LogLevel level, string message, (string Key, object? Value)[]? context, Exception? exception) { }
    }
}
