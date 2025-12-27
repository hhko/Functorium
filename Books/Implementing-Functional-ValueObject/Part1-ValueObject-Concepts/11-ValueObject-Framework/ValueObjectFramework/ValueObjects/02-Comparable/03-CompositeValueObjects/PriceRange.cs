using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 가격 범위를 나타내는 값 객체
/// ComparableValueObject 기반으로 비교 가능한 composite 값 객체 구현
/// Price 값 객체를 조합하여 구성
/// ValueObject 규칙을 준수하여 구현
/// </summary>
public sealed class PriceRange : ComparableValueObject
{
    /// <summary>
    /// 최소 가격
    /// </summary>
    public Price MinPrice { get; }

    /// <summary>
    /// 최대 가격
    /// </summary>
    public Price MaxPrice { get; }

    /// <summary>
    /// PriceRange 인스턴스를 생성하는 private 생성자
    /// </summary>
    /// <param name="minPrice">최소 가격</param>
    /// <param name="maxPrice">최대 가격</param>
    private PriceRange(Price minPrice, Price maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    /// <summary>
    /// PriceRange 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    /// <param name="minPriceValue">최소 가격 값</param>
    /// <param name="maxPriceValue">최대 가격 값</param>
    /// <param name="currencyCode">통화 코드</param>
    /// <returns>생성 결과</returns>
    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice));

    /// <summary>
    /// PriceRange 인스턴스를 생성하는 팩토리 메서드 (값 객체 직접 전달)
    /// </summary>
    /// <param name="minPrice">최소 가격 값 객체</param>
    /// <param name="maxPrice">최대 가격 값 객체</param>
    /// <returns>생성 결과</returns>
    internal static PriceRange CreateFromValidated(Price minPrice, Price maxPrice) =>
        new PriceRange(minPrice, maxPrice);

    /// <summary>
    /// 가격 범위 검증
    /// LINQ Expression을 사용한 함수형 프로그래밍 스타일
    /// </summary>
    /// <param name="minPriceValue">최소 가격 값</param>
    /// <param name="maxPriceValue">최대 가격 값</param>
    /// <param name="currencyCode">통화 코드</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (Price MinPrice, Price MaxPrice)> Validate(
        decimal minPriceValue, 
        decimal maxPriceValue, 
        string currencyCode) =>
        from validMinPriceTuple in Price.Validate(minPriceValue, currencyCode)
        from validMaxPriceTuple in Price.Validate(maxPriceValue, currencyCode)
        from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPriceTuple),
            Price.CreateFromValidated(validMaxPriceTuple))
        select validPriceRange;

    /// <summary>
    /// 가격 범위 검증 (값 객체로부터)
    /// </summary>
    /// <param name="minPrice">최소 가격 값 객체</param>
    /// <param name="maxPrice">최대 가격 값 객체</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(Price minPrice, Price maxPrice) =>
        (decimal)minPrice.Amount > (decimal)maxPrice.Amount
            ? Error.New($"최소 가격은 최대 가격보다 작거나 같아야 합니다: {minPrice} > {maxPrice}")
            : (MinPrice: minPrice, MaxPrice: maxPrice);

    /// <summary>
    /// 비교 가능한 동등성 구성 요소 반환
    /// 통화를 먼저 비교하고, 통화가 같으면 금액으로 비교
    /// </summary>
    /// <returns>비교 가능한 구성 요소들</returns>
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return MinPrice.Currency.GetCode();    // 통화를 먼저 비교
        yield return (decimal)MinPrice.Amount;       // 최소 금액 비교
        yield return (decimal)MaxPrice.Amount;       // 최대 금액 비교
    }

    /// <summary>
    /// 가격 범위의 문자열 표현
    /// </summary>
    /// <returns>가격 범위의 문자열 표현</returns>
    public override string ToString() =>
        $"{MinPrice} ~ {MaxPrice}";

    // 비교 기능은 ComparableValueObject에서 자동으로 제공됨:
    // - IComparable<PriceRange> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=, ==, !=)
    // - GetComparableEqualityComponents() 자동 구현
}
