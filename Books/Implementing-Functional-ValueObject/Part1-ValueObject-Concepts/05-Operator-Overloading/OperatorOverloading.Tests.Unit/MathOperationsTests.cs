using OperatorOverloading.ValueObjects;

namespace OperatorOverloading.Tests.Unit;

/// <summary>
/// MathOperations 클래스의 연산자 오버로딩 활용 테스트
/// 
/// 테스트 목적:
/// 1. 연산자 오버로딩을 통한 자연스러운 나눗셈 연산 검증
/// </summary>
[Trait("Concept-05-Operator-Overloading", "MathOperationsTests")]
public class MathOperationsTests
{
    // 테스트 시나리오: 유효한 Denominator 값 객체를 사용한 나눗셈 연산이 올바른 결과를 반환해야 한다
    [Fact]
    public void Divide_ShouldReturnCorrectResult_WhenUsingValidDenominator()
    {
        // Arrange
        int numerator = 15;
        int denominatorValue = 3;
        int expected = 5;

        // Act - 1단계: 값 객체 생성 (유효성 검증 포함)
        var denominatorResult = Denominator.Create(denominatorValue);

        // Act - 2단계: 성공 케이스 처리
        var denominator = denominatorResult.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"Denominator 생성 실패: {error.Message}")
        );

        // Act - 3단계: 안전한 함수 호출 (검증 불필요)
        int actual = MathOperations.Divide(numerator, denominator);

        // Assert
        actual.ShouldBe(expected);
        ((int)denominator).ShouldBe(denominatorValue);
    }
}
