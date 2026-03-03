using Functorium.Applications.Usecases;
using LanguageExt;
using LanguageExt.Common;

namespace FinToFinResponseBridge;

/// <summary>
/// Fin -> FinResponse 변환 예제
/// </summary>
public static class BridgeExamples
{
    /// <summary>
    /// 1. 직접 변환: Fin<A> -> FinResponse<A>
    /// </summary>
    public static FinResponse<string> DirectConversion()
    {
        Fin<string> fin = Fin.Succ("Hello");
        return fin.ToFinResponse();
    }

    /// <summary>
    /// 2. 매퍼 변환: Fin<A> -> FinResponse<B>
    /// </summary>
    public static FinResponse<int> MappedConversion()
    {
        Fin<string> fin = Fin.Succ("Hello");
        return fin.ToFinResponse(s => s.Length);
    }

    /// <summary>
    /// 3. 팩토리 변환: Fin<Unit> -> FinResponse<string>
    /// </summary>
    public static FinResponse<string> FactoryConversion()
    {
        Fin<Unit> fin = Fin.Succ(Unit.Default);
        return fin.ToFinResponse(() => "Deleted successfully");
    }

    /// <summary>
    /// 4. 실패 변환: Fin Fail -> FinResponse Fail
    /// </summary>
    public static FinResponse<string> FailConversion()
    {
        Fin<string> fin = Fin.Fail<string>(Error.New("not found"));
        return fin.ToFinResponse();
    }
}
