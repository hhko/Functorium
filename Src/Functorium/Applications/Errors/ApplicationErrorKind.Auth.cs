namespace Functorium.Applications.Errors;

public abstract partial record ApplicationErrorKind
{
    /// <summary>
    /// 인증되지 않음
    /// </summary>
    public sealed record Unauthorized : ApplicationErrorKind;

    /// <summary>
    /// 접근 금지
    /// </summary>
    public sealed record Forbidden : ApplicationErrorKind;

    /// <summary>
    /// 권한 부족
    /// </summary>
    /// <param name="Permission">필요한 권한 (선택적)</param>
    public sealed record InsufficientPermission(string? Permission = null) : ApplicationErrorKind;
}
