using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 6. 비교 가능한 복합 값 객체 - ComparableValueObject
/// 주소를 나타내는 값 객체 (여러 값 객체 조합)
/// 
/// 특징:
/// - 복잡한 검증 로직을 가진 값 객체
/// - 비교 기능 자동 제공
/// - 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
/// </summary>
public sealed class Address : ComparableValueObject
{
    public Street Street { get; }
    public City City { get; }
    public PostalCode PostalCode { get; }

    private Address(Street street, City city, PostalCode postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    /// <summary>
    /// 주소 값 객체 생성
    /// </summary>
    /// <param name="street">도로명</param>
    /// <param name="city">도시명</param>
    /// <param name="postalCode">우편번호</param>
    /// <returns>성공 시 Address 값 객체, 실패 시 에러</returns>
    public static Fin<Address> Create(string street, string city, string postalCode) =>
        CreateFromValidation(
            Validate(street, city, postalCode),
            validValues => new Address(validValues.Street, validValues.City, validValues.PostalCode));

    /// <summary>
    /// 이미 검증된 주소로 값 객체 생성
    /// </summary>
    /// <param name="validatedValues">검증된 주소 값들</param>
    /// <returns>Address 값 객체</returns>
    internal static Address CreateFromValidated((Street Street, City City, PostalCode PostalCode) validatedValues) =>
        new Address(validatedValues.Street, validatedValues.City, validatedValues.PostalCode);

    /// <summary>
    /// 주소 유효성 검증
    /// </summary>
    /// <param name="street">도로명</param>
    /// <param name="city">도시명</param>
    /// <param name="postalCode">우편번호</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
        string street, string city, string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select (Street: Street.CreateFromValidated(validStreet),
                City: City.CreateFromValidated(validCity),
                PostalCode: PostalCode.CreateFromValidated(validPostalCode));

    /// <summary>
    /// 비교 가능한 구성 요소 반환
    /// </summary>
    /// <returns>비교 가능한 구성 요소</returns>
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return (string)City;        // 도시를 먼저 비교 (가장 큰 단위)
        yield return (string)PostalCode;  // 우편번호를 두 번째로 비교 (지역 구분)
        yield return (string)Street;      // 도로명을 마지막에 비교 (세부 주소)
    }

    public override string ToString() => $"{Street}, {City} {PostalCode}";
}
