using Functorium.Abstractions.Errors;

namespace Functorium.Adapters.Errors;

/// <summary>
/// 어댑터 레이어 에러 타입
/// sealed record 계층으로 타입 안전한 에러 정의 제공
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Adapters.Errors.AdapterErrorType;
///
/// AdapterError.For&lt;UsecaseValidationPipeline&gt;(new PipelineValidation("PropertyName"), value, "Validation failed");
/// AdapterError.FromException&lt;UsecaseExceptionPipeline&gt;(new PipelineException(), exception);
/// </code>
/// </remarks>
public abstract record AdapterErrorType : ErrorType
{

    #region 공통 에러 타입

    /// <summary>
    /// 값이 비어있음 (null, empty string, empty collection 등)
    /// </summary>
    public sealed record Empty : AdapterErrorType;

    /// <summary>
    /// 값이 null임
    /// </summary>
    public sealed record Null : AdapterErrorType;

    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : AdapterErrorType;

    /// <summary>
    /// 요청한 ID 중 일부를 찾을 수 없음
    /// </summary>
    public sealed record PartialNotFound : AdapterErrorType;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : AdapterErrorType;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : AdapterErrorType;

    /// <summary>
    /// 유효하지 않은 상태
    /// </summary>
    public sealed record InvalidState : AdapterErrorType;

    /// <summary>
    /// 인증되지 않음
    /// </summary>
    public sealed record Unauthorized : AdapterErrorType;

    /// <summary>
    /// 접근 금지
    /// </summary>
    public sealed record Forbidden : AdapterErrorType;

    #endregion

    #region Pipeline 관련

    /// <summary>
    /// 파이프라인 검증 실패
    /// </summary>
    /// <param name="PropertyName">검증 실패한 속성 이름 (선택적)</param>
    public sealed record PipelineValidation(string? PropertyName = null) : AdapterErrorType;

    /// <summary>
    /// 파이프라인 예외 발생
    /// </summary>
    public sealed record PipelineException : AdapterErrorType;

    #endregion

    #region 외부 서비스 관련

    /// <summary>
    /// 외부 서비스 사용 불가
    /// </summary>
    /// <param name="ServiceName">서비스 이름 (선택적)</param>
    public sealed record ExternalServiceUnavailable(string? ServiceName = null) : AdapterErrorType;

    /// <summary>
    /// 연결 실패
    /// </summary>
    /// <param name="Target">연결 대상 (선택적)</param>
    public sealed record ConnectionFailed(string? Target = null) : AdapterErrorType;

    /// <summary>
    /// 타임아웃
    /// </summary>
    /// <param name="Duration">타임아웃 시간 (선택적)</param>
    public sealed record Timeout(TimeSpan? Duration = null) : AdapterErrorType;

    #endregion

    #region 데이터 관련

    /// <summary>
    /// 직렬화 실패
    /// </summary>
    /// <param name="Format">직렬화 형식 (선택적)</param>
    public sealed record Serialization(string? Format = null) : AdapterErrorType;

    /// <summary>
    /// 역직렬화 실패
    /// </summary>
    /// <param name="Format">역직렬화 형식 (선택적)</param>
    public sealed record Deserialization(string? Format = null) : AdapterErrorType;

    /// <summary>
    /// 데이터 손상
    /// </summary>
    public sealed record DataCorruption : AdapterErrorType;

    #endregion

    #region 커스텀

    /// <summary>
    /// 어댑터 특화 커스텀 에러의 기본 클래스 (표준 에러에 해당하지 않는 경우)
    /// </summary>
    /// <remarks>
    /// 파생 sealed record로 정의하여 타입 안전하게 사용합니다.
    /// <code>
    /// public sealed record RateLimited : AdapterErrorType.Custom;
    /// </code>
    /// </remarks>
    public abstract record Custom : AdapterErrorType;

    #endregion
}
