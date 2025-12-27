using LanguageExt;
using LanguageExt.Common;

namespace ValidatedValueCreation.ValueObjects;

/// <summary>
/// 주소를 나타내는 복합 값 객체
/// Street, City, PostalCode를 포함하여 완전한 주소 정보를 표현
/// </summary>
public sealed class Address : IEquatable<Address>
{
    public Street Street { get; }
    public City City { get; }
    public PostalCode PostalCode { get; }

    /// <summary>
    /// Address 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="street">거리명</param>
    /// <param name="city">도시명</param>
    /// <param name="postalCode">우편번호</param>
    private Address(Street street, City city, PostalCode postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    /// <summary>
    /// Address 인스턴스를 생성하는 팩토리 메서드
    /// 검증 책임을 분리하여 단일 책임 원칙 준수
    /// </summary>
    /// <param name="streetValue">거리명</param>
    /// <param name="cityValue">도시명</param>
    /// <param name="postalCodeValue">우편번호</param>
    /// <returns>성공 시 Address, 실패 시 Error</returns>
    public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
        Validate(streetValue, cityValue, postalCodeValue)
            .Map(validatedValues => new Address(
                validatedValues.Street,
                validatedValues.City,
                validatedValues.PostalCode))
            .ToFin();

    /// <summary>
    /// 이미 검증된 값 객체들로부터 Address 인스턴스를 생성하는 internal 메서드
    /// 외부(부모)에서만 사용하며, 자기 자신의 Create에서는 사용하지 않음
    /// </summary>
    /// <param name="street">검증된 거리명</param>
    /// <param name="city">검증된 도시명</param>
    /// <param name="postalCode">검증된 우편번호</param>
    /// <returns>Address 인스턴스</returns>
    internal static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
        new Address(street, city, postalCode);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// 각 구성 요소들의 검증을 조합하여 복합 검증 수행
    /// 
    /// 검증 방식: AND 조건 (All or Nothing)
    /// - 모든 구성 요소(Street, City, PostalCode)가 유효해야만 성공
    /// - 하나라도 유효하지 않으면 전체 검증 실패
    /// - LanguageExt의 Validation 모나드를 사용하여 순차적 AND 검증 수행
    /// </summary>
    /// <param name="streetValue">검증할 거리명</param>
    /// <param name="cityValue">검증할 도시명</param>
    /// <param name="postalCodeValue">검증할 우편번호</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
        string streetValue,
        string cityValue,
        string postalCodeValue)
    {
        var streetValidation = Street.Validate(streetValue);
        var cityValidation = City.Validate(cityValue);
        var postalCodeValidation = PostalCode.Validate(postalCodeValue);

        // AND 조건 검증: street AND city AND postalCode
        // 모든 검증이 성공해야만 전체가 성공 (All or Nothing)
        return from street in streetValidation
               from city in cityValidation
               from postalCode in postalCodeValidation
               select (Street: Street.CreateFromValidated(street),
                       City: City.CreateFromValidated(city),
                       PostalCode: PostalCode.CreateFromValidated(postalCode));
    }

    // 값 기반 동등성 구현

    /// <summary>
    /// IEquatable<T> 구현 - 동등성 비교 책임
    /// </summary>
    public bool Equals(Address? other)
    {
        if (other is null) return false;
        return Street == other.Street &&
               City == other.City &&
               PostalCode == other.PostalCode;
    }

    /// <summary>
    /// Object.Equals 오버라이드
    /// </summary>
    public override bool Equals(object? obj) =>
        Equals(obj as Address);

    /// <summary>
    /// GetHashCode 오버라이드
    /// </summary>
    public override int GetHashCode() =>
        HashCode.Combine(Street, City, PostalCode);

    /// <summary>
    /// 동등성 연산자 오버로딩
    /// </summary>
    public static bool operator ==(Address? left, Address? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// 부등성 연산자 오버로딩
    /// </summary>
    public static bool operator !=(Address? left, Address? right) =>
        !(left == right);

    /// <summary>
    /// 문자열 표현
    /// </summary>
    public override string ToString() =>
        $"{Street}, {City} {PostalCode}";
}
