using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 여러 규칙을 AND 합성하는 복합 규칙입니다.
/// </summary>
/// <typeparam name="TType">검증 대상 타입</typeparam>
public sealed class CompositeArchRule<TType> : IArchRule<TType> where TType : IType
{
    private readonly IReadOnlyList<IArchRule<TType>> _rules;

    public string Description => string.Join(" AND ", _rules.Select(r => r.Description));

    public CompositeArchRule(params IArchRule<TType>[] rules)
    {
        _rules = rules;
    }

    public CompositeArchRule(IEnumerable<IArchRule<TType>> rules)
    {
        _rules = rules.ToList();
    }

    public IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture)
    {
        var violations = new List<RuleViolation>();
        foreach (var rule in _rules)
        {
            violations.AddRange(rule.Validate(target, architecture));
        }
        return violations;
    }
}
