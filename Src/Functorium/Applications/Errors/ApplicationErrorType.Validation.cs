namespace Functorium.Applications.Errors;

public abstract partial record ApplicationErrorType
{
    /// <summary>
    /// 검증 실패
    /// </summary>
    /// <param name="PropertyName">검증 실패한 속성 이름 (선택적)</param>
    public sealed record ValidationFailed(string? PropertyName = null) : ApplicationErrorType;
}
