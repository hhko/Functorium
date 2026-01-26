namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
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
}
