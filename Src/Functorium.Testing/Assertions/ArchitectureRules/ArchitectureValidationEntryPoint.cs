using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 아키텍처 검증을 위한 확장 메서드들을 제공합니다.
///
/// 이 클래스는 ArchUnitNET을 기반으로 한 커스텀 아키텍처 검증 프레임워크의
/// 핵심 유틸리티 기능을 제공합니다. 주로 클래스 집합에 대한 검증 규칙을
/// 일괄 적용하고 결과를 집계하는 기능을 담당합니다.
///
/// 주요 기능:
/// - 클래스 집합에 대한 검증 규칙 일괄 적용
/// - 검증 결과 집계 및 요약
/// - 테스트 출력을 통한 검증 과정 로깅
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

    private static void LogValidationTargets(IList<Class> targetClasses)
    {
        Console.WriteLine($"Validating {targetClasses.Count} classes:");
        foreach (var targetClass in targetClasses)
        {
            Console.WriteLine($"  - {targetClass.FullName}");
        }
        Console.WriteLine();
    }
}
