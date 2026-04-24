namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorKind
{
    /// <summary>
    /// 무효한 상태 전이 (예: Paid → Active)
    /// </summary>
    public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorKind;
}
