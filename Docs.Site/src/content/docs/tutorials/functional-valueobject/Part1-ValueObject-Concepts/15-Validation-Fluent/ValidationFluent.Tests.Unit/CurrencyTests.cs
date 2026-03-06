using ValidationFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// Currency 값 객체의 Validate&lt;T&gt; Fluent 검증 테스트
///
/// 테스트 목적:
/// 1. 유효한 통화 코드로 Currency 생성 검증
/// 2. Fluent API를 사용한 다단계 검증 테스트
///    (NotEmpty → ThenExactLength → ThenNormalize → ThenMust)
/// 3. ThenNormalize를 통한 대문자 정규화 검증
/// 4. ThenMust를 통한 지원 통화 검증
/// </summary>
[Trait("Concept-15-Validation-Fluent", "CurrencyTests")]
public class CurrencyTests
{
    private sealed record Unsupported : DomainErrorType.Custom;
    #region 실패 케이스 - 타입 안전 Assertion 사용

    /// <summary>
    /// 빈 값으로 Currency 생성 시 실패해야 한다
    /// </summary>
    /// <remarks>
    /// Validate&lt;Currency&gt;.NotEmpty() 검증
    /// </remarks>
    [Fact]
    public void Create_ShouldReturnDomainError_WhenValueIsEmpty()
    {
        // Arrange
        string value = "";

        // Act
        var actual = Currency.Create(value);

        // Assert
        actual.ShouldBeDomainError<Currency, Currency>(new DomainErrorType.Empty());
    }

    /// <summary>
    /// 3자리가 아닌 값으로 Currency 생성 시 실패해야 한다
    /// </summary>
    /// <remarks>
    /// .ThenExactLength(3) 검증
    /// </remarks>
    [Theory]
    [InlineData("US")]     // 2자리
    [InlineData("USDD")]   // 4자리
    public void Create_ShouldReturnDomainError_WhenLengthIsNot3(string value)
    {
        // Act
        var actual = Currency.Create(value);

        // Assert
        actual.ShouldBeDomainError<Currency, Currency>(new DomainErrorType.WrongLength(3));
    }

    /// <summary>
    /// 지원하지 않는 통화 코드로 Currency 생성 시 실패해야 한다
    /// </summary>
    /// <remarks>
    /// .ThenMust(v => SupportedCodes.Contains(v), ...) 검증
    /// </remarks>
    [Theory]
    [InlineData("XYZ")]
    [InlineData("ABC")]
    [InlineData("ZZZ")]
    public void Create_ShouldReturnDomainError_WhenCurrencyIsNotSupported(string value)
    {
        // Act
        var actual = Currency.Create(value);

        // Assert
        actual.ShouldBeDomainError<Currency, Currency>(new Unsupported());
    }

    /// <summary>
    /// Validation 메서드 직접 호출 테스트
    /// </summary>
    [Fact]
    public void Validate_ShouldHaveDomainError_WhenValueIsEmpty()
    {
        // Arrange
        string value = "";

        // Act
        Validation<Error, string> validation = Currency.Validate(value);

        // Assert
        validation.ShouldHaveDomainError<Currency, string>(new DomainErrorType.Empty());
    }

    #endregion

    #region 성공 케이스

    /// <summary>
    /// 유효한 통화 코드로 Currency 생성 시 성공해야 한다
    /// </summary>
    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("JPY")]
    public void Create_ShouldReturnSuccessResult_WhenValueIsValid(string value)
    {
        // Act
        var actual = Currency.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency => currency.Value.ShouldBe(value));
    }

    /// <summary>
    /// 소문자 통화 코드도 ThenNormalize를 통해 대문자로 정규화되어 성공해야 한다
    /// </summary>
    /// <remarks>
    /// .ThenNormalize(v => v.ToUpperInvariant()) 검증
    /// </remarks>
    [Theory]
    [InlineData("krw", "KRW")]
    [InlineData("usd", "USD")]
    [InlineData("Eur", "EUR")]
    public void Create_ShouldNormalizeToUpperCase_WhenValueIsLowerCase(string input, string expected)
    {
        // Act
        var actual = Currency.Create(input);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(currency => currency.Value.ShouldBe(expected));
    }

    #endregion
}
