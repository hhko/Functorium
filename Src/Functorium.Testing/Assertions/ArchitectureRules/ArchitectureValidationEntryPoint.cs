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
        Action<ClassValidator> validationRule)
    {
        return ValidateAllClasses(classes, architecture, validationRule, verbose: false);
    }

    public static ValidationResultSummary ValidateAllClasses(
        this IObjectProvider<Class> classes,
        Architecture architecture,
        Action<ClassValidator> validationRule,
        bool verbose)
    {
        var processor = new ValidationResultSummary();
        var targetClasses = classes.GetObjects(architecture).ToList();

        if (verbose)
        {
            LogValidationTargets(targetClasses);
        }

        foreach (var targetClass in targetClasses)
        {
            var validator = new ClassValidator(architecture, targetClass);
            validationRule(validator);
            processor.ProcessValidationResult(targetClass, validator.Validate());
        }

        return processor;
    }

    public static ValidationResultSummary ValidateAllInterfaces(
        this IObjectProvider<Interface> interfaces,
        Architecture architecture,
        Action<InterfaceValidator> validationRule)
    {
        return ValidateAllInterfaces(interfaces, architecture, validationRule, verbose: false);
    }

    public static ValidationResultSummary ValidateAllInterfaces(
        this IObjectProvider<Interface> interfaces,
        Architecture architecture,
        Action<InterfaceValidator> validationRule,
        bool verbose)
    {
        var processor = new ValidationResultSummary();
        var targetInterfaces = interfaces.GetObjects(architecture).ToList();

        if (verbose)
        {
            LogValidationTargets(targetInterfaces);
        }

        foreach (var targetInterface in targetInterfaces)
        {
            var validator = new InterfaceValidator(architecture, targetInterface);
            validationRule(validator);
            processor.ProcessValidationResult(targetInterface, validator.Validate());
        }

        return processor;
    }

    private static void LogValidationTargets(IList<Class> targetClasses)
    {
        Console.WriteLine($"Validating {targetClasses.Count} classes:");
        foreach (var targetClass in targetClasses)
        {
            Console.WriteLine($"  - {targetClass.FullName}");
        }
        Console.WriteLine();
    }

    private static void LogValidationTargets(IList<Interface> targetInterfaces)
    {
        Console.WriteLine($"Validating {targetInterfaces.Count} interfaces:");
        foreach (var targetInterface in targetInterfaces)
        {
            Console.WriteLine($"  - {targetInterface.FullName}");
        }
        Console.WriteLine();
    }
}
