using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 람다 기반 커스텀 규칙입니다.
/// </summary>
/// <typeparam name="TType">검증 대상 타입</typeparam>
public sealed class DelegateArchRule<TType> : IArchRule<TType> where TType : IType
{
    private readonly Func<TType, Architecture, IReadOnlyList<RuleViolation>> _validate;

    public string Description { get; }

    public DelegateArchRule(string description, Func<TType, Architecture, IReadOnlyList<RuleViolation>> validate)
    {
        Description = description;
        _validate = validate;
    }

    public IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture)
    {
        return _validate(target, architecture);
    }
}
