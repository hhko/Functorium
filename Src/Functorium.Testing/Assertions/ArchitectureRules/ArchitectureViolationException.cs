namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 아키텍처 규칙 위반 시 발생하는 예외입니다.
/// </summary>
public sealed class ArchitectureViolationException : Exception
{
    public string RuleName { get; }
    public IReadOnlyList<RuleViolation> Violations { get; }

    public ArchitectureViolationException(string ruleName, IReadOnlyList<RuleViolation> violations)
        : base(FormatMessage(ruleName, violations))
    {
        RuleName = ruleName;
        Violations = violations;
    }

    private static string FormatMessage(string ruleName, IReadOnlyList<RuleViolation> violations)
    {
        var grouped = violations.GroupBy(v => v.TargetName);
        var lines = grouped.Select(g =>
        {
            var failureLines = g.Select(v => $"  - [{v.RuleName}] {v.Description}");
            return $"{g.Key}:\n{string.Join("\n", failureLines)}";
        });
        return $"'{ruleName}' rule violation:\n\n{string.Join("\n\n", lines)}";
    }
}
