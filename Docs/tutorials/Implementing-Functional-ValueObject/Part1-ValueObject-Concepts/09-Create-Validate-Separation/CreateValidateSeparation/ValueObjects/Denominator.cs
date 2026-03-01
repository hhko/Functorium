using LanguageExt;
using LanguageExt.Common;

namespace CreateValidateSeparation.ValueObjects;

/// <summary>
/// 0이 아닌 정수를 나타내는 분모 값 객체
/// 단일 책임 원칙을 적용하여 검증 책임만 담당
/// 비교 가능성도 함께 구현
/// </summary>
public sealed class Denominator
    : IEquatable<Denominator>
    , IComparable<Denominator>
{
    private readonly int _value;

    // Private constructor - 직접 인스턴스 생성 방지
    private Denominator(int value) =>
        _value = value;

    /// <summary>
    /// Denominator 인스턴스를 생성하는 팩토리 메서드
    /// 검증 책임을 분리하여 단일 책임 원칙 준수
    /// </summary>
    /// <param name="value">0이 아닌 정수 값</param>
    /// <returns>성공 시 Denominator, 실패 시 Error</returns>
    public static Fin<Denominator> Create(int value) =>
        Validate(value)
            .Map(validNumber => new Denominator(validNumber))
            .ToFin();

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? Error.New("0은 허용되지 않습니다")
            : value;

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

    // 비교 가능성 구현: IComparable<T>

    /// <summary>
    /// IComparable<T> 구현 - 비교 책임
    /// </summary>
    public int CompareTo(Denominator? other)
    {
        if (other is null) return 1; // null보다는 모든 값이 큼
        return _value.CompareTo(other._value);
    }

    // 변환 연산자

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public static explicit operator Denominator(int value) =>
        Create(value).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidCastException("0은 Denominator로 변환할 수 없습니다")
        );

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public static explicit operator int(Denominator value) =>
        value._value;

    // 연산자 오버로딩

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

    public static int operator -(Denominator denominator1, Denominator denominator2) =>
        denominator1._value - denominator2._value;

    /// <summary>
    /// 작음 연산자 오버로딩
    /// </summary>
    public static bool operator <(Denominator? left, Denominator? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    /// <summary>
    /// 작거나 같음 연산자 오버로딩
    /// </summary>
    public static bool operator <=(Denominator? left, Denominator? right) =>
        left is null || left.CompareTo(right) <= 0;

    /// <summary>
    /// 큼 연산자 오버로딩
    /// </summary>
    public static bool operator >(Denominator? left, Denominator? right) =>
        left is not null && left.CompareTo(right) > 0;

    /// <summary>
    /// 크거나 같음 연산자 오버로딩
    /// </summary>
    public static bool operator >=(Denominator? left, Denominator? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}
