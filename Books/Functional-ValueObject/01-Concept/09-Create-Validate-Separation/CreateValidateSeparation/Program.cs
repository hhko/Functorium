using CreateValidateSeparation.ValueObjects;

namespace CreateValidateSeparation;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 단일 책임 원칙을 통한 Create와 Validate 분리 ===\n");

        // Create와 Validate의 책임 분리 이해
        DemonstrateCreateValidateSeparation();

        // Validate 메서드의 독립적 사용법과 장점 이해
        DemonstrateIndependentValidationUsage();
    }

    /// <summary>
    /// Create와 Validate의 책임 분리 시연
    /// </summary>
    private static void DemonstrateCreateValidateSeparation()
    {
        Console.WriteLine("=== 1. 핵심 개선사항: Create와 Validate 책임 분리 ===");

        // 검증 책임 분리: Validate 메서드만 호출
        Console.WriteLine("검증 책임 분리: Validate 메서드만 호출");
        var validationResult = Denominator.Validate(5);
        validationResult.Match(
            Succ: value => Console.WriteLine($"  검증 성공: {value}"),
            Fail: error => Console.WriteLine($"  검증 실패: {error}")
        );

        // 생성 책임 분리: Create 메서드 호출
        Console.WriteLine("생성 책임 분리: Create 메서드 호출");
        var creationResult = Denominator.Create(5);
        creationResult.Match(
            Succ: denominator => Console.WriteLine($"  생성 성공: {denominator}"),
            Fail: error => Console.WriteLine($"  생성 실패: {error}")
        );
    }

    /// <summary>
    /// Validate 메서드의 독립적 사용법과 장점 시연
    /// </summary>
    private static void DemonstrateIndependentValidationUsage()
    {
        Console.WriteLine("\n=== 2. Validate 메서드 독립적 사용 예제 ===");
        Console.WriteLine("검증 책임만 분리하여 사용:");

        var testValues = new[] { 1, 5, 10, 0, -3 };

        foreach (var value in testValues)
        {
            var validation = Denominator.Validate(value);
            validation.Match(
                Succ: validValue => Console.WriteLine($"  {value} -> 검증 통과: {validValue}"),
                Fail: error => Console.WriteLine($"  {value} -> 검증 실패: {error.Message}")
            );
        }
    }
}
