using ArchUnitNET.Domain;

namespace Framework.Test.ArchitectureRules;

/// <summary>
/// 여러 클래스에 대한 검증 결과를 집계하고 관리합니다.
/// 
/// 이 클래스는 ArchitectureValidationUtilities.ValidateAllClasses() 메서드에서
/// 여러 클래스에 대한 검증을 수행할 때 사용되며, 모든 검증 결과를 집계하여
/// 통합된 보고서를 생성하고 테스트 실패 시 적절한 예외를 발생시킵니다.
/// 
/// 주요 기능:
/// - 여러 클래스의 검증 결과를 하나로 집계
/// - 실패한 검증들을 클래스별로 그룹화하여 가독성 향상
/// - xUnit 테스트 프레임워크와 통합된 예외 발생
/// 
/// 사용 예시:
/// <code>
/// var summary = Classes()
///     .That()
///     .ImplementInterface(typeof(IValueObject))
///     .ValidateAllClasses(Architecture, @class => @class.RequireImmutable());
/// 
/// summary.ThrowIfAnyFailures("ValueObject Immutability Rule");
/// </code>
/// </summary>
public sealed class ValidationResultSummary
{
    private readonly List<string> _allFailures = [];

    /// <summary>
    /// 단일 클래스의 검증 결과를 처리합니다.
    /// 
    /// 이 메서드는 개별 클래스의 검증 결과를 받아서 전체 집계에 추가합니다.
    /// 검증이 성공한 클래스는 무시하고, 실패한 클래스만 집계에 포함시킵니다.
    /// 
    /// 실패한 검증들은 클래스별로 그룹화되어 다음과 같은 형식으로 저장됩니다:
    /// ```
    /// MyProject.ValueObjects.Email:
    ///   - Class 'Email' must be sealed.
    ///   - Found public constructors: .ctor
    /// 
    /// MyProject.ValueObjects.PhoneNumber:
    ///   - Method 'Create' in class 'PhoneNumber' must be static.
    /// ```
    /// </summary>
    /// <param name="targetClass">검증 대상 클래스</param>
    /// <param name="validationResult">해당 클래스의 검증 결과</param>
    internal void ProcessValidationResult(Class targetClass, ValidationResult validationResult)
    {
        if (!validationResult.IsValid)
        {
            var failureLines = validationResult.Failures.Select(failure => $"  - {failure}");
            _allFailures.Add($"{targetClass.FullName}:\n{string.Join("\n", failureLines)}");
        }
    }

    /// <summary>
    /// 검증 실패가 있으면 xUnit 예외를 발생시킵니다.
    /// 
    /// 이 메서드는 집계된 모든 검증 실패를 확인하고,
    /// 하나라도 실패가 있으면 xUnit.Sdk.XunitException을 발생시켜
    /// 테스트를 실패시킵니다.
    /// 
    /// 예외 메시지 형식:
    /// ```
    /// 'ValueObject Rule' rule violation:
    /// 
    /// MyProject.ValueObjects.Email:
    ///   - Class 'Email' must be sealed.
    ///   - Found public constructors: .ctor
    /// 
    /// MyProject.ValueObjects.PhoneNumber:
    ///   - Method 'Create' in class 'PhoneNumber' must be static.
    /// ```
    /// 
    /// 모든 검증이 성공한 경우에는 아무 작업도 수행하지 않습니다.
    /// </summary>
    /// <param name="ruleName">검증 규칙의 이름 (예외 메시지에 포함됨)</param>
    /// <exception cref="ArchitectureRuleViolationException">검증 실패가 하나라도 있으면 발생</exception>
    public void ThrowIfAnyFailures(string ruleName)
    {
        if (_allFailures.Count > 0)
        {
            var message = string.Join("\n\n", _allFailures);
            throw new ArchitectureRuleViolationException($"'{ruleName}' rule violation:\n\n{message}");
        }
    }
}
