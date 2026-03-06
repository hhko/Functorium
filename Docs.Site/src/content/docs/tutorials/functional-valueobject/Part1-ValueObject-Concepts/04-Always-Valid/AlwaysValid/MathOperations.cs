using AlwaysValid.ValueObjects;

namespace AlwaysValid;

public static class MathOperations
{
    /// <summary>
    /// 값 객체를 사용한 안전한 나눗셈 함수
    /// denominator는 항상 유효한 Denominator이므로 검증이 불필요합니다.
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모 (항상 0이 아님을 보장)</param>
    /// <returns>나눗셈 결과</returns>
    public static int Divide(int numerator, Denominator denominator)
    {
        // 검증 불필요! 항상 유효함!
        return numerator / denominator.Value;
    }
}
