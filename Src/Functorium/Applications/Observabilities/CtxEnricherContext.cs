namespace Functorium.Applications.Observabilities;

/// <summary>
/// ctx.* 필드를 지정된 Pillar(Logging, Tracing, Metrics)에 동시 전파하는 정적 컨텍스트.
/// Adapter 레이어에서 SetPushFactory를 호출하여 실제 전파 구현을 등록합니다.
/// </summary>
public static class CtxEnricherContext
{
    private static Func<string, object?, CtxPillar, IDisposable> _pushFactory
        = static (_, _, _) => NullDisposable.Instance;

    /// <summary>
    /// Multi-target Push 팩토리를 등록합니다.
    /// Adapter 레이어 초기화 시 Serilog LogContext + Activity.SetTag + MetricsTagContext 통합 구현을 등록합니다.
    /// </summary>
    public static void SetPushFactory(Func<string, object?, CtxPillar, IDisposable> factory)
        => _pushFactory = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <summary>
    /// ctx.* 필드를 지정된 Pillar로 동시 전파합니다.
    /// </summary>
    /// <param name="name">ctx 필드 이름 (예: "ctx.customer_id")</param>
    /// <param name="value">필드 값</param>
    /// <param name="pillars">대상 Pillar (기본값: Logging + Tracing)</param>
    /// <returns>Dispose 시 전파된 컨텍스트를 정리하는 IDisposable</returns>
    public static IDisposable Push(string name, object? value, CtxPillar pillars = CtxPillar.Default)
        => _pushFactory(name, value, pillars);

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
