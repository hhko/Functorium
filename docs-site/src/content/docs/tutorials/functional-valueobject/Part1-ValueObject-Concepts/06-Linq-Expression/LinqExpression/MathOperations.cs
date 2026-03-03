using LinqExpression.ValueObjects;

namespace LinqExpression;

/// <summary>
/// LINQ 표현식을 활용한 함수형 에러 처리 예제
/// from 키워드를 사용하여 복잡한 Match 체인을 단순화합니다.
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
        return numerator / denominator;
    }
}
