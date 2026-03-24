namespace Functorium.Applications.Observabilities;

/// <summary>
/// ctx.* 프로퍼티가 전파될 관측 가능성 Pillar.
/// Metrics 관련 옵션은 카디널리티 위험이 있으므로 명시적으로 지정해야 합니다.
/// </summary>
[Flags]
public enum CtxPillar
{
    /// <summary>Serilog LogContext에 Push (구조화 로그 필드)</summary>
    Logging = 1,

    /// <summary>Activity.Current?.SetTag (Span Attribute)</summary>
    Tracing = 2,

    /// <summary>Metrics TagList 차원 (저카디널리티 전용)</summary>
    MetricsTag = 4,

    /// <summary>Metrics Histogram/Counter 값 기록 (수치 전용)</summary>
    MetricsValue = 8,

    /// <summary>Logging + Tracing (기본값)</summary>
    Default = Logging | Tracing,

    /// <summary>Logging + Tracing + MetricsTag</summary>
    All = Logging | Tracing | MetricsTag,
}
