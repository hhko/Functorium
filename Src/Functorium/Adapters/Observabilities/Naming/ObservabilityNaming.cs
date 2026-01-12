namespace Functorium.Adapters.Observabilities.Naming;

/// <summary>
/// 관찰 가능성 관련 통합 네이밍 규칙을 정의합니다.
/// 메트릭 이름, 태그 키, Span 이름 등의 단일 진실 공급원(Single Source of Truth)입니다.
///
/// OpenTelemetry Semantic Conventions 준수:
/// - 표준 attributes: code.function, error.type, exception.message 등
/// - 커스텀 attributes: request.*, response.*, error.* 네임스페이스
/// </summary>
public static partial class ObservabilityNaming
{
    /// <summary>
    /// 레이어 상수 (소문자)
    /// </summary>
    public static class Layers
    {
        public const string Application = "application";
        public const string Adapter = "adapter";
    }

    /// <summary>
    /// 카테고리 상수 (소문자)
    /// </summary>
    public static class Categories
    {
        public const string Usecase = "usecase";
        public const string Repository = "repository";
    }

    /// <summary>
    /// CQRS 타입 상수 (소문자)
    /// </summary>
    public static class Cqrs
    {
        public const string Command = "command";
        public const string Query = "query";
        public const string Unknown = "unknown";
    }

    /// <summary>
    /// 상태 상수 (소문자)
    /// </summary>
    public static class Status
    {
        public const string Success = "success";
        public const string Failure = "failure";
    }

    /// <summary>
    /// 에러 타입 상수 (3-Pillar 공통)
    /// </summary>
    public static class ErrorTypes
    {
        /// <summary>예상된 비즈니스 에러 (IsExpected = true)</summary>
        public const string Expected = "expected";

        /// <summary>예외적 시스템 에러 (IsExceptional = true)</summary>
        public const string Exceptional = "exceptional";

        /// <summary>복합 에러 (ManyErrors)</summary>
        public const string Aggregate = "aggregate";
    }

    /// <summary>
    /// Request Handler 메서드 이름
    /// </summary>
    public static class Methods
    {
        public const string Handle = "Handle";
    }

    /// <summary>
    /// SLO 상태 값 (3단계 심각도)
    /// </summary>
    public static class SloStatus
    {
        /// <summary>정상: elapsed &lt;= P95</summary>
        public const string Ok = "ok";

        /// <summary>경고: P95 &lt; elapsed &lt;= P99</summary>
        public const string P95Exceeded = "p95_exceeded";

        /// <summary>심각: elapsed &gt; P99</summary>
        public const string P99Exceeded = "p99_exceeded";
    }
}
