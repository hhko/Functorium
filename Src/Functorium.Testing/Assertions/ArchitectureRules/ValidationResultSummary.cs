using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 여러 타입에 대한 검증 결과를 집계하고 관리합니다.
/// </summary>
public sealed class ValidationResultSummary
{
    private readonly List<RuleViolation> _allViolations = [];

    /// <summary>
    /// 단일 타입의 검증 위반 목록을 처리합니다.
    /// </summary>
    internal void ProcessViolations<TType>(TType target, IReadOnlyList<RuleViolation> violations)
        where TType : IType
    {
        _allViolations.AddRange(violations);
    }

    /// <summary>
    /// 검증 실패가 있으면 예외를 발생시킵니다.
    /// </summary>
    /// <param name="ruleName">검증 규칙의 이름 (예외 메시지에 포함됨)</param>
    /// <exception cref="ArchitectureViolationException">검증 실패가 하나라도 있으면 발생</exception>
    public void ThrowIfAnyFailures(string ruleName)
    {
        if (_allViolations.Count > 0)
        {
            throw new ArchitectureViolationException(ruleName, _allViolations);
        }
    }
}
