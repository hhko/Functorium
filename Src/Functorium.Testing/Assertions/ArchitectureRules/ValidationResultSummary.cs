using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 여러 타입에 대한 검증 결과를 집계하고 관리합니다.
/// </summary>
public sealed class ValidationResultSummary
{
    private readonly List<string> _allFailures = [];

    /// <summary>
    /// 단일 타입의 검증 결과를 처리합니다.
    /// </summary>
    internal void ProcessValidationResult<TType>(TType target, ValidationResult validationResult)
        where TType : IType
    {
        if (!validationResult.IsValid)
        {
            var failureLines = validationResult.Violations.Select(v => $"  - [{v.RuleName}] {v.Description}");
            _allFailures.Add($"{target.FullName}:\n{string.Join("\n", failureLines)}");
        }
    }

    /// <summary>
    /// 검증 실패가 있으면 xUnit 예외를 발생시킵니다.
    /// </summary>
    /// <param name="ruleName">검증 규칙의 이름 (예외 메시지에 포함됨)</param>
    /// <exception cref="Xunit.Sdk.XunitException">검증 실패가 하나라도 있으면 발생</exception>
    public void ThrowIfAnyFailures(string ruleName)
    {
        if (_allFailures.Count > 0)
        {
            var message = string.Join("\n\n", _allFailures);
            throw new Xunit.Sdk.XunitException($"'{ruleName}' rule violation:\n\n{message}");
        }
    }
}
