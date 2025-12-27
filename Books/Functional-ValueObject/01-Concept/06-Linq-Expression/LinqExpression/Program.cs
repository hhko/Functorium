using LinqExpression.ValueObjects;

namespace LinqExpression;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== LINQ 표현식을 통한 코드 단순화 ===\n");

        // LINQ 표현식 기본 기능 테스트
        DemonstrateLinqExpression();
        DemonstrateComplexOperations();

        // LINQ 표현식 고급 기능 테스트
        DemonstrateConversionWithLinq();
        DemonstrateErrorHandling();
    }

    /// <summary>
    /// LINQ 표현식을 통한 단순화 시연
    /// </summary>
    private static void DemonstrateLinqExpression()
    {
        Console.WriteLine("1. 핵심 개선사항: LINQ 표현식을 통한 단순화");
        Console.WriteLine("  Before (05-Operator-Overloading): Match 사용");
        Console.WriteLine("  After  (06-Linq-Expression): from 키워드 사용");

        // LINQ 표현식을 사용한 자연스러운 나눗셈 연산
        var result = from denominator in Denominator.Create(5)
                     select MathOperations.Divide(15, denominator);

        result.Match(
            Succ: value => Console.WriteLine($"  15 / 5 = {value} (LINQ 표현식)"),
            Fail: error => Console.WriteLine($"  에러: {error}")
        );
    }

    /// <summary>
    /// 복합 연산에서의 LINQ 표현식 활용 시연
    /// </summary>
    private static void DemonstrateComplexOperations()
    {
        Console.WriteLine("\n2. 복합 연산에서의 LINQ 표현식 활용:");

        // 여러 값 객체를 사용한 복합 연산
        var complexResult = from a in Denominator.Create(10)
                            from b in Denominator.Create(5)
                            from c in Denominator.Create(2)
                            select a / b / c;

        complexResult.Match(
            Succ: value => Console.WriteLine($"  (10 / 5) * 2 = {value}"),
            Fail: error => Console.WriteLine($"  에러: {error}"));
    }

    /// <summary>
    /// 변환 연산자와 LINQ 표현식 시연
    /// </summary>
    private static void DemonstrateConversionWithLinq()
    {
        Console.WriteLine("\n3. 변환 연산자와 LINQ 표현식:");

        // 성공 케이스
        var successResult = from value in Denominator.Create(15)
                            select $"변환 성공: {value}";

        successResult.Match(
            Succ: message => Console.WriteLine($"    {message}"),
            Fail: error => Console.WriteLine($"    변환 실패: {error}"));

        // 실패 케이스
        var failureResult = from value in Denominator.Create(0)
                            select $"변환 성공: {value}";

        failureResult.Match(
            Succ: message => Console.WriteLine($"    {message}"),
            Fail: error => Console.WriteLine($"    변환 실패: {error}"));
    }

    /// <summary>
    /// 에러 처리 시연
    /// </summary>
    private static void DemonstrateErrorHandling()
    {
        Console.WriteLine("\n4. 에러 처리:");
        Console.WriteLine("  LINQ 표현식을 통한 에러 처리:");

        // 0으로 나누기 시도
        var divisionResult = from ten in Denominator.Create(10)
                             from zero in Denominator.Create(0)
                             select ten / zero;

        divisionResult.Match(
            Succ: value => Console.WriteLine($"    {value}"),
            Fail: error => Console.WriteLine($"    에러: {error}"));

        // 연쇄 에러 처리
        var chainResult = from a in Denominator.Create(20)
                          from b in Denominator.Create(4)
                          from c in Denominator.Create(0)
                          select a / b / c;

        chainResult.Match(
            Succ: value => Console.WriteLine($"    연쇄 연산 결과: {value}"),
            Fail: error => Console.WriteLine($"    연쇄 연산 에러: {error}"));
    }
}
