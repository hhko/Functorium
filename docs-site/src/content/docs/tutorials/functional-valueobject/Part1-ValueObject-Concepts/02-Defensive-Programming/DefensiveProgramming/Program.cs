namespace DefensiveProgramming;

/// <summary>
/// 방어적 프로그래밍의 두 가지 구현 방법을 시연하는 메인 프로그램
/// 
/// 이 프로그램은 방어적 프로그래밍의 발전 과정을 보여줍니다:
/// 1. 사전 검증을 통한 정의된(의도된) 예외 처리
/// 2. 예외 없이 bool 반환을 활용한 TryDivide 패턴
/// 
/// 두 방법 모두 여전히 부작용을 가지고 있어, 다음 단계인 Functional Result에서 해결됩니다.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 방어적 프로그래밍의 두 가지 구현 방법 ===\n");

        Console.WriteLine("=== 10 / 2 계산 시도 ===");

        // 정상적인 나눗셈 연산
        DemonstrateExceptionBasedDivide(10, 2);
        DemonstrateTryPatternDivide(10, 2);

        Console.WriteLine("\n=== 10 / 0 계산 시도 ===");

        // 예외가 발생하는 나눗셈 연산
        DemonstrateExceptionBasedDivide(10, 0);
        DemonstrateTryPatternDivide(10, 0);
    }

    /// <summary>
    /// 예외 기반 Divide 메서드의 동작 시연
    /// </summary>
    /// <param name="dividend">피제수</param>
    /// <param name="divisor">제수</param>
    static void DemonstrateExceptionBasedDivide(int dividend, int divisor)
    {
        Console.WriteLine("\n방법 1: 예외 기반 Divide");
        try
        {
            int result = MathOperations.Divide(dividend, divisor);
            Console.WriteLine($" 성공: {result}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($" 실패: {ex.Message} {(divisor == 0 ? "(프로그램 흐름 중단 부작용)" : "")}");
        }
    }

    /// <summary>
    /// Try 패턴 TryDivide 메서드의 동작 시연
    /// </summary>
    /// <param name="dividend">피제수</param>
    /// <param name="divisor">제수</param>
    static void DemonstrateTryPatternDivide(int dividend, int divisor)
    {
        Console.WriteLine("\n방법 2: Try 패턴 TryDivide");
        if (MathOperations.TryDivide(dividend, divisor, out int tryResult))
        {
            Console.WriteLine($" 성공: {tryResult}");
        }
        else
        {
            Console.WriteLine($" 실패: 계산할 수 없음 {(divisor == 0 ? $"(외부 상태 변경 부작용: result = {tryResult})" : "")}");
        }
    }
}
