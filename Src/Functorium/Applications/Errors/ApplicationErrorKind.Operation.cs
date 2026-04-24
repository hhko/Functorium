namespace Functorium.Applications.Errors;

public abstract partial record ApplicationErrorKind
{
    /// <summary>
    /// 동시성 충돌
    /// </summary>
    public sealed record ConcurrencyConflict : ApplicationErrorKind;

    /// <summary>
    /// 리소스 잠금
    /// </summary>
    /// <param name="ResourceName">잠긴 리소스 이름 (선택적)</param>
    public sealed record ResourceLocked(string? ResourceName = null) : ApplicationErrorKind;

    /// <summary>
    /// 작업 취소됨
    /// </summary>
    public sealed record OperationCancelled : ApplicationErrorKind;
}
