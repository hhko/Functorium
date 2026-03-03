namespace BasicDivide;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 기본 나눗셈 함수 ===\n");

        // 정상적인 나눗셈 연산
        DemonstrateSuccessfulDivide();

        // 예외가 발생하는 나눗셈 연산
        DemonstrateExceptionalDivide();
    }

    /// <summary>
    /// 정상적인 나눗셈 연산 시연
    /// </summary>
    static void DemonstrateSuccessfulDivide()
    {
        Console.WriteLine("정상 케이스:");
        try
        {
            var result = MathOperations.Divide(10, 2);
            Console.WriteLine($"10 / 2 = {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"예외 발생: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 예외가 발생하는 나눗셈 연산 시연
    /// </summary>
    static void DemonstrateExceptionalDivide()
    {
        Console.WriteLine("예외 케이스:");
        try
        {
            var result = MathOperations.Divide(10, 0);
            Console.WriteLine($"10 / 0 = {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"10 / 0 = {ex}");
        }
    }
}
