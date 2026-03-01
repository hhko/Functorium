namespace BasicDivide.Tests.Unit;

/// <summary>
/// MathOperations 클래스의 기본 나눗셈 테스트
/// 
/// 테스트 목적:
/// 1. 정상적인 나눗셈 연산 검증
/// 2. 0으로 나누기 시 DivideByZeroException 발생 검증
/// </summary>
[Trait("Concept-01-Basic-Divide", "MathOperationsTests")]
public class MathOperationsTests
{
    // 테스트 시나리오: 유효한 분모일 때 정확한 결과를 반환해야 한다
    [Fact]
    public void Divide_ShouldReturnCorrectResult_WhenDenominatorIsNotZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 2;
        int expected = 5;

        // Act
        int actual = MathOperations.Divide(numerator, denominator);

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 분모가 0이면 DivideByZeroException 예외가 발생해야 한다
    [Fact]
    public void Divide_ShouldThrowDivideByZeroException_WhenDenominatorIsZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 0;

        // Act & Assert
        var actual = Should.Throw<DivideByZeroException>(() =>
            MathOperations.Divide(numerator, denominator));

        // Assert
        actual.Message.ShouldBe("Attempted to divide by zero.");
    }

    // 테스트 시나리오: 같은 입력에 대해 항상 동일한 결과를 반환하는 순수 함수여야 한다
    [Fact]
    public void Divide_ShouldBePureFunction_WhenDenominatorIsNotZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 2;
        int expected = 5;

        // Act - 같은 입력에 대해 여러 번 호출
        int actual1 = MathOperations.Divide(numerator, denominator);
        int actual2 = MathOperations.Divide(numerator, denominator);
        int actual3 = MathOperations.Divide(numerator, denominator);

        // Assert - 모든 결과가 동일해야 함 (순수 함수의 특성)
        actual1.ShouldBe(expected);
        actual2.ShouldBe(expected);
        actual3.ShouldBe(expected);
        actual1.ShouldBe(actual2);
        actual2.ShouldBe(actual3);
    }
}
