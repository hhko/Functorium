namespace FunctionalResult;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 함수형 결과 타입 ===\n");

        // 성공적인 나눗셈 연산
        DemonstrateSuccessfulDivide();

        // 실패하는 나눗셈 연산
        DemonstrateFailedDivide();
    }

    /// <summary>
    /// 성공적인 나눗셈 연산을 함수형 결과 타입으로 처리하는 방법 시연
    /// </summary>
    static void DemonstrateSuccessfulDivide()
    {
        Console.WriteLine("성공 케이스:");
        var successResult = MathOperations.Divide(10, 2);
        successResult.Match(
            Succ: value => Console.WriteLine($"10 / 2 = {value}"),
            Fail: error => Console.WriteLine($"오류: {error.Message}")
        );

        Console.WriteLine();
    }

    /// <summary>
    /// 실패하는 나눗셈 연산을 함수형 결과 타입으로 처리하는 방법 시연
    /// </summary>
    static void DemonstrateFailedDivide()
    {
        Console.WriteLine("실패 케이스:");
        var failureResult = MathOperations.Divide(10, 0);
        failureResult.Match(
            Succ: value => Console.WriteLine($"10 / 0 = {value}"),
            Fail: error => Console.WriteLine($"10 / 0 = 오류: {error.Message}")
        );
    }
}
