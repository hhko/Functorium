using AlwaysValid.ValueObjects;

namespace AlwaysValid;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 항상 유효한 타입 ===\n");

        // 유효한 Denominator 생성 케이스
        DemonstrateCreatedFromValidValue();

        // 무효한 Denominator 생성 케이스
        DemonstrateCreatedFromInvalidValue();

        // 나눗셈 함수 테스트
        DemonstrateDivisionWithValidType();
    }

    /// <summary>
    /// 유효한 Denominator 생성 케이스 시연
    /// </summary>
    static void DemonstrateCreatedFromValidValue()
    {
        var validResult = Denominator.Create(5);
        validResult.Match(
            Succ: value => Console.WriteLine($"유효한 값: {value}"),
            Fail: error => Console.WriteLine($"오류: {error.Message}")
        );
    }

    /// <summary>
    /// 무효한 Denominator 생성 케이스 시연
    /// </summary>
    static void DemonstrateCreatedFromInvalidValue()
    {
        var invalidResult = Denominator.Create(0);
        invalidResult.Match(
            Succ: value => Console.WriteLine($"유효한 값: {value}"),
            Fail: error => Console.WriteLine($"잘못된 값: 오류: {error.Message}")
        );
    }

    /// <summary>
    /// 항상 유효한 타입을 사용한 나눗셈 함수의 동작 시연
    /// </summary>
    static void DemonstrateDivisionWithValidType()
    {
        Console.WriteLine("나눗셈 함수 테스트:");
        var denominator = Denominator.Create(5);
        var result = MathOperations.Divide(10, (Denominator)denominator);
        Console.WriteLine($"10 / 5 = {result}");
    }
}
