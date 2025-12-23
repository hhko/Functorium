namespace Functorium.Applications.Observabilities;

/// <summary>
/// Usecase 관련 Observability 필드 정의
/// </summary>
public static class UsecaseFields
{
    private const string MetricPrefixBase = "application.usecase";

    public static class Metrics
    {
        /// <summary>
        /// CQRS 타입별 메트릭 접두사를 반환합니다.
        /// </summary>
        /// <param name="cqrsType">CQRS 타입 (query 또는 command)</param>
        /// <returns>메트릭 접두사 (예: application.usecase.query)</returns>
        public static string GetCqrs(string cqrsType)
            => $"{MetricPrefixBase}.{cqrsType.ToLowerInvariant()}";

        /// <summary>
        /// 요청 수 메트릭 이름을 반환합니다.
        /// </summary>
        public static string GetRequest(string cqrsType)
            => $"{MetricPrefixBase}.{cqrsType}.requests";

        /// <summary>
        /// 성공 응답 수 메트릭 이름을 반환합니다.
        /// </summary>
        public static string GetResponseSuccess(string cqrsType)
            => $"{MetricPrefixBase}.{cqrsType}.responses.success";

        /// <summary>
        /// 실패 응답 수 메트릭 이름을 반환합니다.
        /// </summary>
        public static string GetResponseFailure(string cqrsType)
            => $"{MetricPrefixBase}.{cqrsType}.responses.failure";

        /// <summary>
        /// 처리 시간 메트릭 이름을 반환합니다.
        /// </summary>
        public static string GetDuration(string cqrsType)
            => $"{MetricPrefixBase}.{cqrsType}.duration";
    }
}
