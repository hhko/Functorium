using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 클래스에 대한 아키텍처 규칙 검증을 수행하는 클래스입니다.
/// </summary>
public sealed class ClassValidator : TypeValidator<Class, ClassValidator>
{
    private static readonly Rules.ImmutabilityRule s_immutabilityRule = new();

    public ClassValidator(Architecture architecture, Class targetClass)
        : base(architecture, targetClass)
    {
    }

    private ClassValidator(Architecture architecture, Class targetClass, ClassValidator parentValidator)
        : base(architecture, targetClass, parentValidator)
    {
    }

    protected override string TypeKind => "Class";

    protected override IEnumerable<IMember> GetSearchableMembers() => _target.MembersIncludingInherited;

    // C# static class는 IL에서 abstract + sealed로 표현됨
    private bool IsStaticClass => _target.IsAbstract == true && _target.IsSealed == true;

    // --- Visibility ---

    public ClassValidator RequirePublic()
    {
        if (_target.Visibility != Visibility.Public)
        {
            AddViolation($"Class '{_target.Name}' must be public.");
        }
        return this;
    }

    public ClassValidator RequireInternal()
    {
        if (_target.Visibility != Visibility.Internal)
        {
            AddViolation($"Class '{_target.Name}' must be internal.");
        }
        return this;
    }

    // --- Modifiers ---

    public ClassValidator RequireSealed()
    {
        if (_target.IsSealed != true)
        {
            AddViolation($"Class '{_target.Name}' must be sealed.");
        }
        return this;
    }

    public ClassValidator RequireNotSealed()
    {
        if (_target.IsSealed == true)
        {
            AddViolation($"Class '{_target.Name}' must not be sealed.");
        }
        return this;
    }

    public ClassValidator RequireStatic()
    {
        if (!IsStaticClass)
        {
            AddViolation($"Class '{_target.Name}' must be static.");
        }
        return this;
    }

    public ClassValidator RequireNotStatic()
    {
        if (IsStaticClass)
        {
            AddViolation($"Class '{_target.Name}' must not be static.");
        }
        return this;
    }

    public ClassValidator RequireAbstract()
    {
        if (_target.IsAbstract != true || IsStaticClass)
        {
            AddViolation($"Class '{_target.Name}' must be abstract.");
        }
        return this;
    }

    public ClassValidator RequireNotAbstract()
    {
        if (_target.IsAbstract == true && !IsStaticClass)
        {
            AddViolation($"Class '{_target.Name}' must not be abstract.");
        }
        return this;
    }

    // --- Type ---

    public ClassValidator RequireRecord()
    {
        if (_target.IsRecord != true)
        {
            AddViolation($"Class '{_target.Name}' must be a record.");
        }
        return this;
    }

    public ClassValidator RequireNotRecord()
    {
        if (_target.IsRecord == true)
        {
            AddViolation($"Class '{_target.Name}' must not be a record.");
        }
        return this;
    }

    public ClassValidator RequireAttribute(string attributeName)
    {
        if (!_target.Attributes.Any(a =>
            a.FullName != null && a.FullName.Contains(attributeName)))
        {
            AddViolation($"Class '{_target.Name}' must have '{attributeName}' attribute.");
        }
        return this;
    }

    // --- Inheritance ---

    public ClassValidator RequireInherits(Type baseType)
    {
        if (!_target.InheritedClasses.Any(b =>
            b.FullName != null && b.FullName.StartsWith(baseType.FullName!)))
        {
            AddViolation($"Class '{_target.Name}' must inherit from '{baseType.Name}'.");
        }
        return this;
    }

    // --- Constructors ---

    public ClassValidator RequirePrivateAnyParameterlessConstructor()
    {
        var parameterlessConstructor = _target.Constructors.FirstOrDefault(c => c.Parameters.Count() == 0);
        if (parameterlessConstructor == null || parameterlessConstructor.Visibility != Visibility.Private)
        {
            AddViolation($"Class '{_target.Name}' must have a private parameterless constructor.");
        }
        return this;
    }

    public ClassValidator RequireAllPrivateConstructors()
    {
        var nonPrivateConstructors = _target.Constructors.Where(c => c.Visibility != Visibility.Private);
        if (nonPrivateConstructors.Any())
        {
            var constructorNames = string.Join(", ", nonPrivateConstructors.Select(c => c.Name));
            AddViolation(
                $"All constructors in class '{_target.Name}' must be private. Found non-private constructors: {constructorNames}");
        }
        return this;
    }

