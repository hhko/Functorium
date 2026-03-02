using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 아키텍처 검증을 위한 확장 메서드들을 제공합니다.
/// </summary>
public static class ArchitectureValidationEntryPoint
{
    public static ValidationResultSummary ValidateAllClasses(
        this IObjectProvider<Class> classes,
        Architecture architecture,
        Action<ClassValidator> validationRule,
        bool verbose = false)
    {
        return ValidateAll<Class, ClassValidator>(
            classes, architecture, validationRule,
            (arch, target) => new ClassValidator(arch, target),
            verbose);
    }

    public static ValidationResultSummary ValidateAllInterfaces(
        this IObjectProvider<Interface> interfaces,
        Architecture architecture,
        Action<InterfaceValidator> validationRule,
        bool verbose = false)
    {
        return ValidateAll<Interface, InterfaceValidator>(
            interfaces, architecture, validationRule,
            (arch, target) => new InterfaceValidator(arch, target),
            verbose);
    }

    private static ValidationResultSummary ValidateAll<TType, TValidator>(
        IObjectProvider<TType> provider,
        Architecture architecture,
        Action<TValidator> validationRule,
        Func<Architecture, TType, TValidator> validatorFactory,
        bool verbose)
        where TType : IType
        where TValidator : TypeValidator<TType, TValidator>
    {
        var processor = new ValidationResultSummary();
        var targets = provider.GetObjects(architecture).ToList();

        if (verbose)
        {
            LogValidationTargets(targets);
        }

        foreach (var target in targets)
        {
            var validator = validatorFactory(architecture, target);
            validationRule(validator);
            processor.ProcessViolations(target, validator.GetViolations());
        }

        return processor;
    }

    private static void LogValidationTargets<TType>(IList<TType> targets)
        where TType : IType
    {
        Console.WriteLine($"Validating {targets.Count} {typeof(TType).Name.ToLower()}s:");
        foreach (var target in targets)
        {
            Console.WriteLine($"  - {target.FullName}");
        }
        Console.WriteLine();
    }
}
