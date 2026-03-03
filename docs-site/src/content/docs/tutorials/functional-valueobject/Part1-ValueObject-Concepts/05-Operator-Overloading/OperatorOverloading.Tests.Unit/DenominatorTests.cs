using OperatorOverloading.ValueObjects;

namespace OperatorOverloading.Tests.Unit;

/// <summary>
/// Denominator 값 객체의 연산자 오버로딩과 변환 연산자 테스트
/// 
/// 테스트 목적:
/// 1. 연산자 오버로딩을 통한 자연스러운 나눗셈 연산 검증
/// 2. 명시적/암시적 변환 연산자 동작 검증
/// 3. 에러 상황에서의 적절한 예외 발생 검증
/// </summary>
[Trait("Concept-05-Operator-Overloading", "DenominatorTests")]
public class DenominatorTests
{
    // 테스트 시나리오: 유효한 값(0이 아닌 정수)으로 Denominator 생성 시 성공 결과를 반환해야 한다
    [Fact]
    public void Create_ShouldReturnSuccessResult_WhenValueIsNotZero()
    {
        // Arrange
        int validValue = 5;
        int expectedValue = 5;

        // Act
        var actual = Denominator.Create(validValue);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: denominator => ((int)denominator).ShouldBe(expectedValue),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 유효하지 않은 값(0)으로 Denominator 생성 시 실패 결과를 반환해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenValueIsZero()
    {
        // Arrange
        int invalidValue = 0;
        string expectedErrorMessage = "0은 허용되지 않습니다";

        // Act
        var actual = Denominator.Create(invalidValue);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: value => throw new Exception($"예상치 못한 성공: {value}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );
    }

    // 테스트 시나리오: 유효한 Denominator로 나눗셈 연산을 연산자 오버로딩을 통해 직접 수행할 때 정확한 결과를 반환해야 한다
    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(15, 3, 5)]
    [InlineData(100, 10, 10)]
    public void DivisionOperator_ShouldReturnCorrectResult_WhenUsingOperatorOverloadingValidDenominator(int numerator, int denominatorValue, int expected)
    {
        // Arrange
        var denominator = Denominator.Create(denominatorValue).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );

        // Act
        int actual = numerator / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 유효한 값을 Denominator로 명시적 변환할 때 성공해야 한다
    [Fact]
    public void ExplicitConvertingIntToDenominator_ShouldReturnDenominator_WhenValueIsNotZero()
    {
        // Arrange
        int validValue = 7;

        // Act
        var actual = (Denominator)validValue;

        // Assert
        actual.ShouldNotBeNull();
    }

    // 테스트 시나리오: 유효하지 않은 값(0)을 Denominator로 명시적 변환할 때 InvalidCastException이 발생해야 한다
    [Fact]
    public void ExplicitConvertingIntToDenominator_ShouldThrowInvalidCastException_WhenValueIsZero()
    {
        // Arrange
        int invalidValue = 0;

        // Act & Assert
        var exception = Should.Throw<InvalidCastException>(() => (Denominator)invalidValue);
        exception.Message.ShouldBe("0은 Denominator로 변환할 수 없습니다");
    }

    // 테스트 시나리오: 유효한 Denominator를 int로 명시적 변환할 때 정확한 값을 반환해야 한다
    [Fact]
    public void ImplicitConvertingDenominatorToInt_ShouldReturnIntValue_WhenDenominatorIsSuccess()
    {
        // Arrange
        var denominator = Denominator.Create(12).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 12;

        // Act
        int actual = (int)denominator;

        // Assert
        actual.ShouldBe(expected);
    }
}
