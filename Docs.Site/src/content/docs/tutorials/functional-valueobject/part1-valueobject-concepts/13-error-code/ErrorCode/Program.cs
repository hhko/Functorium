using Framework.Abstractions.Errors;
using LanguageExt.Common;
using ErrorCode.ValueObjects.Comparable.CompositeValueObjects;
using ErrorCode.ValueObjects.ComparableNot.CompositeValueObjects;
using ErrorCode.ValueObjects.Comparable.PrimitiveValueObjects;
using ErrorCode.ValueObjects.ComparableNot.PrimitiveValueObjects;
using ErrorCode.ValueObjects.Comparable.CompositePrimitiveValueObjects;
using ErrorCode.ValueObjects.ComparableNot.CompositePrimitiveValueObjects;

namespace ErrorCode;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== 체계적인 에러 처리 패턴 ===\n");

        Console.WriteLine("=== Comparable 테스트 ===");
        
        // 1. CompositeValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- CompositeValueObjects 하위 폴더 ---");
        DemonstrateComparableCompositeValueObjects();
        
        // 2. PrimitiveValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- PrimitiveValueObjects 하위 폴더 ---");
        DemonstrateComparablePrimitiveValueObjects();
        
        // 3. CompositePrimitiveValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- CompositePrimitiveValueObjects 하위 폴더 ---");
        DemonstrateComparableCompositePrimitiveValueObjects();
      
        Console.WriteLine("\n=== ComparableNot 폴더 테스트 ===");
        
        // 4. CompositeValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- CompositeValueObjects 하위 폴더 ---");
        DemonstrateComparableNotCompositeValueObjects();
        
        // 5. PrimitiveValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- PrimitiveValueObjects 하위 폴더 ---");
        DemonstrateComparableNotPrimitiveValueObjects();
        
        // 6. CompositePrimitiveValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- CompositePrimitiveValueObjects 하위 폴더 ---");
        DemonstrateComparableNotCompositePrimitiveValueObjects();
    }

    static string PrintError(Error error)
    {
        // InternalsVisibleTo를 통해 ErrorCodeExpected 클래스에 직접 접근
        return error switch
        {
            ErrorCodeExpected errorCodeExpected => 
                $"ErrorCode: {errorCodeExpected.ErrorCode}, ErrorCurrentValue: {errorCodeExpected.ErrorCurrentValue}",
            
            ErrorCodeExpected<string> errorCodeExpectedString => 
                $"ErrorCode: {errorCodeExpectedString.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedString.ErrorCurrentValue}",
            
            ErrorCodeExpected<int> errorCodeExpectedInt => 
                $"ErrorCode: {errorCodeExpectedInt.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedInt.ErrorCurrentValue}",
            
            ErrorCodeExpected<decimal> errorCodeExpectedDecimal => 
                $"ErrorCode: {errorCodeExpectedDecimal.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedDecimal.ErrorCurrentValue}",
            
            ErrorCodeExpected<DateTime> errorCodeExpectedDateTime => 
                $"ErrorCode: {errorCodeExpectedDateTime.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedDateTime.ErrorCurrentValue}",
            
            ErrorCodeExpected<byte[]> errorCodeExpectedByteArray => 
                $"ErrorCode: {errorCodeExpectedByteArray.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedByteArray.ErrorCurrentValue?.Length.ToString() ?? "null"}",
            
            ErrorCodeExpected<Price, Price> errorCodeExpectedPriceRange => 
                $"ErrorCode: {errorCodeExpectedPriceRange.ErrorCode}, ErrorCurrentValue: MinPrice: {errorCodeExpectedPriceRange.ErrorCurrentValue1}, MaxPrice: {errorCodeExpectedPriceRange.ErrorCurrentValue2}",
            
            ErrorCodeExpected<DateTime, DateTime> errorCodeExpectedDateRange => 
                $"ErrorCode: {errorCodeExpectedDateRange.ErrorCode}, ErrorCurrentValue: StartDate: {errorCodeExpectedDateRange.ErrorCurrentValue1}, EndDate: {errorCodeExpectedDateRange.ErrorCurrentValue2}",
            
            _ => $"Message: {error.Message}"
        };
    }

    static void DemonstrateComparableCompositeValueObjects()
    {
        Console.WriteLine("  === CompositeValueObjects 에러 테스트 ===");
        
        // Currency 에러 테스트
        Console.WriteLine("\n  --- Currency 에러 테스트 ---");
        DemonstrateCurrencyErrors();
        
        // Price 에러 테스트
        Console.WriteLine("\n  --- Price 에러 테스트 ---");
        DemonstratePriceErrors();
        
        // PriceRange 에러 테스트
        Console.WriteLine("\n  --- PriceRange 에러 테스트 ---");
        DemonstratePriceRangeErrors();
    }

    static void DemonstrateCurrencyErrors()
    {
        // 빈 통화 코드
        var emptyResult = Currency.Create("");
        emptyResult.IfFail(error => Console.WriteLine($"빈 통화 코드: {PrintError(error)}"));

        // 3자리가 아닌 형식
        var invalidFormatResult = Currency.Create("AB");
        invalidFormatResult.IfFail(error => Console.WriteLine($"3자리가 아닌 형식: {PrintError(error)}"));

        // 지원하지 않는 통화
        var unsupportedResult = Currency.Create("XYZ");
        unsupportedResult.IfFail(error => Console.WriteLine($"지원하지 않는 통화: {PrintError(error)}"));
    }

    static void DemonstratePriceErrors()
    {
        // 음수 가격
        var negativeResult = Price.Create(-100, "KRW");
        negativeResult.IfFail(error => Console.WriteLine($"음수 가격: {PrintError(error)}"));
    }

    static void DemonstratePriceRangeErrors()
    {
        // 최솟값이 최댓값을 초과하는 가격 범위
        var invalidRangeResult = PriceRange.Create(1000, 500, "KRW");
        invalidRangeResult.IfFail(error => Console.WriteLine($"최솟값이 최댓값을 초과하는 가격 범위: {PrintError(error)}"));
    }

    static void DemonstrateComparableNotCompositeValueObjects()
    {
        Console.WriteLine("  === CompositeValueObjects 에러 테스트 ===");
        
        // Address 에러 테스트
        Console.WriteLine("\n  --- Address 에러 테스트 ---");
        DemonstrateAddressErrors();
        
        // Street 에러 테스트
        Console.WriteLine("\n  --- Street 에러 테스트 ---");
        DemonstrateStreetErrors();
        
        // City 에러 테스트
        Console.WriteLine("\n  --- City 에러 테스트 ---");
        DemonstrateCityErrors();
        
        // PostalCode 에러 테스트
        Console.WriteLine("\n  --- PostalCode 에러 테스트 ---");
        DemonstratePostalCodeErrors();
    }

    static void DemonstrateAddressErrors()
    {
        // 빈 거리명으로 주소 생성
        var emptyStreetResult = Address.Create("", "서울시", "12345");
        emptyStreetResult.IfFail(error => Console.WriteLine($"빈 거리명: {PrintError(error)}"));

        // 빈 도시명으로 주소 생성
        var emptyCityResult = Address.Create("강남대로", "", "12345");
        emptyCityResult.IfFail(error => Console.WriteLine($"빈 도시명: {PrintError(error)}"));

        // 잘못된 우편번호로 주소 생성
        var invalidPostalResult = Address.Create("강남대로", "서울시", "1234");
        invalidPostalResult.IfFail(error => Console.WriteLine($"잘못된 우편번호: {PrintError(error)}"));
    }

    static void DemonstrateStreetErrors()
    {
        var emptyResult = Street.Create("");
        emptyResult.IfFail(error => Console.WriteLine($"빈 거리명: {PrintError(error)}"));
    }

    static void DemonstrateCityErrors()
    {
        var emptyResult = City.Create("");
        emptyResult.IfFail(error => Console.WriteLine($"빈 도시명: {PrintError(error)}"));
    }

    static void DemonstratePostalCodeErrors()
    {
        // 빈 우편번호
        var emptyResult = PostalCode.Create("");
        emptyResult.IfFail(error => Console.WriteLine($"빈 우편번호: {PrintError(error)}"));

        // 5자리 숫자가 아닌 형식
        var invalidFormatResult = PostalCode.Create("1234");
        invalidFormatResult.IfFail(error => Console.WriteLine($"5자리 숫자가 아닌 형식: {PrintError(error)}"));
    }

    static void DemonstrateComparablePrimitiveValueObjects()
    {
        Console.WriteLine("  === PrimitiveValueObjects 에러 테스트 ===");
        
        // Denominator 에러 테스트
        Console.WriteLine("\n  --- Denominator 에러 테스트 ---");
        DemonstrateDenominatorErrors();
    }

    static void DemonstrateComparableNotPrimitiveValueObjects()
    {
        Console.WriteLine("  === PrimitiveValueObjects 에러 테스트 ===");
        
        // BinaryData 에러 테스트
        Console.WriteLine("\n  --- BinaryData 에러 테스트 ---");
        DemonstrateBinaryDataErrors();
    }

    static void DemonstrateDenominatorErrors()
    {
        // 0 값
        var zeroResult = Denominator.Create(0);
        zeroResult.IfFail(error => Console.WriteLine($"0 값: {PrintError(error)}"));
    }

    static void DemonstrateBinaryDataErrors()
    {
        // null 바이너리 데이터
        var nullResult = BinaryData.Create(null!);
        nullResult.IfFail(error => Console.WriteLine($"null 바이너리 데이터: {PrintError(error)}"));

        // 빈 바이너리 데이터
        var emptyResult = BinaryData.Create(new byte[0]);
        emptyResult.IfFail(error => Console.WriteLine($"빈 바이너리 데이터: {PrintError(error)}"));
    }

    static void DemonstrateComparableCompositePrimitiveValueObjects()
    {
        Console.WriteLine("  === CompositePrimitiveValueObjects 에러 테스트 ===");
        
        // DateRange 에러 테스트
        Console.WriteLine("\n  --- DateRange 에러 테스트 ---");
        DemonstrateDateRangeErrors();
    }

    static void DemonstrateComparableNotCompositePrimitiveValueObjects()
    {
        Console.WriteLine("  === CompositePrimitiveValueObjects 에러 테스트 ===");
        
        // Coordinate 에러 테스트
        Console.WriteLine("\n  --- Coordinate 에러 테스트 ---");
        DemonstrateCoordinateErrors();
    }

    static void DemonstrateDateRangeErrors()
    {
        // 시작일이 종료일 이후인 날짜 범위
        var invalidRangeResult = DateRange.Create(
            new DateTime(2024, 12, 31), 
            new DateTime(2024, 1, 1));
        invalidRangeResult.IfFail(error => Console.WriteLine($"시작일이 종료일 이후인 날짜 범위: {PrintError(error)}"));
    }

    static void DemonstrateCoordinateErrors()
    {
        // 범위를 벗어난 X 좌표
        var invalidXResult = Coordinate.Create(-1, 500);
        invalidXResult.IfFail(error => Console.WriteLine($"범위를 벗어난 X 좌표: {PrintError(error)}"));

        // 범위를 벗어난 Y 좌표
        var invalidYResult = Coordinate.Create(500, 1001);
        invalidYResult.IfFail(error => Console.WriteLine($"범위를 벗어난 Y 좌표: {PrintError(error)}"));
    }
}
