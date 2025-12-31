using System.Diagnostics;
using Functorium.Applications.Observabilities.Context;

namespace Functorium.Adapters.Observabilities.Context;

/// <summary>
/// AsyncLocal을 사용하여 Activity 컨텍스트를 관리합니다.
/// 기존 TraceParentActivityHolder와 TraceParentContextHolder를 통합한 구현체입니다.
/// </summary>
/// <remarks>
/// 목적:
///   - TraverseSerial 내부에서 생성된 Activity를 AsyncLocal에 저장
///   - Adapter에서 올바른 부모 Activity를 찾기 위해 사용
///   - FinT의 AsyncLocal 컨텍스트 복원 문제를 우회
/// </remarks>
public static class ActivityContextHolder
{
    private static readonly AsyncLocal<Activity?> CurrentActivity = new();
    private static readonly AsyncLocal<IObservabilityContext?> CurrentContext = new();

    /// <summary>
    /// 현재 Activity를 가져옵니다.
    /// </summary>
    public static Activity? GetCurrentActivity() => CurrentActivity.Value;

    /// <summary>
    /// 현재 관찰 가능성 컨텍스트를 가져옵니다.
    /// </summary>
    public static IObservabilityContext? GetCurrentContext() => CurrentContext.Value;

    /// <summary>
    /// Activity를 설정하고 Disposable 스코프를 반환합니다.
    /// using 문과 함께 사용하여 자동으로 이전 값을 복원합니다.
    /// </summary>
    public static IDisposable EnterActivity(Activity? activity) =>
        new ActivityScope(activity);

    /// <summary>
    /// 관찰 가능성 컨텍스트를 설정하고 Disposable 스코프를 반환합니다.
    /// </summary>
    public static IDisposable EnterContext(IObservabilityContext? context) =>
        new ContextScope(context);

    /// <summary>
    /// Activity를 직접 설정합니다.
    /// 일반적으로 EnterActivity()를 사용하는 것이 권장됩니다.
    /// </summary>
    internal static void SetCurrentActivity(Activity? activity) =>
        CurrentActivity.Value = activity;

    /// <summary>
    /// 컨텍스트를 직접 설정합니다.
    /// </summary>
    internal static void SetCurrentContext(IObservabilityContext? context) =>
        CurrentContext.Value = context;

    private sealed class ActivityScope : IDisposable
    {
        private readonly Activity? _previousActivity;

        public ActivityScope(Activity? activity)
        {
            _previousActivity = CurrentActivity.Value;
            CurrentActivity.Value = activity;
        }

        public void Dispose()
        {
            CurrentActivity.Value = _previousActivity;
        }
    }

    private sealed class ContextScope : IDisposable
    {
        private readonly IObservabilityContext? _previousContext;

        public ContextScope(IObservabilityContext? context)
        {
            _previousContext = CurrentContext.Value;
            CurrentContext.Value = context;
        }

        public void Dispose()
        {
            CurrentContext.Value = _previousContext;
        }
    }
}
