namespace Functorium.Testing.ArchitectureRules;

/// <summary>
/// 단일 클래스에 대한 검증 결과를 나타냅니다.
///
/// 이 클래스는 ClassValidator가 수행한 검증 작업의 결과를 캡슐화합니다.
/// 검증 성공/실패 여부와 실패한 규칙들의 목록을 제공하여
/// 아키텍처 규칙 위반 사항을 명확하게 파악할 수 있도록 합니다.
///
/// 주요 기능:
/// - 검증 성공/실패 상태 관리
/// - 실패한 검증 규칙들의 목록 제공
/// - 불변성을 보장하는 읽기 전용 인터페이스
///
/// 사용 예시:
/// <code>
/// var result = validator.Validate();
/// if (!result.IsValid)
/// {
///     foreach (var failure in result.Failures)
///     {
///         Console.WriteLine($"Validation failed: {failure}");
///     }
/// }
/// </code>
/// </summary>
internal sealed class ValidationResult
{
    private readonly IReadOnlyList<string> _failures;

    public IReadOnlyList<string> Failures => _failures;

    public bool IsValid => _failures.Count == 0;

    public ValidationResult(IReadOnlyList<string> failures)
    {
        _failures = failures ?? [];
    }

    //public override string ToString()
    //{
    //    return IsValid ? "Validation passed" : $"Validation failed: {string.Join(", ", Failures)}";
    //}
}
