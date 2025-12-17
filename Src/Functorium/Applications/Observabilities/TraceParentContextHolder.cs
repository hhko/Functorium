using System.Diagnostics;

namespace Functorium.Applications;

/// <summary>
/// Usecase Activity의 ActivityContext를 AsyncLocal에 저장하여 관리합니다.
///
/// 목적:
///   • Adapter Activity 생성 시 부모 Usecase의 ActivityContext를 저장
///   • TraverseSerial에서 올바른 부모 Context를 찾기 위해 사용
///   • Activity.Stop() 후 Parent가 null이 되는 문제를 우회
///
/// 스레드 안전성:
///   • AsyncLocal은 async/await 흐름에서 자동으로 값을 복원합니다.
///   • 각 async 컨텍스트는 독립적인 값을 가지며 스레드 안전합니다.
///   • Task.Run()이나 ThreadPool에서 새로운 컨텍스트를 시작하면 현재 값이 복사됩니다.
///   • 동일한 async 체인 내에서는 값이 일관되게 유지됩니다.
///
/// 사용 예:
///   using (UsecaseActivityContext.Enter(parentContext))
///   {
///       // 이 스코프 내에서 TraverseSerial은 이 context를 부모로 사용
///       await repository.GetDataAsync();
///   }
/// </summary>
public static class TraceParentContextHolder
{
    private static readonly AsyncLocal<ActivityContext?> CurrentContext = new();

    /// <summary>
    /// 현재 Usecase ActivityContext를 가져옵니다.
    /// </summary>
    /// <returns>현재 설정된 Usecase ActivityContext, 없으면 null</returns>
    public static ActivityContext? GetCurrent() => CurrentContext.Value;

    /// <summary>
    /// Usecase ActivityContext를 설정하고 Disposable 컨텍스트를 반환합니다.
    /// using 문과 함께 사용하여 자동으로 이전 값을 복원합니다.
    /// </summary>
    /// <param name="context">설정할 Usecase ActivityContext</param>
    /// <returns>Dispose 시 이전 값으로 복원하는 컨텍스트</returns>
    public static IDisposable Enter(ActivityContext? context) =>
        new UsecaseActivityScope(context);

    /// <summary>
    /// Usecase ActivityContext를 직접 설정합니다.
    /// 일반적으로 Enter()를 사용하는 것이 권장됩니다.
    /// </summary>
    /// <param name="context">설정할 Usecase ActivityContext</param>
    public static void SetCurrent(ActivityContext? context) =>
        CurrentContext.Value = context;

    /// <summary>
    /// Usecase ActivityContext를 설정하고 Unit을 반환합니다.
    /// LanguageExt IO 모나드 체이닝에 사용됩니다.
    /// </summary>
    /// <param name="context">설정할 Usecase ActivityContext</param>
    /// <returns>Unit</returns>
    public static LanguageExt.Unit SetCurrentUnit(ActivityContext? context)
    {
        CurrentContext.Value = context;
        return LanguageExt.Prelude.unit;
    }

    private sealed class UsecaseActivityScope : IDisposable
    {
        private readonly ActivityContext? _previousContext;

        public UsecaseActivityScope(ActivityContext? context)
        {
            _previousContext = CurrentContext.Value;
            CurrentContext.Value = context;
        }

        public void Dispose()
        {
            CurrentContext.Value = _previousContext;
            GC.SuppressFinalize(this);
        }
    }
}
