namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 단일 타입에 대한 검증 결과를 나타냅니다.
/// </summary>
internal sealed class ValidationResult
{
    private readonly IReadOnlyList<RuleViolation> _violations;

    public IReadOnlyList<RuleViolation> Violations => _violations;

    public bool IsValid => _violations.Count == 0;

    public ValidationResult(IReadOnlyList<RuleViolation> violations)
    {
        _violations = violations ?? [];
    }
}
