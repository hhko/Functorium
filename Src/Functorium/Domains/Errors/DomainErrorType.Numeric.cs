namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
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
