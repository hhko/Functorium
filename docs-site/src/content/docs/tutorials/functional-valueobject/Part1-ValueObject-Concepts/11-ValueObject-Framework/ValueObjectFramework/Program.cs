using ValueObjectFramework.ValueObjects.Comparable.CompositePrimitiveValueObjects;
using ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;
using ValueObjectFramework.ValueObjects.Comparable.PrimitiveValueObjects;
using ValueObjectFramework.ValueObjects.ComparableNot.CompositePrimitiveValueObjects;
using ValueObjectFramework.ValueObjects.ComparableNot.CompositeValueObjects;
using ValueObjectFramework.ValueObjects.ComparableNot.PrimitiveValueObjects;

namespace ValueObjectFramework;

/// <summary>
/// ValueObject Framework 데모 프로그램
/// 
/// 프레임워크의 효율성을 보여주는 6가지 시나리오:
/// 1. 비교 가능한 primitive 값 객체: Denominator (ComparableSimpleValueObject<int>)
/// 2. 비교 불가능한 primitive 값 객체: BinaryData (SimpleValueObject<byte[]>)
/// 3. 비교 불가능한 복합 primitive 값 객체: Coordinate (ValueObject)
/// 4. 비교 가능한 복합 primitive 값 객체: DateRange (ComparableValueObject)
/// 5. 비교 불가능한 복합 값 객체: Address (ValueObject)
/// 6. 비교 가능한 복합 값 객체: PriceRange (ComparableValueObject)
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ValueObject Framework 데모 ===\n");

        // 1. 비교 불가능한 primitive 값 객체 - BinaryData
        DemonstrateComparableNotPrimitiveValueObject();

        // 2. 비교 가능한 primitive 값 객체 - Denominator
        DemonstrateComparablePrimitiveValueObject();

        // 3. 비교 불가능한 복합 primitive 값 객체 - Coordinate
        DemonstrateComparableNotCompositePrimitiveValueObject();

        // 4. 비교 가능한 복합 primitive 값 객체 - DateRange
        DemonstrateComparableCompositePrimitiveValueObject();

        // 5. 비교 불가능한 복합 값 객체 - Address
        DemonstrateComparableNotCompositeValueObject();

        // 6. 비교 가능한 복합 값 객체 - PriceRange
        DemonstrateComparableCompositeValueObject();
    }

    /// <summary>
    /// 1. 비교 불가능한 primitive 값 객체 - BinaryData 시연
    /// SimpleValueObject<T> 기반으로 간결하게 구현
    /// </summary>
    static void DemonstrateComparableNotPrimitiveValueObject()
    {
        Console.WriteLine("1. 비교 불가능한 primitive 값 객체 - BinaryData (바이너리 데이터)");
        Console.WriteLine("   SimpleValueObject<byte[]> 기반으로 간결하게 구현\n");

        // 성공 케이스
        var binaryData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        var binaryResult = BinaryData.Create(binaryData);
        binaryResult.Match(
            Succ: b => Console.WriteLine($"   ✅ 성공: {b}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - 빈 배열
        var emptyResult = BinaryData.Create(new byte[0]);
        emptyResult.Match(
            Succ: b => Console.WriteLine($"   ✅ 성공: {b}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - null
        var nullResult = BinaryData.Create(null!);
        nullResult.Match(
            Succ: b => Console.WriteLine($"   ✅ 성공: {b}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 동등성 비교 (비교 기능은 제공되지 않음)
        var b1 = BinaryData.Create(new byte[] { 1, 2, 3 }).IfFail(_ => throw new Exception("생성 실패"));
        var b2 = BinaryData.Create(new byte[] { 1, 2, 3 }).IfFail(_ => throw new Exception("생성 실패"));
        var b3 = BinaryData.Create(new byte[] { 4, 5, 6 }).IfFail(_ => throw new Exception("생성 실패"));
        Console.WriteLine($"   📊 동등성: {b1} == {b2} = {b1 == b2}");
        Console.WriteLine($"   📊 동등성: {b1} == {b3} = {b1 == b3}");
        Console.WriteLine($"   📊 비교 기능: 제공되지 않음 (의도적으로)\n");
    }

    /// <summary>
    /// 2. 비교 가능한 primitive 값 객체 - Denominator 시연
    /// ComparableSimpleValueObject<T> 기반으로 간결하게 구현
    /// </summary>
    static void DemonstrateComparablePrimitiveValueObject()
    {
        Console.WriteLine("2. 비교 가능한 primitive 값 객체 - Denominator (0이 아닌 정수)");
        Console.WriteLine("   ComparableSimpleValueObject<int> 기반으로 간결하게 구현\n");

        // 성공 케이스
        var denominatorResult = Denominator.Create(5);
        denominatorResult.Match(
            Succ: d => Console.WriteLine($"   ✅ 성공: {d} (값: {(int)d})"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스
        var zeroResult = Denominator.Create(0);
        zeroResult.Match(
            Succ: d => Console.WriteLine($"   ✅ 성공: {d}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 비교 가능성 데모
        var d1 = Denominator.Create(3).IfFail(_ => throw new Exception("생성 실패"));
        var d2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));
        Console.WriteLine($"   📊 비교: {d1} < {d2} = {d1 < d2}");
        Console.WriteLine($"   📊 비교: {d1} == {d2} = {d1 == d2}\n");
    }

    /// <summary>
    /// 3. 비교 불가능한 복합 primitive 값 객체 - Coordinate 시연
    /// ValueObject 기반으로 2개 Validation 조합
    /// </summary>
    static void DemonstrateComparableNotCompositePrimitiveValueObject()
    {
        Console.WriteLine("3. 비교 불가능한 복합 primitive 값 객체 - Coordinate (X, Y 좌표)");
        Console.WriteLine("   ValueObject 기반으로 2개 Validation 조합\n");

        // 성공 케이스
        var coordinateResult = Coordinate.Create(100, 200);
        coordinateResult.Match(
            Succ: c => Console.WriteLine($"   ✅ 성공: {c} (X: {c.X}, Y: {c.Y})"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - X 좌표 범위 초과
        var invalidXResult = Coordinate.Create(1500, 200);
        invalidXResult.Match(
            Succ: c => Console.WriteLine($"   ✅ 성공: {c}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - Y 좌표 범위 초과
        var invalidYResult = Coordinate.Create(100, -50);
        invalidYResult.Match(
            Succ: c => Console.WriteLine($"   ✅ 성공: {c}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 동등성 비교
        var coord1 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        var coord2 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        Console.WriteLine($"   📊 동등성: {coord1} == {coord2} = {coord1 == coord2}\n");
    }

    /// <summary>
    /// 4. 비교 가능한 복합 primitive 값 객체 - DateRange 시연
    /// ComparableValueObject 기반으로 2개 DateTime 조합
    /// </summary>
    static void DemonstrateComparableCompositePrimitiveValueObject()
    {
        Console.WriteLine("4. 비교 가능한 복합 primitive 값 객체 - DateRange (날짜 범위)");
        Console.WriteLine("   ComparableValueObject 기반으로 2개 DateTime 조합\n");

        // 성공 케이스
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var dateRangeResult = DateRange.Create(startDate, endDate);
        dateRangeResult.Match(
            Succ: dr => Console.WriteLine($"   ✅ 성공: {dr} (시작: {dr.StartDate:yyyy-MM-dd}, 종료: {dr.EndDate:yyyy-MM-dd})"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - 시작일이 종료일보다 늦음
        var invalidRangeResult = DateRange.Create(endDate, startDate);
        invalidRangeResult.Match(
            Succ: dr => Console.WriteLine($"   ✅ 성공: {dr}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - 같은 날짜
        var sameDateResult = DateRange.Create(startDate, startDate);
        sameDateResult.Match(
            Succ: dr => Console.WriteLine($"   ✅ 성공: {dr}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 비교 가능성 데모
        var range1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31)).IfFail(_ => throw new Exception("생성 실패"));
        var range3 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).IfFail(_ => throw new Exception("생성 실패"));

        Console.WriteLine($"   📊 비교: {range1} < {range2} = {range1 < range2}");
        Console.WriteLine($"   📊 비교: {range1} == {range3} = {range1 == range3}");
        Console.WriteLine($"   📊 비교: {range1} > {range2} = {range1 > range2}\n");
    }

    /// <summary>
    /// 5. 비교 불가능한 복합 값 객체 - Address 시연
    /// ValueObject 기반으로 3개 값 객체 조합
    /// </summary>
    static void DemonstrateComparableNotCompositeValueObject()
    {
        Console.WriteLine("5. 비교 불가능한 복합 값 객체 - Address (Street, City, PostalCode)");
        Console.WriteLine("   ValueObject 기반으로 3개 값 객체 조합\n");

        // 성공 케이스
        var addressResult = Address.Create("123 Main St", "Seoul", "12345");
        addressResult.Match(
            Succ: addr => Console.WriteLine($"   ✅ 성공: {addr}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - 거리명이 빈 경우
        var emptyStreetResult = Address.Create("", "Seoul", "12345");
        emptyStreetResult.Match(
            Succ: addr => Console.WriteLine($"   ✅ 성공: {addr}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스 - 우편번호 형식 오류
        var invalidPostalResult = Address.Create("123 Main St", "Seoul", "abc123");
        invalidPostalResult.Match(
            Succ: addr => Console.WriteLine($"   ✅ 성공: {addr}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 개별 값 객체 생성 데모
        Console.WriteLine("\n   📋 개별 값 객체 생성:");
        var street = Street.Create("Broadway").IfFail(_ => throw new Exception("생성 실패"));
        var city = City.Create("New York").IfFail(_ => throw new Exception("생성 실패"));
        var postalCode = PostalCode.Create("10001").IfFail(_ => throw new Exception("생성 실패"));

        Console.WriteLine($"   - Street: {street} (값: {(string)street})");
        Console.WriteLine($"   - City: {city} (값: {(string)city})");
        Console.WriteLine($"   - PostalCode: {postalCode} (값: {(string)postalCode})");

        // CreateFromValidated 데모
        var addressFromValidated = Address.CreateFromValidated(street, city, postalCode);
        Console.WriteLine($"   - Address from validated: {addressFromValidated}\n");
    }

    /// <summary>
    /// 6. 비교 가능한 복합 값 객체 - PriceRange 시연
    /// ComparableValueObject 기반으로 Price, Currency 값 객체 조합
    /// </summary>
    static void DemonstrateComparableCompositeValueObject()
    {
        Console.WriteLine("6. 비교 가능한 복합 값 객체 - PriceRange (가격 범위)");
        Console.WriteLine("   ComparableValueObject 기반으로 Price, Currency 값 객체 조합\n");

        // 성공 케이스
        var priceRange1 = PriceRange.Create(10000, 50000, "KRW");
        priceRange1.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range} (최소: {range.MinPrice}, 최대: {range.MaxPrice}, 통화: {range.MinPrice.Currency.GetCode()})"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        // 실패 케이스들
        var priceRange2 = PriceRange.Create(-1000, 50000, "KRW");
        priceRange2.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange3 = PriceRange.Create(10000, -5000, "KRW");
        priceRange3.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange4 = PriceRange.Create(50000, 10000, "KRW");
        priceRange4.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

        var priceRange5 = PriceRange.Create(10000, 50000, "INVALID");
        priceRange5.Match(
            Succ: range => Console.WriteLine($"   ✅ 성공: {range}"),
            Fail: error => Console.WriteLine($"   ❌ 실패: {error.Message}")
        );

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
        Console.WriteLine($"   - Currency: {currency} (코드: {currency.GetCode()})");

        // CreateFromValidated 데모
        var priceRangeFromValidated = PriceRange.CreateFromValidated(minPrice, maxPrice);
        Console.WriteLine($"   - PriceRange from validated: {priceRangeFromValidated}\n");
    }

}