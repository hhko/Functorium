using LanguageExt;
using LanguageExt.Common;

namespace ValidatedValueCreation.ValueObjects;

/// <summary>
/// 도시명을 나타내는 값 객체
/// 단일 책임 원칙을 적용하여 검증 책임만 담당
/// </summary>
public sealed class City : IEquatable<City>
{
    private readonly string _value;

    /// <summary>
    /// City 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="value">도시명</param>
    private City(string value) =>
        _value = value;

    /// <summary>
    /// City 인스턴스를 생성하는 팩토리 메서드
    /// 검증 책임을 분리하여 단일 책임 원칙 준수
    /// </summary>
    /// <param name="value">도시명</param>
    /// <returns>성공 시 City, 실패 시 Error</returns>
    public static Fin<City> Create(string value) =>
        Validate(value)
            .Map(validValue => new City(validValue))
            .ToFin();

    /// <summary>
    /// 이미 검증된 값으로부터 City 인스턴스를 생성하는 internal 메서드
    /// 외부(부모)에서만 사용하며, 자기 자신의 Create에서는 사용하지 않음
    /// </summary>
    /// <param name="validatedValue">검증된 도시명</param>
    /// <returns>City 인스턴스</returns>
    internal static City CreateFromValidated(string validatedValue) =>
        new City(validatedValue);

    ///// <summary>
    ///// 값 접근 책임 - 단일 책임 원칙
    ///// </summary>
    //public string Value => _value;

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Error.New("도시명은 비어있을 수 없습니다")
            : value;

    // 값 기반 동등성 구현

    /// <summary>
    /// IEquatable<T> 구현 - 동등성 비교 책임
    /// </summary>
    public bool Equals(City? other)
    {
        if (other is null) return false;
        return _value == other._value;
    }

    /// <summary>
    /// Object.Equals 오버라이드
    /// </summary>
    public override bool Equals(object? obj) =>
        Equals(obj as City);

    /// <summary>
    /// GetHashCode 오버라이드
    /// </summary>
    public override int GetHashCode() =>
        _value.GetHashCode();

    /// <summary>
    /// 동등성 연산자 오버로딩
    /// </summary>
    public static bool operator ==(City? left, City? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// 부등성 연산자 오버로딩
    /// </summary>
    public static bool operator !=(City? left, City? right) =>
        !(left == right);

    /// <summary>
    /// 문자열 표현
    /// </summary>
    public override string ToString() =>
        _value;

    /// <summary>
    /// City을 string로 명시적 변환
    /// </summary>
    /// <param name="value">변환할 값</param>
    public static explicit operator string(City city) =>
        city._value;
}
