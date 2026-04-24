namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorKind
{
    /// <summary>
    /// 범위가 역전됨 (최소값이 최대값보다 큼)
    /// </summary>
    /// <param name="Min">최소값 (문자열로 표현)</param>
    /// <param name="Max">최대값 (문자열로 표현)</param>
    public sealed record RangeInverted(string? Min = null, string? Max = null) : DomainErrorKind;

    /// <summary>
    /// 범위가 비어있음 (최소값과 최대값이 같음, 엄격한 범위에서 유효한 값이 없음)
    /// </summary>
    /// <param name="Value">최소값/최대값 (문자열로 표현)</param>
    public sealed record RangeEmpty(string? Value = null) : DomainErrorKind;
}
