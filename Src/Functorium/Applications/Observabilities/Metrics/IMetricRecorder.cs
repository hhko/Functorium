using LanguageExt.Common;

namespace Functorium.Applications.Observabilities.Metrics;

/// <summary>
/// 메트릭을 기록하는 인터페이스입니다.
/// 요청 수, 성공/실패 수, 응답 시간 등을 기록합니다.
/// </summary>
/// <remarks>
/// ISP(인터페이스 분리 원칙)에 따라 메트릭 기록만을 담당합니다.
/// </remarks>
public interface IMetricRecorder
{
    /// <summary>
    /// 요청을 기록합니다.
    /// </summary>
    /// <param name="category">카테고리 (예: "Usecase", "Repository")</param>
    /// <param name="handler">핸들러 이름</param>
    /// <param name="method">메서드 이름</param>
    void RecordRequest(
        string category,
        string handler,
        string method);

    /// <summary>
    /// 성공적인 응답을 기록합니다.
    /// </summary>
    /// <param name="category">카테고리</param>
    /// <param name="handler">핸들러 이름</param>
    /// <param name="method">메서드 이름</param>
    /// <param name="elapsedMs">경과 시간(밀리초)</param>
    void RecordResponseSuccess(
        string category,
        string handler,
        string method,
        double elapsedMs);

    /// <summary>
    /// 실패 응답을 기록합니다.
    /// </summary>
    /// <param name="category">카테고리</param>
    /// <param name="handler">핸들러 이름</param>
    /// <param name="method">메서드 이름</param>
    /// <param name="elapsedMs">경과 시간(밀리초)</param>
    /// <param name="error">오류 정보</param>
    void RecordResponseFailure(
        string category,
        string handler,
        string method,
        double elapsedMs,
        Error error);
}
