namespace BasicDivide;

public static class MathOperations
{
    /// <summary>
    /// 문제가 있는 기본 나눗셈 함수
    /// denominator가 0일 경우 DivideByZeroException이 발생합니다.
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <returns>나눗셈 결과</returns>
    /// <exception cref="DivideByZeroException">denominator가 0일 때 발생</exception>
    public static int Divide(int numerator, int denominator)
    {
        // denominator가 0이면 예외 발생!
        return numerator / denominator;
    }
}
