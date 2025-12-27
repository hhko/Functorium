using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCode.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 주소를 나타내는 복합 값 객체 (3개 Validation 조합 예제)
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class Address : ValueObject
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
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="streetValue">거리명</param>
    /// <param name="cityValue">도시명</param>
    /// <param name="postalCodeValue">우편번호</param>
    /// <returns>성공 시 Address, 실패 시 Error</returns>
    public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
        CreateFromValidation(
            Validate(streetValue, cityValue, postalCodeValue),
            validValues => new Address(
                validValues.Street,
                validValues.City,
                validValues.PostalCode));

    /// <summary>
    /// 이미 검증된 값으로 Address 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="street">이미 검증된 거리명</param>
    /// <param name="city">이미 검증된 도시명</param>
    /// <param name="postalCode">이미 검증된 우편번호</param>
    /// <returns>생성된 Address 인스턴스</returns>
    internal static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
        new Address(street, city, postalCode);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 부모 클래스의 CombineValidations 헬퍼를 활용하여 간결하게 구현
    /// 각 구성 요소들의 검증을 조합하여 복합 검증 수행
    /// </summary>
    /// <param name="streetValue">검증할 거리명</param>
    /// <param name="cityValue">검증할 도시명</param>
    /// <param name="postalCodeValue">검증할 우편번호</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
            string street,
            string city,
            string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select
        (
            Street: Street.CreateFromValidated(validStreet),
            City: City.CreateFromValidated(validCity),
            PostalCode: PostalCode.CreateFromValidated(validPostalCode)
        );

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }

    public override string ToString() =>
        $"{Street}, {City} {PostalCode}";
}
