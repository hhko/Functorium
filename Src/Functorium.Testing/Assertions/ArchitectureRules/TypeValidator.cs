using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// Class와 Interface의 공통 검증 로직을 제공하는 CRTP 기반 추상 기반 클래스입니다.
/// </summary>
/// <typeparam name="TType">검증 대상 타입 (Class 또는 Interface)</typeparam>
/// <typeparam name="TSelf">CRTP 패턴을 위한 자기 참조 타입</typeparam>
public abstract class TypeValidator<TType, TSelf>
    where TType : IType
    where TSelf : TypeValidator<TType, TSelf>
{
    protected readonly Architecture _architecture;
    protected readonly TType _target;
    internal readonly List<RuleViolation> _violations;

    protected TypeValidator(Architecture architecture, TType target)
    {
        _architecture = architecture;
        _target = target;
        _violations = [];
    }

    protected TypeValidator(Architecture architecture, TType target, TypeValidator<TType, TSelf> parent)
    {
        _architecture = architecture;
        _target = target;
        _violations = parent._violations;
    }

    /// <summary>
    /// 타입 종류를 반환합니다 (에러 메시지에 사용).
    /// </summary>
    protected abstract string TypeKind { get; }

    /// <summary>
    /// 프로퍼티 검색에 사용할 멤버 컬렉션을 반환합니다.
    /// ClassValidator는 MembersIncludingInherited로 override합니다.
    /// </summary>
    protected virtual IEnumerable<IMember> GetSearchableMembers() => _target.Members;

    // --- 네이밍 ---

    public TSelf RequireNameStartsWith(string prefix)
    {
        if (!_target.Name.StartsWith(prefix))
        {
            AddViolation("RequireNameStartsWith",
                $"{TypeKind} '{_target.Name}' must have name starting with '{prefix}'.");
        }
        return (TSelf)this;
    }

    public TSelf RequireNameEndsWith(string suffix)
    {
        if (!_target.Name.EndsWith(suffix))
        {
            AddViolation("RequireNameEndsWith",
                $"{TypeKind} '{_target.Name}' must have name ending with '{suffix}'.");
        }
        return (TSelf)this;
    }

    public TSelf RequireNameMatching(string regexPattern)
    {
        if (!Regex.IsMatch(_target.Name, regexPattern))
        {
            AddViolation("RequireNameMatching",
                $"{TypeKind} '{_target.Name}' must match pattern '{regexPattern}'.");
        }
        return (TSelf)this;
    }

    // --- 상속/인터페이스 ---

    public TSelf RequireImplements(Type interfaceType)
    {
        if (!_target.ImplementedInterfaces.Any(i =>
            i.FullName != null && i.FullName.StartsWith(interfaceType.FullName!)))
        {
            AddViolation("RequireImplements",
                $"{TypeKind} '{_target.Name}' must implement '{interfaceType.Name}'.");
        }
        return (TSelf)this;
    }

    public TSelf RequireImplementsGenericInterface(string genericInterfaceName)
    {
        if (!_target.ImplementedInterfaces.Any(i =>
            i.FullName != null && i.FullName.Contains(genericInterfaceName)))
        {
            AddViolation("RequireImplementsGenericInterface",
                $"{TypeKind} '{_target.Name}' must implement '{genericInterfaceName}' interface.");
        }
        return (TSelf)this;
    }

    // --- 의존성 ---

    public TSelf RequireNoDependencyOn(string typeNameContains)
    {
        var forbidden = _target.Dependencies
            .Where(d => d.Target.FullName?.Contains(typeNameContains) == true)
            .ToList();

        if (forbidden.Any())
        {
            var details = string.Join(", ", forbidden.Select(d => d.Target.FullName).Distinct());
            AddViolation("RequireNoDependencyOn",
                $"{TypeKind} '{_target.Name}' must not depend on '{typeNameContains}', but found dependencies: {details}");
        }
        return (TSelf)this;
    }

    // --- 메서드 ---

    public TSelf RequireMethod(string methodName, Action<MethodValidator> methodValidation)
    {
        var methods = _target.Members
            .OfType<MethodMember>()
            .Where(m => m.Name.StartsWith(methodName + "("))
            .ToList();

        if (!methods.Any())
        {
            AddViolation("RequireMethod",
                $"{TypeKind} '{_target.Name}' must have a method named '{methodName}'.");
            return (TSelf)this;
        }

        foreach (var method in methods)
        {
            var methodValidator = new MethodValidator(method, _violations);
            methodValidation(methodValidator);
        }

        return (TSelf)this;
    }

    public TSelf RequireAllMethods(Action<MethodValidator> methodValidation)
    {
        var methods = _target.Members
            .OfType<MethodMember>()
            .ToList();

        foreach (var method in methods)
        {
            var methodValidator = new MethodValidator(method, _violations);
            methodValidation(methodValidator);
        }

        return (TSelf)this;
    }

    public TSelf RequireAllMethods(Func<MethodMember, bool> filter, Action<MethodValidator> methodValidation)
    {
        var methods = _target.Members
            .OfType<MethodMember>()
            .Where(filter)
            .ToList();

        foreach (var method in methods)
        {
            var methodValidator = new MethodValidator(method, _violations);
            methodValidation(methodValidator);
        }

        return (TSelf)this;
    }

    public TSelf RequireMethodIfExists(string methodName, Action<MethodValidator> methodValidation)
    {
        var methods = _target.Members
            .OfType<MethodMember>()
            .Where(m => m.Name.StartsWith(methodName + "("))
            .ToList();

        if (!methods.Any())
        {
            return (TSelf)this;
        }

        foreach (var method in methods)
        {
            var methodValidator = new MethodValidator(method, _violations);
            methodValidation(methodValidator);
        }

        return (TSelf)this;
    }

    // --- 프로퍼티 ---

    public virtual TSelf RequireProperty(string propertyName)
    {
        var hasProperty = GetSearchableMembers()
            .OfType<PropertyMember>()
            .Any(p => p.Name == propertyName);

        if (!hasProperty)
        {
            AddViolation("RequireProperty",
                $"{TypeKind} '{_target.Name}' must have property '{propertyName}'.");
        }
        return (TSelf)this;
    }

    // --- 규칙 합성 ---

    public TSelf Apply(IArchRule<TType> rule)
    {
        var violations = rule.Validate(_target, _architecture);
        _violations.AddRange(violations);
        return (TSelf)this;
    }

    // --- 결과 ---

    internal ValidationResult Validate()
    {
        return new ValidationResult(_violations);
    }

    public void ValidateAndThrow()
    {
        if (_violations.Any())
        {
            string message = string.Join(", ", _violations.Select(v => $"[{v.RuleName}] {v.Description}"));
            throw new InvalidOperationException($"{_target.FullName}: {message}");
        }
    }

    protected void AddViolation(string ruleName, string description)
    {
        _violations.Add(new RuleViolation(_target.FullName, ruleName, description));
    }
}
