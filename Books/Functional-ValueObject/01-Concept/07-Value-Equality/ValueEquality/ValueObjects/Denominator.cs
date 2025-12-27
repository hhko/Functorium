using LanguageExt;
using LanguageExt.Common;

namespace ValueEquality.ValueObjects;

/// <summary>
/// 0이 아닌 정수를 나타내는 분모 값 객체
/// 값 기반 동등성 구현: IEquatable<T>
/// </summary>
public sealed class Denominator
    : IEquatable<Denominator>
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

    // 값 기반 동등성 구현: IEquatable<T>

    /// <summary>
    /// IEquatable<T> 구현 - 타입 안전한 동등성 비교
    /// </summary>
    public bool Equals(Denominator? other) =>
        other is not null && _value == other._value;

    /// <summary>
    /// Object.Equals 오버라이드 - 참조 동등성이 아닌 값 동등성 사용
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is Denominator other && Equals(other);

    /// <summary>
    /// 동등성 연산자 오버로딩
    /// </summary>
    public static bool operator ==(Denominator? left, Denominator? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// 부등성 연산자 오버로딩
    /// </summary>
    public static bool operator !=(Denominator? left, Denominator? right) =>
        !(left == right);

    /// <summary>
    /// GetHashCode 오버라이드 - 값 기반 해시 코드 생성
    /// Equals와 GetHashCode는 항상 함께 오버라이드해야 함
    /// </summary>
    public override int GetHashCode() =>
        _value.GetHashCode();

    /// <summary>
    /// 문자열 표현
    /// </summary>
    public override string ToString() =>
        _value.ToString();

    // 기본 연산자들

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
    /// Denominator을 int로 명시적 변환
    /// </summary>
    /// <param name="value">변환할 값</param>
    public static explicit operator int(Denominator denominator) =>
        denominator._value;
}
