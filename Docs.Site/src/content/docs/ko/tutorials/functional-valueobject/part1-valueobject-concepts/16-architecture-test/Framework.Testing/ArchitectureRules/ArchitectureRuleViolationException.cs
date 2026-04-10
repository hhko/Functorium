namespace Framework.Test.ArchitectureRules;

/// <summary>
/// 아키텍처 규칙 위반 시 발생하는 예외입니다.
/// </summary>
public sealed class ArchitectureRuleViolationException : Exception
{
    public ArchitectureRuleViolationException(string message) : base(message)
    {
    }
}
