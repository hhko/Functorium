using ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.Comparable.CompositeValueObjects;

/// <summary>
/// Currency 값 객체 테스트
/// ComparableSimpleValueObject<string> 기반으로 비교 가능한 primitive 값 객체 구현
/// 
/// 테스트 목적:
/// 1. 기본 값 객체 생성 및 검증 검증
/// 2. LINQ Expression을 활용한 함수형 체이닝 검증
/// 3. 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "CurrencyTests")]
public class CurrencyTests
{
    // 테스트 시나리오: 유효한 통화 코드로 Currency 인스턴스를 생성할 수 있어야 한다
    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("JPY")]
    [InlineData("GBP")]
    public void Create_ShouldReturnSuccessResult_WhenValidCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Create(currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency => ((string)currency).ShouldBe(currencyCode.ToUpperInvariant()));
    }

    // 테스트 시나리오: 소문자 통화 코드도 대문자로 변환되어 처리되어야 한다
    [Theory]
    [InlineData("krw", "KRW")]
    [InlineData("usd", "USD")]
    [InlineData("eur", "EUR")]
    public void Create_ShouldReturnSuccessResult_WhenLowerCaseCurrencyCode(string inputCode, string expectedCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Create(inputCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency => ((string)currency).ShouldBe(expectedCode));
    }

    // 테스트 시나리오: 빈 통화 코드로 Currency 생성 시 실패해야 한다
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnFailureResult_WhenEmptyCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe($"통화 코드는 비어있을 수 없습니다: {currencyCode}"));
    }

    // 테스트 시나리오: null 통화 코드로 Currency 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenNullCurrencyCode()
    {
        // Arrange
        string? currencyCode = null;

        // Act
        var actual = Currency.Create(currencyCode!);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("통화 코드는 비어있을 수 없습니다: "));
    }

    // 테스트 시나리오: 잘못된 형식의 통화 코드로 Currency 생성 시 실패해야 한다
    [Theory]
    [InlineData("KR")]
    [InlineData("KOREA")]
    [InlineData("123")]
    [InlineData("KR1")]
    [InlineData("K-R")]
    public void Create_ShouldReturnFailureResult_WhenInvalidFormatCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => 
        {
            error.Message.ShouldBe($"통화 코드는 3자리 영문자여야 합니다: {currencyCode}");
        });
    }

    // 테스트 시나리오: Validate 메서드가 올바른 검증 결과를 반환해야 한다
    [Theory]
    [InlineData("KRW", true)]
    [InlineData("USD", true)]
    [InlineData("INVALID", false)]
    [InlineData("", false)]
    [InlineData("KR", false)]
    public void Validate_ShouldReturnCorrectValidationResult_WhenVariousCurrencyCodes(string currencyCode, bool expectedIsSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Validate(currencyCode);

        // Assert
        actual.Match(
            Succ: _ => expectedIsSuccess.ShouldBeTrue(),
            Fail: _ => expectedIsSuccess.ShouldBeFalse()
        );
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 코드로 Currency를 생성해야 한다
    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void CreateFromValidated_ShouldReturnCurrencyInstance_WhenValidatedCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.CreateFromValidated(currencyCode);

        // Assert
        actual.ShouldNotBeNull();
        ((string)actual).ShouldBe(currencyCode);
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 통화 정보를 반환해야 한다
    [Theory]
    [InlineData("KRW", "KRW")]
    [InlineData("USD", "USD")]
    [InlineData("EUR", "EUR")]
    public void ToString_ShouldReturnFormattedCurrencyInfo_WhenCalled(string currencyCode, string expectedFormat)
    {
        // Arrange
        var currency = Currency.CreateFromValidated(currencyCode);

        // Act
        var actual = currency.ToString();

        // Assert
        actual.ShouldBe(GetExpectedCurrencyFormat(expectedFormat));
    }

    // 테스트 시나리오: Currency 인스턴스들이 올바르게 비교되어야 한다
    [Theory]
    [InlineData("EUR", "USD", -1)] // EUR < USD
    [InlineData("USD", "EUR", 1)]  // USD > EUR
    [InlineData("KRW", "KRW", 0)]  // KRW == KRW
    public void CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingCurrencies(string currencyCode1, string currencyCode2, int expectedComparison)
    {
        // Arrange
        var currency1 = Currency.CreateFromValidated(currencyCode1);
        var currency2 = Currency.CreateFromValidated(currencyCode2);

        // Act
        var actual = currency1.CompareTo(currency2);

        // Assert
        actual.ShouldBe(expectedComparison);
    }

    // 테스트 시나리오: Currency 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Theory]
    [InlineData("KRW", "KRW", true)]
    [InlineData("KRW", "USD", false)]
    [InlineData("USD", "USD", true)]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenComparingCurrencies(string currencyCode1, string currencyCode2, bool expectedEquality)
    {
        // Arrange
        var currency1 = Currency.CreateFromValidated(currencyCode1);
        var currency2 = Currency.CreateFromValidated(currencyCode2);

        // Act
        var actual = currency1.Equals(currency2);

        // Assert
        actual.ShouldBe(expectedEquality);
    }

    // 테스트 시나리오: Currency 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Theory]
    [InlineData("EUR", "USD", true, false, false, true, false, true)]  // EUR < USD
    [InlineData("USD", "EUR", false, true, false, false, true, true)]  // USD > EUR
    [InlineData("KRW", "KRW", false, false, true, true, true, false)]  // KRW == KRW
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenComparingCurrencies(
        string currencyCode1, string currencyCode2, 
        bool expectedLessThan, bool expectedGreaterThan, bool expectedEqual, 
        bool expectedLessThanOrEqual, bool expectedGreaterThanOrEqual, bool expectedNotEqual)
    {
        // Arrange
        var currency1 = Currency.CreateFromValidated(currencyCode1);
        var currency2 = Currency.CreateFromValidated(currencyCode2);

        // Act & Assert
        (currency1 < currency2).ShouldBe(expectedLessThan);
        (currency1 > currency2).ShouldBe(expectedGreaterThan);
        (currency1 == currency2).ShouldBe(expectedEqual);
        (currency1 <= currency2).ShouldBe(expectedLessThanOrEqual);
        (currency1 >= currency2).ShouldBe(expectedGreaterThanOrEqual);
        (currency1 != currency2).ShouldBe(expectedNotEqual);
    }

    // 테스트 시나리오: Currency 인스턴스가 null과 비교될 때 올바르게 처리되어야 한다
    [Fact]
    public void ComparisonWithNull_ShouldReturnCorrectResults_WhenComparingWithNull()
    {
        // Arrange
        var currency = Currency.CreateFromValidated("KRW");

        // Act & Assert
        (currency == null).ShouldBeFalse();
        (currency != null).ShouldBeTrue();
        (null == currency).ShouldBeFalse();
        (null != currency).ShouldBeTrue();
    }

    // 테스트 시나리오: Currency 인스턴스가 다른 타입과 비교될 때 올바르게 처리되어야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        var currency = Currency.CreateFromValidated("KRW");
        var otherObject = "not a currency";

        // Act
        var actual = currency.Equals(otherObject);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: Currency 인스턴스가 올바르게 해시 코드를 생성해야 한다
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenSameCurrency()
    {
        // Arrange
        var currency1 = Currency.CreateFromValidated("KRW");
        var currency2 = Currency.CreateFromValidated("KRW");

        // Act
        var actual1 = currency1.GetHashCode();
        var actual2 = currency2.GetHashCode();

        // Assert
        actual1.ShouldBe(actual2);
    }

    // 테스트 시나리오: Currency 인스턴스가 string으로 명시적 변환되어야 한다
    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void ExplicitConversion_ShouldConvertToString_WhenCurrencyInstance(string currencyCode)
    {
        // Arrange
        var currency = Currency.CreateFromValidated(currencyCode);

        // Act
        var actual = (string)currency;

        // Assert
        actual.ShouldBe(currencyCode);
    }

    // 테스트 시나리오: LINQ Expression을 활용한 검증 파이프라인이 올바르게 동작해야 한다
    [Theory]
    [InlineData("KRW", true)]
    [InlineData("USD", true)]
    [InlineData("", false)]
    [InlineData("KR", false)]
    [InlineData("US1", false)]
    public void ValidationPipeline_ShouldWorkCorrectly_WhenUsingLINQExpression(string currencyCode, bool expectedSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var validationResult = Currency.Validate(currencyCode);

        // Assert
        validationResult.Match(
            Succ: _ => expectedSuccess.ShouldBeTrue(),
            Fail: _ => expectedSuccess.ShouldBeFalse()
        );
    }

    /// <summary>
    /// 통화 코드에 따른 예상 형식을 반환하는 헬퍼 메서드
    /// </summary>
    /// <param name="currencyCode">통화 코드</param>
    /// <returns>예상 형식</returns>
    private static string GetExpectedCurrencyFormat(string currencyCode) => currencyCode switch
    {
        "USD" => "USD (미국 달러) $",
        "KRW" => "KRW (한국 원화) ₩",
        "EUR" => "EUR (유로) €",
        _ => currencyCode
    };
}