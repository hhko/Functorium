using AlwaysValid.ValueObjects;

using LanguageExt;

namespace AlwaysValid.Tests.Unit;

/// <summary>
/// Denominator 값 객체의 핵심 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 값으로 Denominator 생성 시 성공 검증
/// 2. 유효하지 않은 값(0)으로 Denominator 생성 시 실패 검증
/// 3. 생성된 Denominator의 불변성과 값 접근 검증
/// 4. 항상 유효한 타입 패턴의 핵심 개념 검증
/// </summary>
[Trait("Concept-04-Always-Valid", "DenominatorTests")]
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
            Succ: denominator => denominator.Value.ShouldBe(expectedValue),
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

    // 테스트 시나리오: 유효한 같은 입력에 대해 항상 동일한 결과를 반환하는 순수 함수여야 한다
    [Fact]
    public void Create_ShouldBePureFunction_WhenDenominatorIsNotZero()
    {
        // Arrange
        int denominator = 2;
        int expected = 2;

        // Act - 같은 입력에 대해 여러 번 호출
        Fin<Denominator> actual1 = Denominator.Create(denominator);
        Fin<Denominator> actual2 = Denominator.Create(denominator);
        Fin<Denominator> actual3 = Denominator.Create(denominator);

        // Assert - 모든 결과가 동일해야 함 (순수 함수의 특성)
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
        actual3.IsSucc.ShouldBeTrue();

        actual1.Match(
            Succ: value1 => value1.Value.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );

        actual2.Match(
            Succ: value1 => value1.Value.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );

        actual3.Match(
            Succ: value1 => value1.Value.ShouldBe(expected),
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 유효하지 않은 같은 입력에 대해 항상 동일한 결과를 반환하는 순수 함수여야 한다
    [Fact]
    public void Create_ShouldBePureFunction_WhenDenominatorIstZero()
    {
        // Arrange
        int denominator = 0;
        string expectedErrorMessage = "0은 허용되지 않습니다";

        // Act - 같은 입력에 대해 여러 번 호출
        Fin<Denominator> actual1 = Denominator.Create(denominator);
        Fin<Denominator> actual2 = Denominator.Create(denominator);
        Fin<Denominator> actual3 = Denominator.Create(denominator);

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

    // 테스트 시나리오: 생성된 Denominator의 Value 속성이 올바른 값을 반환해야 한다
    [Fact]
    public void Value_ShouldReturnCorrectValue_WhenDenominatorIsCreated()
    {
        // Arrange
        int expectedValue = 42;
        var denominatorResult = Denominator.Create(expectedValue);
        var denominator = denominatorResult.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );

        // Act
        int actualValue = denominator.Value;

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    // 테스트 시나리오: Denominator 값 객체가 불변성을 유지해야 한다
    [Fact]
    public void Denominator_ShouldBeImmutable_WhenValueIsAccessed()
    {
        // Arrange
        int originalValue = 10;
        var denominatorResult = Denominator.Create(originalValue);
        var denominator = denominatorResult.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );

        // Act
        int firstAccess = denominator.Value;
        int secondAccess = denominator.Value;

        // Assert
        firstAccess.ShouldBe(originalValue);
        secondAccess.ShouldBe(originalValue);
        firstAccess.ShouldBe(secondAccess);
    }
}
