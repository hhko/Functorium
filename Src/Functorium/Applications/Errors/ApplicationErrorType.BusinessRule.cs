namespace Functorium.Applications.Errors;

public abstract partial record ApplicationErrorType
{
    /// <summary>
    /// 비즈니스 규칙 위반
    /// </summary>
    /// <param name="RuleName">위반된 규칙 이름 (선택적)</param>
    public sealed record BusinessRuleViolated(string? RuleName = null) : ApplicationErrorType;
}
