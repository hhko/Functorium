using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules.Rules;

/// <summary>
/// 클래스의 불변성(Immutable)을 종합적으로 검증하는 규칙입니다.
///
/// 6가지 차원에서 검증을 수행합니다:
/// 1. 기본 Writability 검증
/// 2. 생성자 검증 (모두 private)
/// 3. 프로퍼티 검증 (public setter 금지)
/// 4. 필드 검증 (public 필드 금지)
/// 5. 가변 컬렉션 타입 검증
/// 6. 상태 변경 메서드 검증
/// </summary>
public sealed class ImmutabilityRule : IArchRule<Class>
{
    public string Description => "Requires class to be immutable";

    private static readonly string[] s_mutableCollectionTypes =
    [
        "List<", "Dictionary<", "HashSet<", "Queue<", "Stack<",
        "LinkedList<", "SortedList<", "SortedDictionary<", "SortedSet<",
        "ConcurrentQueue<", "ConcurrentStack<", "ConcurrentBag<",
        "ConcurrentDictionary<", "BlockingCollection<"
    ];

    private static readonly HashSet<string> s_equalityMethods =
    [
        "Equals", "GetHashCode", "GetEqualityComponents", "GetComparableEqualityComponents"
    ];

    private static readonly string[] s_getterPrefixes = ["get_", "Get", "Format", "To"];

    public IReadOnlyList<RuleViolation> Validate(Class target, Architecture architecture)
    {
        var violations = new List<RuleViolation>();
        var nonStaticMembers = target.Members
            .Where(m => m.IsStatic == false)
            .ToList();

        violations.AddRange(ValidateWritability(target, nonStaticMembers));
        violations.AddRange(ValidateConstructors(target));
        violations.AddRange(ValidateProperties(target));
        violations.AddRange(ValidateFields(target));
        violations.AddRange(ValidateCollections(target, nonStaticMembers));
        violations.AddRange(ValidateMethods(target));

        return violations;
    }

    private static List<RuleViolation> ValidateWritability(Class target, List<IMember> nonStaticMembers)
    {
        var mutableMembers = nonStaticMembers
            .Where(m => !m.Writability.IsImmutable())
            .ToList();

        return mutableMembers.Count > 0
            ? [new RuleViolation(target.FullName, "RequireImmutable.Writability",
                $"Class '{target.Name}' has mutable members: {string.Join(", ", mutableMembers.Select(m => m.Name))}")]
            : [];
    }

    private static List<RuleViolation> ValidateConstructors(Class target)
    {
        var publicConstructors = target.Constructors
            .Where(c => c.Visibility == Visibility.Public)
            .ToList();

        return publicConstructors.Count > 0
            ? [new RuleViolation(target.FullName, "RequireImmutable.Constructors",
                $"Class '{target.Name}' has public constructors: {string.Join(", ", publicConstructors.Select(c => c.Name))}")]
            : [];
    }

    private static List<RuleViolation> ValidateProperties(Class target)
    {
        var propertiesWithSetters = target.Members
            .OfType<PropertyMember>()
            .Where(p => p.Writability == Writability.Writable)
            .ToList();

        return propertiesWithSetters.Count > 0
            ? [new RuleViolation(target.FullName, "RequireImmutable.PropertySetters",
                $"Class '{target.Name}' has properties with setters: {string.Join(", ", propertiesWithSetters.Select(p => p.Name))}")]
            : [];
    }

    private static List<RuleViolation> ValidateFields(Class target)
    {
        var publicFields = target.Members
            .OfType<FieldMember>()
            .Where(f => f.Visibility == Visibility.Public && f.IsStatic != true)
            .ToList();

        return publicFields.Count > 0
            ? [new RuleViolation(target.FullName, "RequireImmutable.PublicFields",
                $"Class '{target.Name}' has public fields: {string.Join(", ", publicFields.Select(f => f.Name))}")]
            : [];
    }

    private static List<RuleViolation> ValidateCollections(Class target, List<IMember> nonStaticMembers)
    {
        var mutableCollectionMembers = nonStaticMembers
            .OfType<FieldMember>()
            .Where(field => IsMutableCollectionType(field.Type))
            .ToList();

        return mutableCollectionMembers.Count > 0
            ? [new RuleViolation(target.FullName, "RequireImmutable.MutableCollections",
                $"Class '{target.Name}' has mutable collection types: {string.Join(", ", mutableCollectionMembers.Select(m => m.Name))}")]
            : [];
    }

    private static List<RuleViolation> ValidateMethods(Class target)
    {
        var stateChangingMethods = target.Members
            .OfType<MethodMember>()
            .Where(m => m.Visibility == Visibility.Public &&
                       m.IsStatic != true &&
                       !IsAllowedMethod(m))
            .ToList();

        return stateChangingMethods.Count > 0
            ? [new RuleViolation(target.FullName, "RequireImmutable.StateChangingMethods",
                $"Class '{target.Name}' has potentially state-changing methods: {string.Join(", ", stateChangingMethods.Select(m => m.Name))}")]
            : [];
    }

    private static bool IsMutableCollectionType(IType type)
    {
        var typeName = type.FullName ?? "";
        return s_mutableCollectionTypes.Any(collectionType => typeName.Contains(collectionType));
    }

    private static bool IsAllowedMethod(MethodMember method)
    {
        return IsEqualityMethod(method) ||
               method.Name == "ToString" ||
               IsFactoryMethod(method) ||
               IsGetterMethod(method);
    }

    private static bool IsEqualityMethod(MethodMember method)
    {
        return s_equalityMethods.Contains(method.Name);
    }

    private static bool IsFactoryMethod(MethodMember method)
    {
        var methodName = method.Name;
        return methodName == "Create" ||
               methodName == "CreateFromValidated" ||
               methodName == "Validate" ||
               methodName.StartsWith("op_");
    }

    private static bool IsGetterMethod(MethodMember method)
    {
        var methodName = method.Name;
        return s_getterPrefixes.Any(prefix => methodName.StartsWith(prefix));
    }
}
