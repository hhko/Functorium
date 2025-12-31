using Functorium.Applications.Observabilities.Spans;

namespace Functorium.Applications.Observabilities.Context;

/// <summary>
/// 관찰 가능성 컨텍스트의 전파를 관리하는 인터페이스입니다.
/// 비동기 경계를 넘어 컨텍스트를 유지하고 전파합니다.
/// </summary>
public interface IContextPropagator
{
    /// <summary>
    /// 현재 컨텍스트를 가져옵니다.
    /// </summary>
    IObservabilityContext? Current { get; }

    /// <summary>
    /// 지정된 컨텍스트로 스코프를 생성합니다.
    /// using 문과 함께 사용하여 스코프 종료 시 이전 컨텍스트로 자동 복원됩니다.
    /// </summary>
    /// <param name="context">설정할 컨텍스트</param>
    /// <returns>Dispose 시 이전 컨텍스트를 복원하는 스코프</returns>
    IDisposable CreateScope(IObservabilityContext context);

    /// <summary>
    /// Span에서 컨텍스트를 추출합니다.
    /// </summary>
    /// <param name="span">컨텍스트를 추출할 Span</param>
    /// <returns>추출된 컨텍스트 또는 null</returns>
    IObservabilityContext? ExtractContext(ISpan? span);
}
