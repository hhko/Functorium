using DefensiveProgramming;

/// <summary>
/// MathOperations 클래스의 방어적 프로그래밍 구현 방법 테스트
/// 
/// 테스트 목적:
/// 1. 사전 검증을 통한 정의된(의도된) 예외 처리 방법 검증
/// 2. 예외 없이 bool 반환을 활용한 TryDivide 패턴 검증
/// </summary>
[Trait("Concept-02-Defensive-Programming", "MathOperationsTests")]
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

    // 테스트 시나리오: 분모가 0이면 ArgumentException 예외가 발생해야 한다
    [Fact]
    public void Divide_ShouldThrowArgumentException_WhenDenominatorIsZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 0;

        // Act & Assert
        var actual = Should.Throw<ArgumentException>(() => MathOperations.Divide(numerator, denominator));

        // Assert
        actual.Message.ShouldContain("0으로 나눌 수 없습니다");
        actual.ParamName.ShouldBe("denominator");
    }

    // 테스트 시나리오: 유효한 분모일 때 true를 반환하고 정확한 결과를 out 매개변수에 설정해야 한다
    [Fact]
    public void TryDivide_ShouldReturnTrueAndCorrectResult_ForValidDenominators()
    {
        // Arrange
        int numerator = 10;
        int denominator = 2;
        int expected = 5;

        // Act
        bool success = MathOperations.TryDivide(numerator, denominator, out int actual);

        // Assert
        success.ShouldBeTrue();
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 분모가 0일 때 false를 반환하고 out 매개변수는 기본값이어야 한다
    [Fact]
    public void TryDivide_ShouldReturnFalseAndDefaultResult_WhenDenominatorIsZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 0;
        int expectedDefault = default(int);

        // Act
        bool success = MathOperations.TryDivide(numerator, denominator, out int actual);

        // Assert
        success.ShouldBeFalse();
        actual.ShouldBe(expectedDefault);
    }

    // 테스트 시나리오: TryDivide는 예외를 발생시키지 않아야 한다
    [Fact]
    public void TryDivide_ShouldNotThrowException_WhenDenominatorIsZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 0;

        // Act & Assert
        Should.NotThrow(() => MathOperations.TryDivide(numerator, denominator, out int result));
    }
}
