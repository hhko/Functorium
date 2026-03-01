using ArchUnitNET.Domain;
using Xunit;

namespace Framework.Test.ArchitectureRules;

/// <summary>
/// 아키텍처 검증을 위한 유틸리티 메서드들을 제공합니다.
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
public static class ArchitectureValidationUtilities
{
    /// <summary>
    /// 모든 클래스에 대해 검증 규칙을 적용합니다.
    /// 
    /// 이 메서드는 주어진 클래스 집합에 대해 검증 규칙을 일괄 적용하고
    /// 검증 결과를 집계하여 반환합니다. 테스트 출력은 수행하지 않습니다.
    /// 
    /// 사용 예시:
    /// <code>
    /// var result = Classes()
    ///     .That()
    ///     .ImplementInterface(typeof(IValueObject))
    ///     .ValidateAllClasses(Architecture, @class => @class.RequireImmutable());
    /// result.ThrowIfAnyFailures("ValueObject Rule");
    /// </code>
    /// </summary>
    /// <param name="classes">검증할 클래스들을 제공하는 IObjectProvider</param>
    /// <param name="architecture">ArchUnitNET 아키텍처 인스턴스</param>
    /// <param name="validationRule">각 클래스에 적용할 검증 규칙 (ClassValidator를 받는 Action)</param>
    /// <returns>검증 결과를 집계한 ValidationResultSummary</returns>
    public static ValidationResultSummary ValidateAllClasses(
        this IObjectProvider<Class> classes,
        Architecture architecture,
        Action<ClassValidator> validationRule)
    {
        return ValidateAllClasses(classes, architecture, validationRule, null);
    }

    /// <summary>
    /// 모든 클래스에 대해 검증 규칙을 적용하고 결과를 출력합니다.
    /// 
    /// 이 메서드는 주어진 클래스 집합에 대해 검증 규칙을 일괄 적용하고,
    /// 검증 과정을 테스트 출력에 로깅하며, 검증 결과를 집계하여 반환합니다.
    /// 
    /// 검증 과정:
    /// 1. 클래스 목록을 아키텍처에서 추출
    /// 2. 검증 대상 클래스들을 테스트 출력에 로깅
    /// 3. 각 클래스에 대해 ClassValidator를 생성하고 검증 규칙 적용
    /// 4. 검증 결과를 ValidationResultSummary에 집계
    /// 5. 집계된 결과 반환
    /// 
    /// 사용 예시:
    /// <code>
    /// var result = Classes()
    ///     .That()
    ///     .ImplementInterface(typeof(IValueObject))
    ///     .ValidateAllClasses(Architecture, @class => @class.RequireImmutable(), _output);
    /// result.ThrowIfAnyFailures("ValueObject Rule");
    /// </code>
    /// </summary>
    /// <param name="classes">검증할 클래스들을 제공하는 IObjectProvider</param>
    /// <param name="architecture">ArchUnitNET 아키텍처 인스턴스</param>
    /// <param name="validationRule">각 클래스에 적용할 검증 규칙 (ClassValidator를 받는 Action)</param>
    /// <param name="output">테스트 출력 헬퍼 (선택사항, null이면 Console에 출력)</param>
    /// <returns>검증 결과를 집계한 ValidationResultSummary</returns>
    public static ValidationResultSummary ValidateAllClasses(
        this IObjectProvider<Class> classes,
        Architecture architecture,
        Action<ClassValidator> validationRule,
        ITestOutputHelper? output)
    {
        var processor = new ValidationResultSummary();
        var targetClasses = classes.GetObjects(architecture).ToList();

        LogValidationTargets(targetClasses, output);

        foreach (var targetClass in targetClasses)
        {
            var validator = new ClassValidator(architecture, targetClass);
            validationRule(validator);
            processor.ProcessValidationResult(targetClass, validator.Validate());
        }

        return processor;
    }

    /// <summary>
    /// 검증 대상 클래스들을 로깅합니다.
    /// 
    /// 검증을 시작하기 전에 어떤 클래스들이 검증 대상인지
    /// 명확하게 표시하여 디버깅과 테스트 결과 분석을 용이하게 합니다.
    /// 
    /// 출력 형식:
    /// ```
    /// Validating 5 classes:
    ///   - MyProject.ValueObjects.Email
    ///   - MyProject.ValueObjects.PhoneNumber
    ///   - MyProject.ValueObjects.Address
    ///   - MyProject.ValueObjects.Currency
    ///   - MyProject.ValueObjects.Price
    /// ```
    /// </summary>
    /// <param name="targetClasses">검증 대상 클래스 목록</param>
    /// <param name="output">테스트 출력 헬퍼 (null이면 Console에 출력)</param>
    private static void LogValidationTargets(IList<Class> targetClasses, ITestOutputHelper? output)
    {
        var logMessage = $"Validating {targetClasses.Count} classes:\n";
        foreach (var targetClass in targetClasses)
        {
            logMessage += $"  - {targetClass.FullName}\n";
        }
        logMessage += "\n";

        if (output != null)
        {
            output.WriteLine(logMessage);
        }
        else
        {
            Console.WriteLine(logMessage);
        }
    }
}