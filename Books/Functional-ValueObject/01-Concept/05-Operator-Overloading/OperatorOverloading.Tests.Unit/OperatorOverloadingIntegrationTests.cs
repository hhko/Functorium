using OperatorOverloading.ValueObjects;

namespace OperatorOverloading.Tests.Unit;

/// <summary>
/// 연산자 오버로딩의 통합 동작과 실제 사용 시나리오 테스트
/// 
/// 테스트 목적:
/// 1. 연산자 오버로딩과 변환 연산자의 통합 동작 검증
/// 2. 실제 도메인 시나리오에서의 자연스러운 사용법 검증
/// 3. 에러 상황에서의 적절한 처리 검증
/// </summary>
[Trait("Concept-05-Operator-Overloading", "OperatorOverloadingIntegrationTests")]
public class OperatorOverloadingIntegrationTests
{
    [Fact]
    public void DivisionOperator_ShouldWorkWithDirectOperatorUsage_WhenUsingValidDenominator()
    {
        // 테스트 시나리오: 직접 연산자 사용 시에도 정상적으로 동작해야 한다

        // Arrange
        int numerator = 18;
        var denominator = Denominator.Create(6).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 3;

        // Act
        int actual = numerator / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void ConversionOperators_ShouldWorkTogether_WhenConvertingBetweenTypes()
    {
        // 테스트 시나리오: 변환 연산자들이 함께 사용될 때 정상적으로 동작해야 한다

        // Arrange
        int originalValue = 25;
        int expected = 25;

        // Act
        var denominator = (Denominator)originalValue;  // 명시적 변환: Denominator <- int
        int actual = (int)denominator;                 // 명시적 변환: int         <- Denominator  

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void DivisionOperator_ShouldWorkWithChainedOperations_WhenUsingValidDenominators()
    {
        // 테스트 시나리오: 연쇄 연산에서도 정상적으로 동작해야 한다

        // Arrange
        var denominator1 = Denominator.Create(2).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        var denominator2 = Denominator.Create(3).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 2;  // 12 / 2 / 3 = 6 / 3 = 2

        // Act
        int actual = 12 / denominator1 / denominator2;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void DivisionOperator_ShouldWorkWithComplexExpressions_WhenUsingValidDenominators()
    {
        // 테스트 시나리오: 복잡한 수식에서도 정상적으로 동작해야 한다

        // Arrange
        var denominator = Denominator.Create(4).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 5;  // (20 + 0) / 4 = 20 / 4 = 5

        // Act
        int actual = (20 + 0) / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void DivisionOperator_ShouldWorkWithMethodCalls_WhenUsingValidDenominator()
    {
        // 테스트 시나리오: 메서드 호출 결과와 함께 사용될 때도 정상적으로 동작해야 한다

        // Arrange
        var denominator = Denominator.Create(5).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 4;  // GetValue() / 5 = 20 / 5 = 4

        // Act
        int actual = GetValue() / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void DivisionOperator_ShouldWorkWithLocalVariables_WhenUsingValidDenominator()
    {
        // 테스트 시나리오: 지역 변수와 함께 사용될 때도 정상적으로 동작해야 한다

        // Arrange
        int localNumerator = 30;
        var denominator = Denominator.Create(6).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 5;

        // Act
        int actual = localNumerator / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void DivisionOperator_ShouldWorkWithConstants_WhenUsingValidDenominator()
    {
        // 테스트 시나리오: 상수와 함께 사용될 때도 정상적으로 동작해야 한다

        // Arrange
        const int CONSTANT_NUMERATOR = 50;
        var denominator = Denominator.Create(10).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = 5;

        // Act
        int actual = CONSTANT_NUMERATOR / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void DivisionOperator_ShouldWorkWithNegativeResults_WhenUsingValidDenominator()
    {
        // 테스트 시나리오: 음수 결과가 나올 때도 정상적으로 동작해야 한다

        // Arrange
        int numerator = -15;
        var denominator = Denominator.Create(3).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException("테스트 실패: Denominator 생성 실패")
        );
        int expected = -5;

        // Act
        int actual = numerator / denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(100, 2, 50)]
    [InlineData(200, 4, 50)]
    [InlineData(300, 6, 50)]
    public void DivisionOperator_ShouldWorkWithTheoryData_WhenUsingValidDenominators(int numerator, int denominatorValue, int expected)
    {
        // 테스트 시나리오: Theory와 InlineData를 사용한 다양한 케이스에서도 정상적으로 동작해야 한다

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

    // 테스트 헬퍼 메서드
    private static int GetValue() => 20;
}
