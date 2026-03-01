using LinqExpression.ValueObjects;

namespace LinqExpression.Tests.Unit;

/// <summary>
/// LINQ 표현식과 연산자 오버로딩의 통합 테스트
/// 
/// 테스트 목적:
/// 1. LINQ 표현식을 통한 복합 연산 체이닝 검증
/// 2. 에러 전파와 자동 처리 검증
/// 3. 다양한 연산자 조합과 LINQ 표현식의 상호작용 검증
/// 4. 실제 사용 시나리오 기반 통합 테스트
/// </summary>
[Trait("Concept-06-Linq-Expression", "LinqExpressionIntegrationTests")]
public class LinqExpressionIntegrationTests
{
    // 테스트 시나리오: LINQ 표현식을 사용하여 유효한 Denominator로 나눗셈 연산을 수행할 때 정확한 결과를 반환해야 한다
    [Fact]
    public void LinqExpressionDivision_ShouldReturnCorrectResult_WhenUsingValidDenominator()
    {
        // Arrange
        int numerator = 10;
        int denominatorValue = 2;
        int expected = 5;

        // Act
        var result = from denominator in Denominator.Create(denominatorValue)
                     select numerator / denominator;

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: actual => actual.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: LINQ 표현식에서 유효하지 않은 Denominator 생성 시 실패 결과를 반환해야 한다
    [Fact]
    public void LinqExpressionDivision_ShouldReturnFailureResult_WhenUsingInvalidDenominator()
    {
        // Arrange
        int numerator = 10;
        int invalidDenominatorValue = 0;
        string expectedErrorMessage = "0은 허용되지 않습니다";

        // Act
        var result = from denominator in Denominator.Create(invalidDenominatorValue)
                     select numerator / denominator;

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: value => throw new Exception($"예상치 못한 성공: {value}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );
    }

    // 테스트 시나리오: LINQ 표현식을 사용한 다단계 연산 체이닝이 정확한 결과를 반환해야 한다
    [Fact]
    public void LinqExpressionChaining_ShouldReturnCorrectResult_WhenUsingMultipleOperations()
    {
        // Arrange
        int numerator = 100;
        int denominator1Value = 10;
        int denominator2Value = 2;
        int denominator3Value = 5;
        int expected = 1; // (100 / 10) / 2 / 5 = 10 / 2 / 5 = 5 / 5 = 1

        // Act - LINQ 표현식을 사용한 다단계 연산 체이닝
        var result = from denom1 in Denominator.Create(denominator1Value)
                     from denom2 in Denominator.Create(denominator2Value)
                     from denom3 in Denominator.Create(denominator3Value)
                     select ((numerator / denom1) / denom2) / denom3;

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: actual => actual.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: LINQ 표현식에서 중간 단계에서 실패가 발생하면 에러가 자동으로 전파되어야 한다
    [Fact]
    public void LinqExpressionErrorPropagation_ShouldReturnFailureResult_WhenAnyStepFails()
    {
        // Arrange
        int numerator = 100;
        int validDenominator1Value = 10;
        int invalidDenominatorValue = 0;
        int validDenominator2Value = 5;
        string expectedErrorMessage = "0은 허용되지 않습니다";

        // Act - LINQ 표현식을 사용한 다단계 연산 (중간 단계에서 실패)
        var result = from denom1 in Denominator.Create(validDenominator1Value)
                     from denom2 in Denominator.Create(invalidDenominatorValue)
                     from denom3 in Denominator.Create(validDenominator2Value)
                     select ((numerator / denom1) / denom2) / denom3;

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: value => throw new Exception($"예상치 못한 성공: {value}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );
    }

    // 테스트 시나리오: LINQ 표현식과 MathOperations.Divide 메서드를 조합한 연산이 정확한 결과를 반환해야 한다
    [Theory]
    [InlineData(30, 6, 2, 2)]
    [InlineData(60, 12, 3, 1)]
    [InlineData(90, 18, 5, 1)]
    public void LinqExpressionMathOperations_ShouldReturnCorrectResult_WhenUsingCombinedOperations(int numerator, int denominator1Value, int denominator2Value, int expected)
    {
        // Arrange
        int expectedResult = expected;

        // Act - LINQ 표현식과 MathOperations.Divide 조합
        var result = from denom1 in Denominator.Create(denominator1Value)
                     from denom2 in Denominator.Create(denominator2Value)
                     select MathOperations.Divide(numerator, denom1) / denom2;

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: actual => actual.ShouldBe(expectedResult),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }
}
