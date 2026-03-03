using ValidatedValueCreation.ValueObjects;

/// <summary>
/// Street 클래스의 3가지 메서드 패턴 테스트
/// 
/// 테스트 목적:
/// 1. Create 메서드의 검증 후 객체 생성 기능 검증
/// 2. Validate 메서드의 독립적 검증 기능 검증
/// 3. CreateFromValidated 메서드의 검증된 값으로 직접 생성 기능 검증
/// </summary>
[Trait("Concept-10-Validated-Value-Creation", "StreetTests")]
public class StreetTests
{
    // 테스트 시나리오: 유효한 거리명으로 Street 객체가 정상 생성되어야 한다
    [Theory]
    [InlineData("123 Main St")]
    [InlineData("Broadway")]
    [InlineData("서울시 강남구 테헤란로")]
    public void Create_ShouldReturnSuccess_WhenStreetNameIsValid(string streetName)
    {
        // Arrange
        string expected = streetName;

        // Act
        var actual = Street.Create(streetName);

        // Assert
        actual.Match(
            Succ: street => ((string)street).ShouldBe(expected),
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: 빈 거리명으로 Street 생성 시 실패해야 한다
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnFailure_WhenStreetNameIsEmpty(string streetName)
    {
        // Arrange
        string expectedMessage = "거리명은 비어있을 수 없습니다";

        // Act
        var actual = Street.Create(streetName);

        // Assert
        actual.Match(
            Succ: street => throw new Exception($"Expected failure but got success: {street}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: 유효한 거리명에 대해 Validate 메서드가 성공 결과를 반환해야 한다
    [Theory]
    [InlineData("123 Main St")]
    [InlineData("Broadway")]
    [InlineData("서울시 강남구 테헤란로")]
    public void Validate_ShouldReturnSuccess_WhenStreetNameIsValid(string streetName)
    {
        // Arrange
        string expected = streetName;

        // Act
        var actual = Street.Validate(streetName);

        // Assert
        actual.Match(
            Succ: value => value.ShouldBe(expected),
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: 빈 거리명에 대해 Validate 메서드가 실패 결과를 반환해야 한다
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnFailure_WhenStreetNameIsEmpty(string streetName)
    {
        // Arrange
        string expectedMessage = "거리명은 비어있을 수 없습니다";

        // Act
        var actual = Street.Validate(streetName);

        // Assert
        actual.Match(
            Succ: value => throw new Exception($"Expected failure but got success: {value}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 값으로 직접 Street 객체를 생성해야 한다
    [Theory]
    [InlineData("123 Main St")]
    [InlineData("Broadway")]
    [InlineData("서울시 강남구 테헤란로")]
    public void CreateFromValidated_ShouldCreateStreet_WhenValidatedValueIsProvided(string validatedValue)
    {
        // Arrange
        string expected = validatedValue;

        // Act
        var actual = Street.CreateFromValidated(validatedValue);

        // Assert
        ((string)actual).ShouldBe(expected);
    }

    // 테스트 시나리오: Create와 Validate가 동일한 검증 로직을 사용하는지 검증해야 한다
    [Theory]
    [InlineData("123 Main St")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldUseSameValidationLogic_AsValidate(string streetName)
    {
        // Arrange
        var validationResult = Street.Validate(streetName);
        var creationResult = Street.Create(streetName);

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

    // 테스트 시나리오: null 거리명으로 Street 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenStreetNameIsNull()
    {
        // Arrange
        string? streetName = null;
        string expectedMessage = "거리명은 비어있을 수 없습니다";

        // Act
        var actual = Street.Create(streetName!);

        // Assert
        actual.Match(
            Succ: street => throw new Exception($"Expected failure but got success: {street}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: null 거리명에 대해 Validate 메서드가 실패 결과를 반환해야 한다
    [Fact]
    public void Validate_ShouldReturnFailure_WhenStreetNameIsNull()
    {
        // Arrange
        string? streetName = null;
        string expectedMessage = "거리명은 비어있을 수 없습니다";

        // Act
        var actual = Street.Validate(streetName!);

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
        string streetName = "123 Main St";

        // Act
        var result1 = Street.Validate(streetName);
        var result2 = Street.Validate(streetName);

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
