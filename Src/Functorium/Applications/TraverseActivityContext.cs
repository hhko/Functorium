using System.Diagnostics;

namespace Functorium.Applications;

/// <summary>
/// Traverse Activity의 AsyncLocal 컨텍스트를 관리합니다.
///
/// 목적:
///   • TraverseSerial 내부에서 생성된 Activity를 AsyncLocal에 저장
///   • Adapter에서 올바른 부모 Activity를 찾기 위해 사용
///   • FinT의 AsyncLocal 컨텍스트 복원 문제를 우회
///
/// 사용 예:
///   using (TraverseActivityContext.Enter(activity))
///   {
///       // 이 스코프 내에서 모든 Adapter는 activity를 부모로 사용
///       await ProcessItemAsync();
///   }
/// </summary>
public static class TraverseActivityContext
{
    private static readonly AsyncLocal<Activity?> CurrentActivity = new();

    /// <summary>
    /// 현재 Traverse Activity를 가져옵니다.
    /// </summary>
    /// <returns>현재 설정된 Traverse Activity, 없으면 null</returns>
    public static Activity? GetCurrent() => CurrentActivity.Value;

    /// <summary>
    /// Traverse Activity를 설정하고 Disposable 컨텍스트를 반환합니다.
    /// using 문과 함께 사용하여 자동으로 이전 값을 복원합니다.
    /// </summary>
    /// <param name="activity">설정할 Traverse Activity</param>
    /// <returns>Dispose 시 이전 값으로 복원하는 컨텍스트</returns>
    public static IDisposable Enter(Activity? activity) =>
        new TraverseActivityScope(activity);

    /// <summary>
    /// Traverse Activity를 직접 설정합니다.
    /// 일반적으로 Enter()를 사용하는 것이 권장됩니다.
    /// </summary>
    /// <param name="activity">설정할 Traverse Activity</param>
    internal static void SetCurrent(Activity? activity) =>
        CurrentActivity.Value = activity;

    private sealed class TraverseActivityScope : IDisposable
    {
        private readonly Activity? _previousActivity;

        public TraverseActivityScope(Activity? activity)
        {
            _previousActivity = CurrentActivity.Value;
            CurrentActivity.Value = activity;
        }

        public void Dispose()
        {
            CurrentActivity.Value = _previousActivity;
        }
    }
}
