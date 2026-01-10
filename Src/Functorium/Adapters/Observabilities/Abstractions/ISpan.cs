using LanguageExt.Common;

namespace Functorium.Adapters.Observabilities.Abstractions;

/// <summary>
/// 기술 독립적인 추적 Span을 나타냅니다.
/// 하나의 작업 단위(operation)를 추적하며, 시작과 종료 사이의 실행을 기록합니다.
/// </summary>
/// <remarks>
/// OpenTelemetry의 Activity를 추상화한 인터페이스입니다.
/// Dispose 호출 시 Span이 종료됩니다.
/// </remarks>
public interface ISpan : IDisposable
{
    /// <summary>
    /// 현재 Span의 고유 식별자입니다.
    /// </summary>
    string SpanId { get; }

    /// <summary>
    /// 이 Span이 속한 추적의 고유 식별자입니다.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// 이 Span의 관찰 가능성 컨텍스트를 반환합니다.
    /// 자식 Span 생성 시 부모 컨텍스트로 사용할 수 있습니다.
    /// </summary>
    IObservabilityContext Context { get; }

    /// <summary>
    /// Span에 태그(속성)를 설정합니다.
    /// </summary>
    /// <param name="key">태그 키</param>
    /// <param name="value">태그 값</param>
    void SetTag(string key, object? value);

    /// <summary>
    /// Span을 성공 상태로 표시합니다.
    /// </summary>
    /// <param name="elapsedMs">선택적 경과 시간(밀리초)</param>
    void SetSuccess(double? elapsedMs = null);

    /// <summary>
    /// Span을 실패 상태로 표시합니다.
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="elapsedMs">선택적 경과 시간(밀리초)</param>
    void SetFailure(string message, double? elapsedMs = null);

    /// <summary>
    /// Span을 실패 상태로 표시합니다.
    /// </summary>
    /// <param name="error">오류 정보</param>
    /// <param name="elapsedMs">선택적 경과 시간(밀리초)</param>
    void SetFailure(Error error, double? elapsedMs = null);
}
