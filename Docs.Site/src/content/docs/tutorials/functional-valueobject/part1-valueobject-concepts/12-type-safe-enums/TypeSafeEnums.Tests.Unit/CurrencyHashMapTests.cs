/// <summary>
/// CurrencyHashMap 클래스의 SimpleValueObject + HashMap 패턴 통화 테스트
///
/// 테스트 목적:
/// 1. SmartEnum 없이 SimpleValueObject&lt;string&gt; + HashMap 패턴 검증
/// 2. Create/Validate/CreateFromValidated 3메서드 계약 검증
/// 3. 기존 SmartEnum 패턴과 동일한 기능 검증
/// </summary>
[Trait("Concept-12-Type-Safe-Enums", "CurrencyHashMapTests")]
public class CurrencyHashMapTests
{
    // 테스트 시나리오: 유효한 통화 코드로 CurrencyHashMap 인스턴스를 생성할 수 있어야 한다
    [Theory]
    [InlineData("KRW", "한국 원화", "₩")]
    [InlineData("USD", "미국 달러", "$")]
    [InlineData("EUR", "유로", "€")]
    [InlineData("JPY", "일본 엔", "¥")]
    [InlineData("GBP", "영국 파운드", "£")]
    public void Create_ShouldReturnSuccessResult_WhenSupportedCurrencyCode(string currencyCode, string expectedKoreanName, string expectedSymbol)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = CurrencyHashMap.Create(currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency =>
        {
            currency.KoreanName.ShouldBe(expectedKoreanName);
            currency.Symbol.ShouldBe(expectedSymbol);
            currency.GetCode().ShouldBe(currencyCode);
        });
    }

    // 테스트 시나리오: 빈 통화 코드로 생성 시 실패해야 한다
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnFailureResult_WhenEmptyCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = CurrencyHashMap.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("통화 코드는 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: 잘못된 형식의 통화 코드로 생성 시 실패해야 한다
    [Theory]
    [InlineData("KR")]
    [InlineData("KOREA")]
    [InlineData("123")]
    public void Create_ShouldReturnFailureResult_WhenInvalidFormatCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = CurrencyHashMap.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("통화 코드는 3자리 영문자여야 합니다"));
    }

    // 테스트 시나리오: 지원하지 않는 통화 코드로 생성 시 실패해야 한다
    [Theory]
    [InlineData("XYZ")]
    [InlineData("ABC")]
    public void Create_ShouldReturnFailureResult_WhenUnsupportedCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = CurrencyHashMap.Create(currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("지원하지 않는 통화 코드입니다"));
    }

    // 테스트 시나리오: 소문자 통화 코드도 대문자로 변환되어야 한다
    [Theory]
    [InlineData("krw", "KRW")]
    [InlineData("usd", "USD")]
    public void Create_ShouldReturnSuccessResult_WhenLowerCaseCurrencyCode(string inputCode, string expectedCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = CurrencyHashMap.Create(inputCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency => currency.GetCode().ShouldBe(expectedCode));
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 코드로 인스턴스를 생성해야 한다
    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void CreateFromValidated_ShouldReturnInstance_WhenValidatedCurrencyCode(string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = CurrencyHashMap.CreateFromValidated(currencyCode);

        // Assert
        actual.ShouldNotBeNull();
        actual.GetCode().ShouldBe(currencyCode);
    }

    // 테스트 시나리오: 정적 필드 인스턴스들이 동등성을 보장해야 한다
    [Fact]
    public void StaticFields_ShouldBeEqualToCreatedInstances_WhenSameCode()
    {
        // Arrange
        var created = CurrencyHashMap.CreateFromValidated("KRW");

        // Act & Assert
        created.ShouldBe(CurrencyHashMap.KRW);
    }

    // 테스트 시나리오: GetAllSupportedCurrencies 메서드가 모든 지원 통화를 반환해야 한다
    [Fact]
    public void GetAllSupportedCurrencies_ShouldReturnAllCurrencies_WhenCalled()
    {
        // Arrange
        int expectedCount = 10;

        // Act
        var actual = CurrencyHashMap.GetAllSupportedCurrencies();

        // Assert
        actual.Count().ShouldBe(expectedCount);
    }
}
