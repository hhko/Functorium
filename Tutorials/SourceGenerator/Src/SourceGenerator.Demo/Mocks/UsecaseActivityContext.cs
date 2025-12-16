using System.Diagnostics;

// Source Generator가 생성하는 코드에서 사용하는 네임스페이스
namespace Functorium.Applications.Observabilities;

/// <summary>
/// Usecase Activity Context 관리 (Mock).
/// AsyncLocal을 사용하여 현재 실행 컨텍스트의 ActivityContext를 저장합니다.
/// </summary>
public static class UsecaseActivityContext
{
    private static readonly AsyncLocal<ActivityContext?> _current = new();

    public static ActivityContext? GetCurrent() => _current.Value;

    public static void SetCurrent(ActivityContext? context) => _current.Value = context;

    public static LanguageExt.Unit SetCurrentUnit(ActivityContext? context)
    {
        _current.Value = context;
        return LanguageExt.Prelude.unit;
    }
}
