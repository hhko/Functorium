using OperatorOverloading.ValueObjects;

namespace OperatorOverloading;

/// <summary>
/// 연산자 오버로딩을 활용한 나눗셈 연산 예제
/// 도메인 언어를 자연스럽게 표현하는 방법을 보여줍니다.
/// </summary>
public static class MathOperations
{
    /// <summary>
    /// 연산자 오버로딩을 사용한 자연스러운 나눗셈 연산
    /// numerator / denominator 형태로 도메인 언어를 표현
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모 (항상 0이 아님을 보장)</param>
    /// <returns>나눗셈 결과</returns>
    public static int Divide(int numerator, Denominator denominator)
    {
        // 핵심 개선사항: .Value 없이 자연스러운 연산
        return numerator / denominator;
    }
}
