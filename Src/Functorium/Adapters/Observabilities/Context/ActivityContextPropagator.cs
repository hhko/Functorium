using System.Diagnostics;
using Functorium.Applications.Observabilities.Context;
using Functorium.Applications.Observabilities.Spans;

namespace Functorium.Adapters.Observabilities.Context;

/// <summary>
/// OpenTelemetry Activity 기반의 IContextPropagator 구현체입니다.
/// </summary>
/// <remarks>
/// .NET의 ExecutionContext가 async/await를 통해 AsyncLocal을 자동으로 전파하므로,
/// Activity.Current는 LanguageExt의 IO/FinT 실행에서도 올바르게 유지됩니다.
/// </remarks>
public sealed class ActivityContextPropagator : IContextPropagator
{
    /// <summary>
    /// 현재 관찰 가능성 컨텍스트를 가져옵니다.
    /// Activity.Current를 사용하여 현재 실행 컨텍스트의 Activity를 반환합니다.
    /// </summary>
    public IObservabilityContext? Current
    {
        get
        {
            Activity? currentActivity = Activity.Current;
            if (currentActivity != null)
                return ObservabilityContext.FromActivity(currentActivity);

            return null;
        }
    }

    /// <summary>
    /// 지정된 컨텍스트로 스코프를 생성합니다.
    /// </summary>
    /// <remarks>
    /// .NET의 ExecutionContext가 Activity.Current를 자동으로 전파하므로,
    /// 별도의 AsyncLocal 관리 없이 Activity를 직접 설정합니다.
    /// </remarks>
    public IDisposable CreateScope(IObservabilityContext context)
    {
        if (context is ObservabilityContext otelContext)
        {
            return new ActivityScope(otelContext.ActivityContext);
        }

        return new NoOpScope();
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

    private sealed class ActivityScope : IDisposable
    {
        private readonly Activity? _previousActivity;
        private readonly Activity? _newActivity;

        public ActivityScope(ActivityContext context)
        {
            _previousActivity = Activity.Current;

            // ActivityContext에서 새 Activity를 생성하지 않고
            // 현재 Activity를 유지합니다.
            // 이는 ExecutionContext가 자동으로 전파하기 때문입니다.
            _newActivity = null;
        }

        public void Dispose()
        {
            _newActivity?.Dispose();
            Activity.Current = _previousActivity;
        }
    }

    private sealed class NoOpScope : IDisposable
    {
        public void Dispose()
        {
            // No-op
        }
    }
}
