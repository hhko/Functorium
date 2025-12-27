/// <summary>
/// Currency 클래스의 SmartEnum 기반 통화 테스트
/// 
/// 테스트 목적:
/// 1. SmartEnum 패키지 활용 검증
/// 2. 기존 값 객체 패턴과의 통합 검증
/// 3. LINQ Expression을 활용한 함수형 체이닝 검증
/// </summary>
[Trait("Concept-12-Type-Safe-Enums", "CurrencyTests")]
public class CurrencyTests
{
    // 테스트 시나리오: 유효한 통화 코드로 Currency 인스턴스를 생성할 수 있어야 한다
    [Fact]
    public void Create_ShouldReturnSuccessResult_WhenValidCurrencyCode()
    {
        // Arrange
        string currencyCode = "KRW";
        Currency expected = Currency.KRW;

        // Act
        var actual = Currency.Create(currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency => currency.ShouldBe(expected));
    }

    // 테스트 시나리오: 지원되는 모든 통화 코드로 Currency 인스턴스를 생성할 수 있어야 한다
    [Theory]
    [InlineData("KRW", "한국 원화", "₩")]
    [InlineData("USD", "미국 달러", "$")]
    [InlineData("EUR", "유로", "€")]
    [InlineData("JPY", "일본 엔", "¥")]
    [InlineData("CNY", "중국 위안", "¥")]
    [InlineData("GBP", "영국 파운드", "£")]
    [InlineData("AUD", "호주 달러", "A$")]
    [InlineData("CAD", "캐나다 달러", "C$")]
    [InlineData("CHF", "스위스 프랑", "CHF")]
    [InlineData("SGD", "싱가포르 달러", "S$")]
    public void Create_ShouldReturnSuccessResult_WhenSupportedCurrencyCode(string currencyCode, string expectedKoreanName, string expectedSymbol)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Create(currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency =>
        {
            currency.KoreanName.ShouldBe(expectedKoreanName);
            currency.Symbol.ShouldBe(expectedSymbol);
            currency.GetCode().ShouldBe(currencyCode);
        });
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
        actual.IfFail(error => error.Message.ShouldContain("통화 코드는 비어있을 수 없습니다"));
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
        actual.IfFail(error => error.Message.ShouldContain("통화 코드는 비어있을 수 없습니다"));
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
        actual.IfFail(error => error.Message.ShouldContain("통화 코드는 3자리 영문자여야 합니다"));
    }

    // 테스트 시나리오: 지원하지 않는 3자리 통화 코드로 Currency 생성 시 실패해야 한다
    [Theory]
    [InlineData("abc")]
    [InlineData("XYZ")]
    [InlineData("ABC")]
    public void Create_ShouldReturnFailureResult_WhenUnsupportedCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain($"지원하지 않는 통화 코드입니다: {currencyCode}"));
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
        actual.IfSucc(currency => currency.GetCode().ShouldBe(expectedCode));
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
        actual.GetCode().ShouldBe(currencyCode);
    }

    // 테스트 시나리오: GetAllSupportedCurrencies 메서드가 모든 지원 통화를 반환해야 한다
    [Fact]
    public void GetAllSupportedCurrencies_ShouldReturnAllSupportedCurrencies_WhenCalled()
    {
        // Arrange
        var expectedCount = 10; // KRW, USD, EUR, JPY, CNY, GBP, AUD, CAD, CHF, SGD

        // Act
        var actual = Currency.GetAllSupportedCurrencies();

        // Assert
        actual.ShouldNotBeNull();
        actual.Count().ShouldBe(expectedCount);
        actual.ShouldContain(Currency.KRW);
        actual.ShouldContain(Currency.USD);
        actual.ShouldContain(Currency.EUR);
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 통화 정보를 반환해야 한다
    [Theory]
    [InlineData("KRW", "KRW (한국 원화) ₩")]
    [InlineData("USD", "USD (미국 달러) $")]
    [InlineData("EUR", "EUR (유로) €")]
    public void ToString_ShouldReturnFormattedCurrencyInfo_WhenCalled(string currencyCode, string expectedFormat)
    {
        // Arrange
        var currency = Currency.CreateFromValidated(currencyCode);

        // Act
        var actual = currency.ToString();

        // Assert
        actual.ShouldBe(expectedFormat);
    }

    // 테스트 시나리오: FormatAmount 메서드가 올바른 형식으로 금액을 포맷팅해야 한다
    [Fact]
    public void FormatAmount_ShouldReturnFormattedAmount_WhenValidAmount()
    {
        // Arrange
        var testCases = new[]
        {
            new { CurrencyCode = "KRW", Amount = 12345.67m, ExpectedFormat = "₩12,345.67" },
            new { CurrencyCode = "USD", Amount = 123.45m, ExpectedFormat = "$123.45" },
            new { CurrencyCode = "EUR", Amount = 89.12m, ExpectedFormat = "€89.12" }
        };

        foreach (var testCase in testCases)
        {
            var currency = Currency.CreateFromValidated(testCase.CurrencyCode);

            // Act
            var actual = currency.FormatAmount(testCase.Amount);

            // Assert
            actual.ShouldBe(testCase.ExpectedFormat);
        }
    }

    // 테스트 시나리오: FormatAmountWithoutDecimals 메서드가 소수점 없이 금액을 포맷팅해야 한다
    [Fact]
    public void FormatAmountWithoutDecimals_ShouldReturnFormattedAmountWithoutDecimals_WhenValidAmount()
    {
        // Arrange
        var testCases = new[]
        {
            new { CurrencyCode = "KRW", Amount = 12345.67m, ExpectedFormat = "₩12,346" },
            new { CurrencyCode = "USD", Amount = 123.45m, ExpectedFormat = "$123" },
            new { CurrencyCode = "EUR", Amount = 89.12m, ExpectedFormat = "€89" }
        };

        foreach (var testCase in testCases)
        {
            var currency = Currency.CreateFromValidated(testCase.CurrencyCode);

            // Act
            var actual = currency.FormatAmountWithoutDecimals(testCase.Amount);

            // Assert
            actual.ShouldBe(testCase.ExpectedFormat);
        }
    }

    // 테스트 시나리오: SmartEnum의 정적 필드들이 올바르게 정의되어야 한다
    [Fact]
    public void StaticFields_ShouldBeCorrectlyDefined_WhenAccessed()
    {
        // Arrange
        // (정적 필드 직접 접근)

        // Act & Assert
        Currency.KRW.KoreanName.ShouldBe("한국 원화");
        Currency.KRW.Symbol.ShouldBe("₩");
        Currency.KRW.GetCode().ShouldBe("KRW");

        Currency.USD.KoreanName.ShouldBe("미국 달러");
        Currency.USD.Symbol.ShouldBe("$");
        Currency.USD.GetCode().ShouldBe("USD");

        Currency.EUR.KoreanName.ShouldBe("유로");
        Currency.EUR.Symbol.ShouldBe("€");
        Currency.EUR.GetCode().ShouldBe("EUR");
    }

    // 테스트 시나리오: SmartEnum의 FromValue 메서드가 올바르게 작동해야 한다
    [Theory]
    [InlineData("KRW", "KRW")]
    [InlineData("USD", "USD")]
    [InlineData("EUR", "EUR")]
    public void FromValue_ShouldReturnCorrectCurrency_WhenValidValue(string value, string expectedCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.FromValue(value);

        // Assert
        actual.ShouldNotBeNull();
        actual.GetCode().ShouldBe(expectedCode);
    }

    // 테스트 시나리오: SmartEnum의 TryFromValue 메서드가 올바르게 작동해야 한다
    [Theory]
    [InlineData("KRW", true)]
    [InlineData("USD", true)]
    [InlineData("INVALID", false)]
    public void TryFromValue_ShouldReturnCorrectResult_WhenVariousValues(string value, bool expectedSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Currency.TryFromValue(value, out var currency);

        // Assert
        actual.ShouldBe(expectedSuccess);
        if (expectedSuccess)
        {
            currency.ShouldNotBeNull();
            currency.GetCode().ShouldBe(value);
        }
        else
        {
            currency.ShouldBeNull();
        }
    }
}
