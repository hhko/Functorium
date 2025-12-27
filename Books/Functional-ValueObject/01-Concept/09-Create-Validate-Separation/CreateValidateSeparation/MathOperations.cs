using CreateValidateSeparation.ValueObjects;

namespace CreateValidateSeparation;

/// <summary>
/// 수학 연산을 담당하는 클래스
/// 단일 책임: 수학 연산 로직만 담당
/// </summary>
public static class MathOperations
{
    /// <summary>
    /// 나눗셈 연산
    /// 수학 연산 책임만 담당, 검증은 Denominator가 담당
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모 (Denominator로 검증됨)</param>
    /// <returns>나눗셈 결과</returns>
    public static int Divide(int numerator, Denominator denominator)
    {
        // 검증 책임은 Denominator가 담당하므로 여기서는 검증하지 않음
        return numerator / denominator;
    }
}
