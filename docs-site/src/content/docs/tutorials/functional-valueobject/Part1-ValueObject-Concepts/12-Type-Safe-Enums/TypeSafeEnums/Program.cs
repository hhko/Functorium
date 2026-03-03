using TypeSafeEnums.ValueObjects.Comparable.CompositeValueObjects;

namespace TypeSafeEnums;

/// <summary>
/// ValueObject Framework 데모 프로그램
/// 
/// 프레임워크의 효율성을 보여주는 6가지 시나리오:
/// 비교 가능한 복합 값 객체: PriceRange (ComparableValueObject)
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ValueObject Framework 데모 ===\n");

        // 비교 가능한 복합 값 객체 - PriceRange
        DemonstrateComparableCompositeValueObject();
    }

    /// <summary>
    /// SmartEnum 기반 Currency와 PriceRange 시연
    /// SmartEnum을 사용한 타입 안전한 통화 처리와 PriceRange 조합
    /// </summary>
    static void DemonstrateComparableCompositeValueObject()
    {
        Console.WriteLine("   SmartEnum 기반 Currency와 PriceRange (가격 범위)");
        Console.WriteLine("   SmartEnum을 사용한 타입 안전한 통화 처리와 PriceRange 조합\n");

        // SmartEnum Currency 데모
        Console.WriteLine("   📋 지원되는 통화 목록:");
        foreach (var currencyItem in Currency.GetAllSupportedCurrencies())
        {
            Console.WriteLine($"      - {currencyItem} (코드: {currencyItem.GetCode()})");
        }
        Console.WriteLine();

        // 성공 케이스들 - 다양한 통화
        var priceRange1 = PriceRange.Create(10000, 50000, "KRW");
        priceRange1.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공 (KRW): {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange2 = PriceRange.Create(100, 500, "USD");
        priceRange2.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공 (USD): {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange3 = PriceRange.Create(80, 400, "EUR");
        priceRange3.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공 (EUR): {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스들
        Console.WriteLine("\n   🚫 실패 케이스들:");
        
        var priceRange4 = PriceRange.Create(-1000, 50000, "KRW");
        priceRange4.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange5 = PriceRange.Create(10000, -5000, "KRW");
        priceRange5.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange6 = PriceRange.Create(50000, 10000, "KRW");
        priceRange6.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 지원하지 않는 통화 코드
        var priceRange7 = PriceRange.Create(10000, 50000, "INVALID");
        priceRange7.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // SmartEnum Currency 직접 사용 데모
        Console.WriteLine("\n   💰 SmartEnum Currency 직접 사용:");
        var krw = Currency.KRW;
        var usd = Currency.USD;
        
        Console.WriteLine($"      KRW: {krw} - {krw.FormatAmount(12345.67m)}");
        Console.WriteLine($"      USD: {usd} - {usd.FormatAmount(123.45m)}");
        Console.WriteLine($"      EUR: {Currency.EUR} - {Currency.EUR.FormatAmount(89.12m)}");
        
        // 통화 지원 여부 확인
        Console.WriteLine($"\n   🔍 통화 지원 여부 확인:");
        Console.WriteLine($"      KRW 지원: {Currency.Validate("KRW").Match(Succ: _ => true, Fail: _ => false)}");
        Console.WriteLine($"      USD 지원: {Currency.Validate("USD").Match(Succ: _ => true, Fail: _ => false)}");
        Console.WriteLine($"      INVALID 지원: {Currency.Validate("INVALID").Match(Succ: _ => true, Fail: _ => false)}");

        // 비교 기능 데모
        Console.WriteLine("\n   📊 비교 기능 데모:");
        var range1 = PriceRange.Create(10000, 30000, "KRW").IfFail(_ => throw new Exception("생성 실패"));
        var range2 = PriceRange.Create(20000, 40000, "KRW").IfFail(_ => throw new Exception("생성 실패"));
        var range3 = PriceRange.Create(10000, 30000, "KRW").IfFail(_ => throw new Exception("생성 실패"));

        Console.WriteLine($"   - {range1} < {range2} = {range1 < range2}");
        Console.WriteLine($"   - {range1} == {range3} = {range1 == range3}");
        Console.WriteLine($"   - {range1} > {range2} = {range1 > range2}");
        Console.WriteLine($"   - {range1} <= {range3} = {range1 <= range3}");
        Console.WriteLine($"   - {range1} >= {range3} = {range1 >= range3}");
        Console.WriteLine($"   - {range1} != {range2} = {range1 != range2}");

        // 개별 값 객체 생성 데모
        Console.WriteLine("\n   📋 개별 값 객체 생성:");
        var minPrice = Price.Create(15000, "USD").IfFail(_ => throw new Exception("생성 실패"));
        var maxPrice = Price.Create(35000, "USD").IfFail(_ => throw new Exception("생성 실패"));
        var currency = Currency.Create("USD").IfFail(_ => throw new Exception("생성 실패"));

        Console.WriteLine($"   - MinPrice: {minPrice} (금액: {(decimal)minPrice.Amount})");
        Console.WriteLine($"   - MaxPrice: {maxPrice} (금액: {(decimal)maxPrice.Amount})");
        Console.WriteLine($"   - Currency: {currency} (값: {currency.GetCode()})");

        // CreateFromValidated 데모
        var priceRangeFromValidated = PriceRange.CreateFromValidated(minPrice, maxPrice);
        Console.WriteLine($"   - PriceRange from validated: {priceRangeFromValidated}");

        // 개선된 Price 비교 기능 데모
        Console.WriteLine("\n   🔄 Price 비교 기능 데모:");
        
        // 같은 통화 비교
        var usdPrice1 = Price.Create(100, "USD").IfFail(_ => throw new Exception("생성 실패"));
        var usdPrice2 = Price.Create(200, "USD").IfFail(_ => throw new Exception("생성 실패"));
        var usdPrice3 = Price.Create(100, "USD").IfFail(_ => throw new Exception("생성 실패"));
        
        Console.WriteLine($"   📊 같은 통화 (USD) 비교:");
        Console.WriteLine($"      - {usdPrice1} < {usdPrice2} = {usdPrice1 < usdPrice2}");
        Console.WriteLine($"      - {usdPrice1} == {usdPrice3} = {usdPrice1 == usdPrice3}");
        Console.WriteLine($"      - {usdPrice1} > {usdPrice2} = {usdPrice1 > usdPrice2}");
        Console.WriteLine($"      - CanCompareWith: {usdPrice1.CanCompareWith(usdPrice2)} = {usdPrice1.CanCompareWith(usdPrice2)}");
        
        // 다른 통화 비교
        var krwPrice = Price.Create(100000, "KRW").IfFail(_ => throw new Exception("생성 실패"));
        var eurPrice = Price.Create(80, "EUR").IfFail(_ => throw new Exception("생성 실패"));
        
        Console.WriteLine($"\n   🌍 다른 통화 비교:");
        Console.WriteLine($"      - USD vs KRW: {usdPrice1} vs {krwPrice}");
        Console.WriteLine($"      - CanCompareWith: {usdPrice1.CanCompareWith(krwPrice)} = {usdPrice1.CanCompareWith(krwPrice)}");
        Console.WriteLine($"      - 비교 결과: {usdPrice1 < krwPrice} (통화 우선 비교)");
        
        Console.WriteLine($"      - USD vs EUR: {usdPrice1} vs {eurPrice}");
        Console.WriteLine($"      - CanCompareWith: {usdPrice1.CanCompareWith(eurPrice)} = {usdPrice1.CanCompareWith(eurPrice)}");
        Console.WriteLine($"      - 비교 결과: {usdPrice1 < eurPrice} (통화 우선 비교)");
        
        // 안전한 비교 유틸리티 데모
        Console.WriteLine($"\n   🛡️ 안전한 비교 유틸리티:");
        Console.WriteLine($"      - {ComparePrices(usdPrice1, usdPrice2)}");
        Console.WriteLine($"      - {ComparePrices(usdPrice1, krwPrice)}");
        Console.WriteLine($"      - {ComparePrices(krwPrice, eurPrice)}");
        
        // 정렬 데모
        Console.WriteLine($"\n   📈 가격 정렬 데모 (통화 우선, 금액 순):");
        var prices = new[] { usdPrice2, krwPrice, usdPrice1, eurPrice, usdPrice3 };
        var sortedPrices = prices.OrderBy(p => p).ToArray();
        
        for (int i = 0; i < sortedPrices.Length; i++)
        {
            Console.WriteLine($"      {i + 1}. {sortedPrices[i]}");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// 가격 비교 유틸리티 메서드
    /// CanCompareWith를 사용한 안전한 가격 비교
    /// </summary>
    /// <param name="price1">첫 번째 가격</param>
    /// <param name="price2">두 번째 가격</param>
    /// <returns>비교 결과 문자열</returns>
    static string ComparePrices(Price price1, Price price2)
    {
        if (!price1.CanCompareWith(price2))
        {
            return $"서로 다른 통화는 비교할 수 없습니다: {price1.Currency} vs {price2.Currency}";
        }
        
        if (price1 < price2)
            return $"{price1} < {price2}";
        else if (price1 > price2)
            return $"{price1} > {price2}";
        else
            return $"{price1} == {price2}";
    }

}