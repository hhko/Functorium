using System.Diagnostics;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// Adapter 메트릭 인터페이스 (Mock).
/// Source Generator가 생성하는 코드에서 사용됩니다.
/// </summary>
public interface IAdapterMetric
{
    /// <summary>
    /// 요청 메트릭을 기록합니다.
    /// </summary>
    void Request(
        Activity? activity,
        string requestCategory,
        string requestHandler,
        string requestHandlerMethod,
        DateTimeOffset startTime);

    /// <summary>
    /// 성공 응답 메트릭을 기록합니다.
    /// </summary>
    void ResponseSuccess(Activity? activity, string requestCategory, double elapsedMs);

    /// <summary>
    /// 실패 응답 메트릭을 기록합니다.
    /// </summary>
    void ResponseFailure(Activity? activity, string requestCategory, double elapsedMs, LanguageExt.Common.Error error);
}
