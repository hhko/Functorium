namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
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
}
