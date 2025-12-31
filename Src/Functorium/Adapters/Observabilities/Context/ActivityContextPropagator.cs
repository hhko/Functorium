using System.Diagnostics;
using Functorium.Applications.Observabilities.Context;
using Functorium.Applications.Observabilities.Spans;

namespace Functorium.Adapters.Observabilities.Context;

/// <summary>
/// OpenTelemetry Activity 기반의 IContextPropagator 구현체입니다.
/// </summary>
public sealed class ActivityContextPropagator : IContextPropagator
{
    /// <summary>
    /// 현재 관찰 가능성 컨텍스트를 가져옵니다.
    /// AsyncLocal에 저장된 컨텍스트를 우선 반환하고, 없으면 Activity.Current를 사용합니다.
    /// </summary>
    public IObservabilityContext? Current
    {
        get
        {
            // AsyncLocal에 저장된 컨텍스트 우선
            IObservabilityContext? storedContext = ActivityContextHolder.GetCurrentContext();
            if (storedContext != null)
                return storedContext;

            // AsyncLocal Activity 확인
            Activity? storedActivity = ActivityContextHolder.GetCurrentActivity();
            if (storedActivity != null)
                return ObservabilityContext.FromActivity(storedActivity);

            // Activity.Current 폴백
            Activity? currentActivity = Activity.Current;
            if (currentActivity != null)
                return ObservabilityContext.FromActivity(currentActivity);

            return null;
        }
    }

    /// <summary>
    /// 지정된 컨텍스트로 스코프를 생성합니다.
    /// </summary>
    public IDisposable CreateScope(IObservabilityContext context)
    {
        return ActivityContextHolder.EnterContext(context);
    }

    /// <summary>
    /// Span에서 컨텍스트를 추출합니다.
    /// </summary>
    public IObservabilityContext? ExtractContext(ISpan? span)
    {
        if (span == null)
            return null;

        return span.Context;
    }
}
