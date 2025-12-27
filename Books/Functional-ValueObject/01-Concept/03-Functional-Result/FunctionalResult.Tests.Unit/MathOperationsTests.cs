using LanguageExt;

namespace FunctionalResult.Tests.Unit;

/// <summary>
/// MathOperations 클래스의 함수형 결과 타입 테스트
/// 
/// 테스트 목적:
/// 1. 함수형 결과 타입의 장점 이해 (예외 대신 명시적인 성공/실패 표현)
/// 2. Fin<T> 타입의 활용법 습득 (Match 패턴을 통한 결과 처리)
/// 3. 순수 함수의 중요성 인식 (부작용 없는 예측 가능한 함수)
/// </summary>
[Trait("Concept-03-Functional-Result", "MathOperationsTests")]
public class MathOperationsTests
{
    // 테스트 시나리오: 유효한 분모일 때 Fin<int>.Succ(결과)를 반환해야 한다
    [Fact]
    public void Divide_ShouldReturnSuccessResult_WhenDenominatorIsNotZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 2;
        int expected = 5;

        // Act
        Fin<int> actual = MathOperations.Divide(numerator, denominator);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: value => value.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 분모가 0이면 Fin<int>.Fail(Error)를 반환해야 한다
    [Fact]
    public void Divide_ShouldReturnFailureResult_WhenDenominatorIsZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 0;
        string expectedErrorMessage = "0은 허용되지 않습니다";

        // Act
        Fin<int> actual = MathOperations.Divide(numerator, denominator);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: value => throw new Exception($"예상치 못한 성공: {value}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );
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
        Fin<int> actual1 = MathOperations.Divide(numerator, denominator);
        Fin<int> actual2 = MathOperations.Divide(numerator, denominator);
        Fin<int> actual3 = MathOperations.Divide(numerator, denominator);

        // Assert - 모든 결과가 동일해야 함 (순수 함수의 특성)
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
        actual3.IsSucc.ShouldBeTrue();

        actual1.Match(
            Succ: value1 => value1.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );

        actual2.Match(
            Succ: value2 => value2.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );

        actual3.Match(
            Succ: value3 => value3.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 같은 입력에 대해 항상 동일한 결과를 반환하는 순수 함수여야 한다
    [Fact]
    public void Divide_ShouldBePureFunction_WhenDenominatorIstZero()
    {
        // Arrange
        int numerator = 10;
        int denominator = 0;
        string expectedErrorMessage = "0은 허용되지 않습니다";

        // Act - 같은 입력에 대해 여러 번 호출
        Fin<int> actual1 = MathOperations.Divide(numerator, denominator);
        Fin<int> actual2 = MathOperations.Divide(numerator, denominator);
        Fin<int> actual3 = MathOperations.Divide(numerator, denominator);

        // Assert - 모든 결과가 동일해야 함 (순수 함수의 특성)
        actual1.IsSucc.ShouldBeFalse();
        actual2.IsSucc.ShouldBeFalse();
        actual3.IsSucc.ShouldBeFalse();

        actual1.Match(
            Succ: value1 => throw new Exception($"예상치 못한 성공: {value1}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );

        actual2.Match(
            Succ: value1 => throw new Exception($"예상치 못한 성공: {value1}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );

        actual3.Match(
            Succ: value1 => throw new Exception($"예상치 못한 성공: {value1}"),
            Fail: error => error.Message.ShouldBe(expectedErrorMessage)
        );
    }
}
