using TypeSafeEnum.ValueObjects;
using LanguageExt.Common;

namespace TypeSafeEnum;

/// <summary>
/// Type Safe Enum 데모 프로그램
/// SmartEnum을 사용한 타입 안전한 열거형 구현 예제
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Type Safe Enum 데모 ===\n");

        DemonstrateBasicUsage();
        DemonstrateValidation();
        DemonstrateComparison();
        DemonstrateBusinessLogic();
        DemonstrateErrorHandling();
        DemonstrateAllCurrencies();

        Console.WriteLine("\n=== 데모 완료 ===");
    }

    /// <summary>
    /// 기본 사용법 데모
    /// </summary>
    static void DemonstrateBasicUsage()
    {
        Console.WriteLine("1. 기본 사용법");
        Console.WriteLine("================");

        // 정적 인스턴스 직접 사용
        var krw = Currency.KRW;
        var usd = Currency.USD;
        var eur = Currency.EUR;

        Console.WriteLine($"KRW: {krw}");
        Console.WriteLine($"USD: {usd}");
        Console.WriteLine($"EUR: {eur}");

        // FromValue 메서드 사용
        var jpy = Currency.FromValue("JPY");
        Console.WriteLine($"JPY from value: {jpy}");

        // FromName 메서드 사용
        var gbp = Currency.FromName("GBP");
        Console.WriteLine($"GBP from name: {gbp}");

        Console.WriteLine();
    }

    /// <summary>
    /// 검증 기능 데모
    /// </summary>
    static void DemonstrateValidation()
    {
        Console.WriteLine("2. 검증 기능");
        Console.WriteLine("=============");

        // 유효한 통화 코드
        var validResult = Currency.Create("USD");
        validResult.Match(
            Succ: currency => Console.WriteLine($"✅ 유효한 통화: {currency}"),
            Fail: error => Console.WriteLine($"❌ 에러: {error.Message}")
        );

        // 잘못된 형식의 통화 코드
        var invalidFormatResult = Currency.Create("US");
        invalidFormatResult.Match(
            Succ: currency => Console.WriteLine($"✅ 유효한 통화: {currency}"),
            Fail: error => Console.WriteLine($"❌ 에러: {GetErrorDetails(error)}")
        );

        // 지원하지 않는 통화 코드
        var unsupportedResult = Currency.Create("XYZ");
        unsupportedResult.Match(
            Succ: currency => Console.WriteLine($"✅ 유효한 통화: {currency}"),
            Fail: error => Console.WriteLine($"❌ 에러: {GetErrorDetails(error)}")
        );

        // 빈 통화 코드
        var emptyResult = Currency.Create("");
        emptyResult.Match(
            Succ: currency => Console.WriteLine($"✅ 유효한 통화: {currency}"),
            Fail: error => Console.WriteLine($"❌ 에러: {GetErrorDetails(error)}")
        );

        Console.WriteLine();
    }

    /// <summary>
    /// 비교 기능 데모
    /// </summary>
    static void DemonstrateComparison()
    {
        Console.WriteLine("3. 비교 기능");
        Console.WriteLine("=============");

        var krw = Currency.KRW;
        var usd = Currency.USD;
        var eur = Currency.EUR;

        // 동등성 비교
        Console.WriteLine($"KRW == KRW: {krw == Currency.KRW}");
        Console.WriteLine($"KRW == USD: {krw == usd}");

        // 비교 연산자
        Console.WriteLine($"KRW < USD: {krw.CompareTo(usd) < 0}");
        Console.WriteLine($"USD > EUR: {usd.CompareTo(eur) > 0}");

        // 해시 코드
        Console.WriteLine($"KRW HashCode: {krw.GetHashCode()}");
        Console.WriteLine($"USD HashCode: {usd.GetHashCode()}");

        Console.WriteLine();
    }

    /// <summary>
    /// 비즈니스 로직 데모
    /// </summary>
    static void DemonstrateBusinessLogic()
    {
        Console.WriteLine("4. 비즈니스 로직");
        Console.WriteLine("=================");

        var currencies = new[] { Currency.KRW, Currency.USD, Currency.EUR, Currency.JPY };

        foreach (var currency in currencies)
        {
            var amount = 1000m;
            Console.WriteLine($"{currency.GetCode()}: {currency.FormatAmount(amount)}");
            Console.WriteLine($"{currency.GetCode()}: {currency.FormatAmountWithoutDecimals(amount)}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 에러 처리 데모
    /// </summary>
    static void DemonstrateErrorHandling()
    {
        Console.WriteLine("5. 에러 처리");
        Console.WriteLine("=============");

        var testCodes = new[] { "USD", "INVALID", "KR", "XYZ", "EUR" };

        foreach (var code in testCodes)
        {
            var result = Currency.Create(code);
            result.Match(
                Succ: currency => Console.WriteLine($"✅ {code} → {currency}"),
                Fail: error => Console.WriteLine($"❌ {code} → {GetErrorDetails(error)}")
            );
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 모든 통화 목록 데모
    /// </summary>
    static void DemonstrateAllCurrencies()
    {
        Console.WriteLine("6. 모든 지원 통화");
        Console.WriteLine("==================");

        var allCurrencies = Currency.GetAllSupportedCurrencies();
        Console.WriteLine($"총 {allCurrencies.Count()}개 통화 지원:");

        foreach (var currency in allCurrencies)
        {
            Console.WriteLine($"  - {currency}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// ErrorCodeFactory 기반 에러의 상세 정보를 반환
    /// </summary>
    /// <param name="error">에러 객체</param>
    /// <returns>에러 상세 정보 문자열</returns>
    static string GetErrorDetails(Error error)
    {
        // ErrorCodeExpected 또는 ErrorCodeExpected<T> 타입인지 확인 (리플렉션 사용)
        var errorType = error.GetType();
        if (errorType.Name.StartsWith("ErrorCodeExpected"))
        {
            var errorCodeProperty = errorType.GetProperty("ErrorCode");
            var errorCurrentValueProperty = errorType.GetProperty("ErrorCurrentValue");

            if (errorCodeProperty != null && errorCurrentValueProperty != null)
            {
                var errorCode = errorCodeProperty.GetValue(error)?.ToString() ?? "Unknown";
                var errorCurrentValue = errorCurrentValueProperty.GetValue(error)?.ToString() ?? "Unknown";
                return $"코드: {errorCode}, 값: {errorCurrentValue}";
            }
        }

        // 기본 에러 메시지 반환
        return error.Message;
    }
}