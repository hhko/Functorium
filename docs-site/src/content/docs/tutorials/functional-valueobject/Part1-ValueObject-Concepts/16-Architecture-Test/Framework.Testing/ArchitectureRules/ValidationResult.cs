namespace Framework.Test.ArchitectureRules;

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

    /// <summary>
    /// 검증 실패 목록을 가져옵니다.
    /// 
    /// 검증 과정에서 위반된 아키텍처 규칙들의 상세한 오류 메시지 목록입니다.
    /// 각 실패 메시지는 어떤 규칙이 위반되었는지와 구체적인 위반 내용을 포함합니다.
    /// 
    /// 예시 실패 메시지:
    /// - "Class 'Email' must be sealed."
    /// - "Found public constructors: .ctor"
    /// - "Method 'Create' in class 'Email' must be static."
    /// </summary>
    public IReadOnlyList<string> Failures => _failures;

    /// <summary>
    /// 검증이 성공했는지 여부를 확인합니다.
    /// 
    /// 모든 아키텍처 규칙이 통과되었으면 true,
    /// 하나라도 실패한 규칙이 있으면 false를 반환합니다.
    /// </summary>
    public bool IsValid => _failures.Count == 0;

    /// <summary>
    /// ValidationResult 인스턴스를 생성합니다.
    /// 
    /// 검증 실패 목록을 받아서 불변 결과 객체를 생성합니다.
    /// null이 전달되면 빈 목록으로 처리됩니다.
    /// </summary>
    /// <param name="failures">검증 실패 목록 (null이면 빈 목록으로 처리)</param>
    public ValidationResult(IReadOnlyList<string> failures)
    {
        _failures = failures ?? [];
    }

    //public override string ToString()
    //{
    //    return IsValid ? "Validation passed" : $"Validation failed: {string.Join(", ", Failures)}";
    //}
}
