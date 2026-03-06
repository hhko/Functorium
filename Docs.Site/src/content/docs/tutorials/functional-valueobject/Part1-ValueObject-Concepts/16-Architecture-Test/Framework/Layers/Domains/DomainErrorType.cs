using Framework.Abstractions.Errors;

namespace Framework.Layers.Domains;

/// <summary>
/// 도메인 에러 타입의 기본 record 클래스
/// sealed record 계층으로 타입 안전한 에러 정의 제공
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Framework.Layers.Domains.DomainErrorType;
///
/// DomainError.For&lt;Email&gt;(new Empty(), value, "Email cannot be empty");
/// DomainError.For&lt;Password&gt;(new TooShort(MinLength: 8), value, "Password too short");
/// // 커스텀 에러: sealed record 파생 정의
/// // public sealed record Unsupported : DomainErrorType.Custom;
/// DomainError.For&lt;Currency&gt;(new Unsupported(), value, "Currency not supported");
/// </code>
/// </remarks>
public abstract record DomainErrorType : ErrorType
{
    #region Presence (존재 관련)

    /// <summary>
    /// 값이 비어있음 (null, empty string, empty collection 등)
    /// </summary>
    public sealed record Empty : DomainErrorType;

    /// <summary>
    /// 값이 null임
    /// </summary>
    public sealed record Null : DomainErrorType;

    #endregion

    #region Length (길이 관련)

    /// <summary>
    /// 값이 최소 길이보다 짧음
    /// </summary>
    /// <param name="MinLength">요구되는 최소 길이 (0이면 미지정)</param>
    public sealed record TooShort(int MinLength = 0) : DomainErrorType;

    /// <summary>
    /// 값이 최대 길이를 초과함
    /// </summary>
    /// <param name="MaxLength">허용되는 최대 길이 (int.MaxValue면 미지정)</param>
    public sealed record TooLong(int MaxLength = int.MaxValue) : DomainErrorType;

    /// <summary>
    /// 값의 길이가 기대와 불일치 (R6: 두 값 불일치)
    /// </summary>
    /// <param name="Expected">기대되는 길이 (0이면 미지정)</param>
    public sealed record WrongLength(int Expected = 0) : DomainErrorType;

    #endregion

    #region Numeric (숫자 관련)

    /// <summary>
    /// 값이 0임
    /// </summary>
    public sealed record Zero : DomainErrorType;

    /// <summary>
    /// 값이 음수임
    /// </summary>
    public sealed record Negative : DomainErrorType;

    /// <summary>
    /// 값이 양수가 아님 (0 또는 음수)
    /// </summary>
    public sealed record NotPositive : DomainErrorType;

    /// <summary>
    /// 값이 허용 범위를 벗어남
    /// </summary>
    /// <param name="Min">최소값 (문자열로 표현, null이면 미지정)</param>
    /// <param name="Max">최대값 (문자열로 표현, null이면 미지정)</param>
    public sealed record OutOfRange(string? Min = null, string? Max = null) : DomainErrorType;

    /// <summary>
    /// 값이 최소값보다 작음
    /// </summary>
    /// <param name="Minimum">최소값 (문자열로 표현, null이면 미지정)</param>
    public sealed record BelowMinimum(string? Minimum = null) : DomainErrorType;

    /// <summary>
    /// 값이 최대값을 초과함 (R2: 기준 대비 비교, BelowMinimum과 대칭)
    /// </summary>
    /// <param name="Maximum">최대값 (문자열로 표현, null이면 미지정)</param>
    public sealed record AboveMaximum(string? Maximum = null) : DomainErrorType;

    #endregion

    #region Format (형식 관련)

    /// <summary>
    /// 값의 형식이 유효하지 않음
    /// </summary>
    /// <param name="Pattern">기대되는 형식 패턴 (선택적)</param>
    public sealed record InvalidFormat(string? Pattern = null) : DomainErrorType;

    /// <summary>
    /// 값이 대문자가 아님 (대문자여야 함)
    /// </summary>
    public sealed record NotUpperCase : DomainErrorType;

    /// <summary>
    /// 값이 소문자가 아님 (소문자여야 함)
    /// </summary>
    public sealed record NotLowerCase : DomainErrorType;

    #endregion

    #region Existence (존재 여부 관련)

    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : DomainErrorType;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : DomainErrorType;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : DomainErrorType;

    /// <summary>
    /// 값이 일치하지 않음 (예: 비밀번호 확인)
    /// </summary>
    public sealed record Mismatch : DomainErrorType;

    #endregion

    #region Range (범위 관련)

    /// <summary>
    /// 범위가 역전됨 (최소값이 최대값보다 큼)
    /// </summary>
    /// <param name="Min">최소값 (문자열로 표현)</param>
    /// <param name="Max">최대값 (문자열로 표현)</param>
    public sealed record RangeInverted(string? Min = null, string? Max = null) : DomainErrorType;

    /// <summary>
    /// 범위가 비어있음 (최소값과 최대값이 같음, 엄격한 범위에서 유효한 값이 없음)
    /// </summary>
    /// <param name="Value">최소값/최대값 (문자열로 표현)</param>
    public sealed record RangeEmpty(string? Value = null) : DomainErrorType;

    #endregion

    #region DateTime (날짜/시간 관련)

    /// <summary>
    /// 날짜가 기본값(DateTime.MinValue)임
    /// </summary>
    public sealed record DefaultDate : DomainErrorType;

    /// <summary>
    /// 날짜가 과거여야 하는데 미래임
    /// </summary>
    public sealed record NotInPast : DomainErrorType;

    /// <summary>
    /// 날짜가 미래여야 하는데 과거임
    /// </summary>
    public sealed record NotInFuture : DomainErrorType;

    /// <summary>
    /// 날짜가 특정 기준 날짜보다 이후임 (이전이어야 함)
    /// </summary>
    /// <param name="Boundary">기준 날짜</param>
    public sealed record TooLate(string? Boundary = null) : DomainErrorType;

    /// <summary>
    /// 날짜가 특정 기준 날짜보다 이전임 (이후여야 함)
    /// </summary>
    /// <param name="Boundary">기준 날짜</param>
    public sealed record TooEarly(string? Boundary = null) : DomainErrorType;

    #endregion

    #region Custom (커스텀)

    /// <summary>
    /// 도메인 특화 커스텀 에러의 기본 클래스 (표준 에러에 해당하지 않는 경우)
    /// </summary>
    /// <remarks>
    /// 표준 에러로 표현할 수 없는 도메인 특화 에러에 사용합니다.
    /// 파생 sealed record로 정의하여 타입 안전하게 사용합니다.
    /// <code>
    /// // 엔티티 내부에 nested record로 정의
    /// public sealed record Unsupported : DomainErrorType.Custom;
    ///
    /// DomainError.For&lt;Currency&gt;(new Unsupported(), value, "Currency not supported");
    /// </code>
    /// </remarks>
    public abstract record Custom : DomainErrorType;

    #endregion
}
