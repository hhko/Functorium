using CreateValidateSeparation.ValueObjects;

/// <summary>
/// Denominator 클래스의 Create와 Validate 분리 패턴 테스트
/// 
/// 테스트 목적:
/// 1. Validate 메서드의 독립적 검증 기능 검증
/// 2. Create 메서드의 검증 후 객체 생성 기능 검증
/// 3. Create와 Validate의 책임 분리 원칙 검증
/// </summary>
[Trait("Concept-09-Create-Validate-Separation", "DenominatorTests")]
public class DenominatorTests
{
    // 테스트 시나리오: 유효한 값에 대해 Validate 메서드가 성공 결과를 반환해야 한다
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(-3)]
    [InlineData(-10)]
    public void Validate_ShouldReturnSuccess_WhenValueIsNotZero(int value)
    {
        // Arrange
        int expected = value;

        // Act
        var actual = Denominator.Validate(value);

        // Assert
        actual.Match(
            Succ: validValue => validValue.ShouldBe(expected),
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: 0 값에 대해 Validate 메서드가 실패 결과를 반환해야 한다
    [Fact]
    public void Validate_ShouldReturnFailure_WhenValueIsZero()
    {
        // Arrange
        int value = 0;
        string expectedMessage = "0은 허용되지 않습니다";

        // Act
        var actual = Denominator.Validate(value);

        // Assert
        actual.Match(
            Succ: validValue => throw new Exception($"Expected failure but got success: {validValue}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: Create와 Validate가 동일한 검증 로직을 사용하는지 검증해야 한다
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_ShouldUseSameValidationLogic_AsValidate(int value)
    {
        // Arrange
        var validationResult = Denominator.Validate(value);
        var creationResult = Denominator.Create(value);

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

    // 테스트 시나리오: Validate 메서드가 순수 함수로 동작하는지 검증해야 한다
    [Fact]
    public void Validate_ShouldBePureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        int value = 5;

        // Act
        var result1 = Denominator.Validate(value);
        var result2 = Denominator.Validate(value);

        // Assert
        int val1 = 0;
        int val2 = 0;

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