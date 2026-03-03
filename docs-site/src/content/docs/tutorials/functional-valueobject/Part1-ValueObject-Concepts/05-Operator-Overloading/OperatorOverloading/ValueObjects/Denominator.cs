using LanguageExt;
using LanguageExt.Common;

namespace OperatorOverloading.ValueObjects;

/// <summary>
/// 0이 아닌 정수를 나타내는 분모 값 객체
/// 연산자 오버로딩을 통해 자연스러운 나눗셈 연산 지원
/// </summary>
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - 직접 인스턴스 생성 방지
    private Denominator(int value) =>
        _value = value;

    /// <summary>
    /// Denominator를 생성합니다. 0인 경우 실패를 반환합니다.
    /// </summary>
    /// <param name="value">0이 아닌 정수 값</param>
    /// <returns>성공 시 Denominator, 실패 시 Error</returns>
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("0은 허용되지 않습니다");

        return new Denominator(value);
    }

    // 연산자 오버로딩과 명시적 형변환을 이용하여 더 이상 내부 값을 직접 반환하지 않습니다.
    //public int Value => 
    //    _value;

    // 핵심 개선사항: int와 Denominator 간의 나눗셈 연산자

    /// <summary>
    /// int와 Denominator 간의 나눗셈 연산자
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <returns>나눗셈 결과</returns>
    public static int operator /(int numerator, Denominator denominator) =>
        numerator / denominator._value;

    /// <summary>
    /// Denominator와 int 간의 나눗셈 연산자
    /// </summary>
    /// <param name="denominator">분모</param>
    /// <param name="divisor">나누는 수</param>
    /// <returns>나눗셈 결과</returns>
    public static int operator /(Denominator denominator, int divisor) =>
        denominator._value / divisor;

    /// <summary>
    /// Denominator와 Denominator 간의 나눗셈 연산자
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <returns>나눗셈 결과</returns>
    public static int operator /(Denominator numerator, Denominator denominator) =>
        numerator._value / denominator._value;

    // 변환 연산자

    /// <summary>
    /// int를 Denominator로 명시적 변환
    /// </summary>
    /// <param name="value">변환할 값</param>
    public static explicit operator Denominator(int value) =>
        Create(value).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidCastException("0은 Denominator로 변환할 수 없습니다")
        );

    /// <summary>
    /// Denominator를 int로 암시적 변환
    /// </summary>
    /// <param name="value">변환할 값</param>
    public static explicit operator int(Denominator value) =>
        value._value;
}