    // --- Properties ---

    public ClassValidator RequireNoPublicSetters()
    {
        var propertiesWithPublicSetters = _target.Members
            .OfType<PropertyMember>()
            .Where(p => p.Writability == Writability.Writable)
            .ToList();

        if (propertiesWithPublicSetters.Any())
        {
            var details = string.Join(", ", propertiesWithPublicSetters.Select(p => p.Name));
            AddViolation($"Class '{_target.Name}' must not have public setters, but found: {details}");
        }
        return this;
    }

    public ClassValidator RequireOnlyPrimitiveProperties(params string[] additionalAllowedTypePrefixes)
    {
        var nonPrimitiveProperties = _target.Members
            .OfType<PropertyMember>()
            .Where(p => p.IsStatic != true && !IsCompilerGenerated(p))
            .Where(p => !IsPrimitiveOrAllowedType(p.Type, additionalAllowedTypePrefixes))
            .ToList();

        if (nonPrimitiveProperties.Any())
        {
            var details = string.Join(", ", nonPrimitiveProperties.Select(p => $"{p.Name} ({p.Type.Name})"));
            AddViolation($"Class '{_target.Name}' has non-primitive properties: {details}");
        }
        return this;
    }

    // --- Fields ---

    public ClassValidator RequireNoInstanceFields(params string[] excludeFieldTypeContaining)
    {
        var instanceFields = _target.Members
            .OfType<FieldMember>()
            .Where(f => f.IsStatic != true)
            .Where(f => !f.Name.StartsWith("<") && !f.Name.Contains("k__BackingField"))
            .Where(f => !excludeFieldTypeContaining.Any(exc =>
                (f.Type.FullName ?? f.Type.Name).Contains(exc)))
            .ToList();

        if (instanceFields.Any())
        {
            var details = string.Join(", ", instanceFields.Select(f => f.Name));
            AddViolation($"Class '{_target.Name}' must have no instance fields, but found: {details}");
        }
        return this;
    }

    // --- Nested ---

    public ClassValidator RequireNestedClass(string nestedClassName, Action<ClassValidator>? nestedClassValidation = null)
    {
        Class? nestedClass = _architecture.Classes.FirstOrDefault(c => c.FullName == _target.FullName + "+" + nestedClassName);
        if (nestedClass == null)
        {
            AddViolation($"Nested class '{nestedClassName}' must be required but not found.");
            return this;
        }

        nestedClassValidation?.Invoke(new ClassValidator(_architecture, nestedClass, this));
        return this;
    }

    public ClassValidator RequireNestedClassIfExists(string nestedClassName, Action<ClassValidator>? nestedClassValidation = null)
    {
        Class? nestedClass = _architecture.Classes.FirstOrDefault(c => c.FullName == _target.FullName + "+" + nestedClassName);
        if (nestedClass == null)
        {
            return this;
        }

        nestedClassValidation?.Invoke(new ClassValidator(_architecture, nestedClass, this));
        return this;
    }

    // --- Immutability ---

    public ClassValidator RequireImmutable()
    {
        return Apply(s_immutabilityRule);
    }

    // --- Private helpers ---

    private static bool IsCompilerGenerated(PropertyMember property)
    {
        return property.Name.StartsWith("<") || property.Name == "EqualityContract";
    }

    private static readonly HashSet<string> s_primitiveTypes =
    [
        "System.String",
        "System.Int32",
        "System.Int64",
        "System.Decimal",
        "System.Double",
        "System.Single",
        "System.Boolean",
        "System.DateTime",
        "System.DateTimeOffset",
        "System.Guid",
        "System.Byte",
        "System.Int16",
        "System.UInt32",
        "System.UInt64",
    ];

    private static bool IsPrimitiveOrAllowedType(IType type, string[] additionalAllowedTypePrefixes)
    {
        if (type is ArchUnitNET.Domain.Enum)
            return true;

        var fullName = type.FullName ?? "";

        foreach (var prefix in additionalAllowedTypePrefixes)
        {
            if (fullName.StartsWith(prefix))
                return true;
        }

        if (fullName.StartsWith("System.Nullable"))
            return true;

        if (fullName.StartsWith("System.Collections.Generic.List"))
            return true;

        return s_primitiveTypes.Contains(fullName);
    }
}
