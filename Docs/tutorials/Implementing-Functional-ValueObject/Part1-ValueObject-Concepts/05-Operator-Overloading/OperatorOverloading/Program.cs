using OperatorOverloading.ValueObjects;

namespace OperatorOverloading;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 연산자 오버로딩을 통한 자연스러운 나눗셈 연산 ===\n");

        // 기본 연산자 오버로딩 테스트
        DemonstrateNaturalDivision();

        // 변환 연산자 테스트
        DemonstrateConversionOperators();

        // 에러 처리 테스트
        DemonstrateErrorHandling();
    }

    /// <summary>
    /// 자연스러운 나눗셈 연산 시연
    /// </summary>
    private static void DemonstrateNaturalDivision()
    {
        Console.WriteLine("1. 핵심 개선사항: 자연스러운 나눗셈 연산");
        Console.WriteLine("  Before (04-Always-Valid): numerator / denominator.Value");
        Console.WriteLine("  After  (05-Operator-Overloading): numerator / denominator");

        var denominator = Denominator.Create(5);
        denominator.Match(
            Succ: denom =>
            {
                // 핵심 개선사항: .Value 없이 자연스러운 연산
                int result = MathOperations.Divide(15, denom);
                Console.WriteLine($"  15 / {denom} = {result}");

                // 직접 연산자 사용도 가능
                int directResult = 15 / denom;
                Console.WriteLine($"  15 / {denom} = {directResult} (직접 연산자)");
            },
            Fail: error => Console.WriteLine($"  에러: {error}")
        );
    }

    /// <summary>
    /// 변환 연산자 시연
    /// </summary>
    private static void DemonstrateConversionOperators()
    {
        Console.WriteLine("\n2. 변환 연산자:");
        Console.WriteLine("  int에서 Denominator로 변환:");

        // 성공 케이스
        try
        {
            // 명시적 변환: Denominator -> int
            var nonZero = (Denominator)15;
            Console.WriteLine($"    15 -> Denominator: {nonZero}");

            // 암시적 변환: Denominator -> int
            int intValue = (int)nonZero;
            Console.WriteLine($"    Denominator -> int: {intValue}");
        }
        catch (InvalidCastException ex)
        {
            Console.WriteLine($"    변환 실패: {ex.Message}");
        }

        // 실패 케이스
        try
        {
            var nonZero = (Denominator)0;
            //Console.WriteLine($"    0 -> Denominator: {nonZero}");
        }
        catch (InvalidCastException ex)
        {
            Console.WriteLine($"    0 -> Denominator(변환 실패): {ex.Message}");
        }
    }

    /// <summary>
    /// 에러 처리 시연
    /// </summary>
    private static void DemonstrateErrorHandling()
    {
        Console.WriteLine("\n3. 에러 처리:");
        Console.WriteLine("  연산 중 에러 처리:");

        // 0으로 나누기 시도
        var ten = Denominator.Create(10);
        var zero = Denominator.Create(0);

        ten.Match(
            Succ: aVal => zero.Match(
                Succ: zeroVal =>
                {
                    // 이 경우는 발생하지 않아야 함 (0은 Denominator가 될 수 없음)
                    Console.WriteLine($"    {aVal} / {zeroVal} = {aVal / zeroVal}");
                },
                Fail: error => Console.WriteLine($"    Denominator 생성 실패: {error}")
            ),
            Fail: error => Console.WriteLine($"    에러: {error}")
        );
    }
}
