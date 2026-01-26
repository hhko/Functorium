namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
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
}
