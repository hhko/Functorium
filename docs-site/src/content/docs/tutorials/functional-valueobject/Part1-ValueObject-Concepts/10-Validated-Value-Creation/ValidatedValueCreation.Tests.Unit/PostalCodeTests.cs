using ValidatedValueCreation.ValueObjects;

/// <summary>
/// PostalCode 클래스의 3가지 메서드 패턴 테스트
/// 
/// 테스트 목적:
/// 1. Create 메서드의 검증 후 객체 생성 기능 검증
/// 2. Validate 메서드의 독립적 검증 기능 검증
/// 3. CreateFromValidated 메서드의 검증된 값으로 직접 생성 기능 검증
/// </summary>
[Trait("Concept-10-Validated-Value-Creation", "PostalCodeTests")]
public class PostalCodeTests
{
    // 테스트 시나리오: 유효한 우편번호로 PostalCode 객체가 정상 생성되어야 한다
    [Theory]
    [InlineData("12345")]
    [InlineData("123456")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void Create_ShouldReturnSuccess_WhenPostalCodeIsValid(string postalCode)
    {
        // Arrange
        string expected = postalCode;

        // Act
        var actual = PostalCode.Create(postalCode);

        // Assert
        actual.Match(
            Succ: code => ((string)code).ShouldBe(expected),
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: 빈 우편번호로 PostalCode 생성 시 실패해야 한다
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnFailure_WhenPostalCodeIsEmpty(string postalCode)
    {
        // Arrange
        string expectedMessage = "우편번호는 비어있을 수 없습니다";

        // Act
        var actual = PostalCode.Create(postalCode);

        // Assert
        actual.Match(
            Succ: code => throw new Exception($"Expected failure but got success: {code}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: 숫자가 아닌 문자가 포함된 우편번호로 PostalCode 생성 시 실패해야 한다
    [Theory]
    [InlineData("1234a")]
    [InlineData("abc123")]
    [InlineData("123-456")]
    [InlineData("123 456")]
    public void Create_ShouldReturnFailure_WhenPostalCodeContainsNonDigits(string postalCode)
    {
        // Arrange
        string expectedMessage = "우편번호는 숫자만 포함해야 합니다";

        // Act
        var actual = PostalCode.Create(postalCode);

        // Assert
        actual.Match(
            Succ: code => throw new Exception($"Expected failure but got success: {code}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: 유효한 우편번호에 대해 Validate 메서드가 성공 결과를 반환해야 한다
    [Theory]
    [InlineData("12345")]
    [InlineData("123456")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void Validate_ShouldReturnSuccess_WhenPostalCodeIsValid(string postalCode)
    {
        // Arrange
        string expected = postalCode;

        // Act
        var actual = PostalCode.Validate(postalCode);

        // Assert
        actual.Match(
            Succ: value => value.ShouldBe(expected),
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: 빈 우편번호에 대해 Validate 메서드가 실패 결과를 반환해야 한다
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnFailure_WhenPostalCodeIsEmpty(string postalCode)
    {
        // Arrange
        string expectedMessage = "우편번호는 비어있을 수 없습니다";

        // Act
        var actual = PostalCode.Validate(postalCode);

        // Assert
        actual.Match(
            Succ: value => throw new Exception($"Expected failure but got success: {value}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: 숫자가 아닌 문자가 포함된 우편번호에 대해 Validate 메서드가 실패 결과를 반환해야 한다
    [Theory]
    [InlineData("1234a")]
    [InlineData("abc123")]
    [InlineData("123-456")]
    [InlineData("123 456")]
    public void Validate_ShouldReturnFailure_WhenPostalCodeContainsNonDigits(string postalCode)
    {
        // Arrange
        string expectedMessage = "우편번호는 숫자만 포함해야 합니다";

        // Act
        var actual = PostalCode.Validate(postalCode);

        // Assert
        actual.Match(
            Succ: value => throw new Exception($"Expected failure but got success: {value}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 값으로 직접 PostalCode 객체를 생성해야 한다
    [Theory]
    [InlineData("12345")]
    [InlineData("123456")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void CreateFromValidated_ShouldCreatePostalCode_WhenValidatedValueIsProvided(string validatedValue)
    {
        // Arrange
        string expected = validatedValue;

        // Act
        var actual = PostalCode.CreateFromValidated(validatedValue);

        // Assert
        ((string)actual).ShouldBe(expected);
    }

    // 테스트 시나리오: Create와 Validate가 동일한 검증 로직을 사용하는지 검증해야 한다
    [Theory]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData("1234a")]
    public void Create_ShouldUseSameValidationLogic_AsValidate(string postalCode)
    {
        // Arrange
        var validationResult = PostalCode.Validate(postalCode);
        var creationResult = PostalCode.Create(postalCode);

        // Act & Assert
        bool validationSuccess = false;
        bool creationSuccess = false;
        string validationError = "";
        string creationError = "";

        validationResult.Match(
            Succ: _ => validationSuccess = true,
            Fail: error => validationError = error.Message
        );

        creationResult.Match(
            Succ: _ => creationSuccess = true,
            Fail: error => creationError = error.Message
        );

        validationSuccess.ShouldBe(creationSuccess);

        if (!validationSuccess)
        {
            validationError.ShouldBe(creationError);
        }
    }

    // 테스트 시나리오: null 우편번호로 PostalCode 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenPostalCodeIsNull()
    {
        // Arrange
        string? postalCode = null;
        string expectedMessage = "우편번호는 비어있을 수 없습니다";

        // Act
        var actual = PostalCode.Create(postalCode!);

        // Assert
        actual.Match(
            Succ: code => throw new Exception($"Expected failure but got success: {code}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: null 우편번호에 대해 Validate 메서드가 실패 결과를 반환해야 한다
    [Fact]
    public void Validate_ShouldReturnFailure_WhenPostalCodeIsNull()
    {
        // Arrange
        string? postalCode = null;
        string expectedMessage = "우편번호는 비어있을 수 없습니다";

        // Act
        var actual = PostalCode.Validate(postalCode!);

        // Assert
        actual.Match(
            Succ: value => throw new Exception($"Expected failure but got success: {value}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: Validate 메서드가 순수 함수로 동작하는지 검증해야 한다
    [Fact]
    public void Validate_ShouldBePureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string postalCode = "12345";

        // Act
        var result1 = PostalCode.Validate(postalCode);
        var result2 = PostalCode.Validate(postalCode);

        // Assert
        string val1 = "";
        string val2 = "";

        result1.Match(
            Succ: v => val1 = v,
            Fail: _ => throw new Exception("Expected success")
        );

        result2.Match(
            Succ: v => val2 = v,
            Fail: _ => throw new Exception("Expected success")
        );

        val1.ShouldBe(val2);
    }
}
