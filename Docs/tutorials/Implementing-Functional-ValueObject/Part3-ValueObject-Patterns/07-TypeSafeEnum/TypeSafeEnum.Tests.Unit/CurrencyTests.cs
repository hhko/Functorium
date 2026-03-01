using TypeSafeEnum.ValueObjects;
using LanguageExt;

namespace TypeSafeEnum.Tests.Unit;

/// <summary>
/// Currency 값 객체의 SmartEnum (TypeSafeEnum) 패턴 테스트
///
/// 학습 목표:
/// 1. SmartEnum 기반 타입 안전 열거형 패턴 이해
/// 2. 정적 인스턴스 사용과 Create 메서드 비교
/// 3. SmartEnum이 제공하는 비교 기능 검증
/// </summary>
[Trait("Part3-Patterns", "07-TypeSafeEnum")]
public class CurrencyTests
{
    // 테스트 시나리오: 유효한 통화 코드로 Currency 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenCurrencyCodeIsValid()
    {
        // Arrange
        string currencyCode = "KRW";

        // Act
        Fin<Currency> actual = Currency.Create(currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: currency =>
            {
                currency.Value.ShouldBe(currencyCode);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 빈 통화 코드로 실패
    [Fact]
    public void Create_ReturnsFail_WhenCurrencyCodeIsEmpty()
    {
        // Arrange
        string currencyCode = "";

        // Act
        Fin<Currency> actual = Currency.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 3자리가 아닌 통화 코드로 실패
    [Fact]
    public void Create_ReturnsFail_WhenCurrencyCodeIsNotThreeLetters()
    {
        // Arrange
        string currencyCode = "US";

        // Act
        Fin<Currency> actual = Currency.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 지원하지 않는 통화 코드로 실패
    [Fact]
    public void Create_ReturnsFail_WhenCurrencyCodeIsUnsupported()
    {
        // Arrange
        string currencyCode = "XYZ";

        // Act
        Fin<Currency> actual = Currency.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 정적 인스턴스 접근
    [Fact]
    public void StaticInstance_ReturnsCorrectCurrency_WhenAccessingKRW()
    {
        // Act
        Currency actual = Currency.KRW;

        // Assert
        actual.Value.ShouldBe("KRW");
        actual.KoreanName.ShouldBe("한국 원화");
        actual.Symbol.ShouldBe("₩");
    }

    // 테스트 시나리오: 소문자 통화 코드도 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenCurrencyCodeIsLowercase()
    {
        // Arrange
        string currencyCode = "usd";

        // Act
        Fin<Currency> actual = Currency.Create(currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 통화 동등성 비교
    [Fact]
    public void Equals_ReturnsTrue_WhenCurrenciesAreEqual()
    {
        // Arrange
        var currency1 = Currency.Create("USD").Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        currency1.Equals(Currency.USD).ShouldBeTrue();
    }

    // 테스트 시나리오: FormatAmount 메서드 검증
    [Fact]
    public void FormatAmount_ReturnsFormattedString_WhenAmountIsProvided()
    {
        // Arrange
        var currency = Currency.USD;
        decimal amount = 1234.56m;

        // Act
        string actual = currency.FormatAmount(amount);

        // Assert
        actual.ShouldStartWith("$");
    }

    // 테스트 시나리오: 모든 지원 통화 목록 조회
    [Fact]
    public void GetAllSupportedCurrencies_ReturnsAllCurrencies_WhenCalled()
    {
        // Act
        var actual = Currency.GetAllSupportedCurrencies().ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldContain(Currency.KRW);
        actual.ShouldContain(Currency.USD);
        actual.ShouldContain(Currency.EUR);
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string currencyCode = "EUR";

        // Act
        Fin<Currency> actual1 = Currency.Create(currencyCode);
        Fin<Currency> actual2 = Currency.Create(currencyCode);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: GetCode 메서드 검증
    [Fact]
    public void GetCode_ReturnsCorrectCode_WhenCurrencyIsCreated()
    {
        // Arrange
        var currency = Currency.JPY;

        // Act
        string actual = currency.GetCode();

        // Assert
        actual.ShouldBe("JPY");
    }
}
