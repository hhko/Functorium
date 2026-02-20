using Functorium.Abstractions.Errors;

namespace Functorium.Applications.Errors;

/// <summary>
/// 애플리케이션 레이어 에러 타입
/// sealed record 계층으로 타입 안전한 에러 정의 제공
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Applications.Errors.ApplicationErrorType;
///
/// ApplicationError.For&lt;CreateProductCommand&gt;(new AlreadyExists(), productId, "Product already exists");
/// ApplicationError.For&lt;UpdateOrderCommand&gt;(new ValidationFailed("Quantity"), value, "Quantity must be positive");
/// </code>
/// </remarks>
public abstract record ApplicationErrorType : ErrorType
{

    #region 공통 에러 타입

    /// <summary>
    /// 값이 비어있음 (null, empty string, empty collection 등)
    /// </summary>
    public sealed record Empty : ApplicationErrorType;

    /// <summary>
    /// 값이 null임
    /// </summary>
    public sealed record Null : ApplicationErrorType;

    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : ApplicationErrorType;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : ApplicationErrorType;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : ApplicationErrorType;

    /// <summary>
    /// 유효하지 않은 상태
    /// </summary>
    public sealed record InvalidState : ApplicationErrorType;

    /// <summary>
    /// 인증되지 않음
    /// </summary>
    public sealed record Unauthorized : ApplicationErrorType;

    /// <summary>
    /// 접근 금지
    /// </summary>
    public sealed record Forbidden : ApplicationErrorType;

    #endregion

    #region 검증 관련

    /// <summary>
    /// 검증 실패
    /// </summary>
    /// <param name="PropertyName">검증 실패한 속성 이름 (선택적)</param>
    public sealed record ValidationFailed(string? PropertyName = null) : ApplicationErrorType;

    #endregion

    #region 비즈니스 규칙

    /// <summary>
    /// 비즈니스 규칙 위반
    /// </summary>
    /// <param name="RuleName">위반된 규칙 이름 (선택적)</param>
    public sealed record BusinessRuleViolated(string? RuleName = null) : ApplicationErrorType;

    /// <summary>
    /// 동시성 충돌
    /// </summary>
    public sealed record ConcurrencyConflict : ApplicationErrorType;

    /// <summary>
    /// 리소스 잠금
    /// </summary>
    /// <param name="ResourceName">잠긴 리소스 이름 (선택적)</param>
    public sealed record ResourceLocked(string? ResourceName = null) : ApplicationErrorType;

    /// <summary>
    /// 작업 취소됨
    /// </summary>
    public sealed record OperationCancelled : ApplicationErrorType;

    /// <summary>
    /// 권한 부족
    /// </summary>
    /// <param name="Permission">필요한 권한 (선택적)</param>
    public sealed record InsufficientPermission(string? Permission = null) : ApplicationErrorType;

    #endregion

    #region 커스텀

    /// <summary>
    /// 애플리케이션 특화 커스텀 에러의 기본 클래스 (표준 에러에 해당하지 않는 경우)
    /// </summary>
    /// <remarks>
    /// 파생 sealed record로 정의하여 타입 안전하게 사용합니다.
    /// <code>
    /// public sealed record CannotProcess : ApplicationErrorType.Custom;
    /// </code>
    /// </remarks>
    public abstract record Custom : ApplicationErrorType;

    #endregion
}
