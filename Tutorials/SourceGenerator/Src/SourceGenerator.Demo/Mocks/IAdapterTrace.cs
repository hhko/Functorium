using System.Diagnostics;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// Adapter 트레이싱 인터페이스 (Mock).
/// Source Generator가 생성하는 코드에서 사용됩니다.
/// </summary>
public interface IAdapterTrace
{
    /// <summary>
    /// 요청 시작 시 Activity를 생성합니다.
    /// </summary>
    Activity? Request(
        ActivityContext parentContext,
        string requestCategory,
        string requestHandler,
        string requestHandlerMethod,
        DateTimeOffset startTime);

    /// <summary>
    /// 성공 응답을 기록합니다.
    /// </summary>
    void ResponseSuccess(Activity? activity, double elapsedMs);

    /// <summary>
    /// 실패 응답을 기록합니다.
    /// </summary>
    void ResponseFailure(Activity? activity, double elapsedMs, LanguageExt.Common.Error error);
}
