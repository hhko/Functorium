using ValidationFluent.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// PostalCode 값 객체의 Validate&lt;T&gt; Fluent 검증 테스트
///
/// 테스트 목적:
/// 1. 유효한 값으로 PostalCode 생성 검증
/// 2. Fluent API를 사용한 다단계 검증 테스트 (NotEmpty → ThenExactLength → ThenMatches)
/// 3. DomainErrorAssertions를 사용한 타입 안전 에러 검증
/// </summary>
[Trait("Concept-15-Validation-Fluent", "PostalCodeTests")]
public class PostalCodeTests
{
    #region 실패 케이스 - 타입 안전 Assertion 사용

    /// <summary>
    /// 빈 값으로 PostalCode 생성 시 실패해야 한다
    /// </summary>
    /// <remarks>
    /// Validate&lt;PostalCode&gt;.NotEmpty() 검증
    /// </remarks>
    [Fact]
    public void Create_ShouldReturnDomainError_WhenValueIsEmpty()
    {
        // Arrange
        string value = "";

        // Act
        var actual = PostalCode.Create(value);

        // Assert - 타입 안전 Assertion
        actual.ShouldBeDomainError<PostalCode, PostalCode>(new DomainErrorType.Empty());
    }

    /// <summary>
    /// 5자리가 아닌 값으로 PostalCode 생성 시 실패해야 한다
    /// </summary>
    /// <remarks>
    /// .ThenExactLength(5) 검증
    /// </remarks>
    [Theory]
    [InlineData("1234")]    // 4자리
    [InlineData("123456")]  // 6자리
    public void Create_ShouldReturnDomainError_WhenLengthIsNot5(string value)
    {
        // Act
        var actual = PostalCode.Create(value);

        // Assert
        actual.ShouldBeDomainError<PostalCode, PostalCode>(new DomainErrorType.WrongLength(5));
    }

    /// <summary>
    /// 숫자가 아닌 문자가 포함된 값으로 PostalCode 생성 시 실패해야 한다
    /// </summary>
    /// <remarks>
    /// .ThenMatches(DigitsPattern) 검증
    /// </remarks>
    [Theory]
    [InlineData("1234a")]  // 문자 포함
    [InlineData("12-45")]  // 특수문자 포함
    [InlineData("abcde")]  // 모두 문자
    public void Create_ShouldReturnDomainError_WhenContainsNonDigits(string value)
    {
        // Act
        var actual = PostalCode.Create(value);

        // Assert
        actual.ShouldBeDomainError<PostalCode, PostalCode>(new DomainErrorType.InvalidFormat());
    }

    /// <summary>
    /// Validation 메서드 직접 호출 테스트
    /// </summary>
    [Fact]
    public void Validate_ShouldHaveDomainError_WhenValueIsEmpty()
    {
        // Arrange
        string value = "";

        // Act - Validate 메서드 직접 호출
        Validation<Error, string> validation = PostalCode.Validate(value);

        // Assert - Validation 결과에 대한 타입 안전 검증
        validation.ShouldHaveDomainError<PostalCode, string>(new DomainErrorType.Empty());
    }

    #endregion

    #region 성공 케이스

    /// <summary>
    /// 유효한 5자리 숫자로 PostalCode 생성 시 성공해야 한다
    /// </summary>
    [Theory]
    [InlineData("12345")]
    [InlineData("00000")]
    [InlineData("99999")]
    public void Create_ShouldReturnSuccessResult_WhenValueIsValid(string value)
    {
        // Act
        var actual = PostalCode.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(postalCode => ((string)postalCode).ShouldBe(value));
    }

    #endregion
}
