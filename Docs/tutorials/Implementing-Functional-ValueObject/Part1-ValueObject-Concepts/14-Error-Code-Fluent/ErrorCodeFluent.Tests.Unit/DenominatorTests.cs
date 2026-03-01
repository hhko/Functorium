using ErrorCodeFluent.ValueObjects.Comparable.PrimitiveValueObjects;

/// <summary>
/// Denominator 값 객체의 생성 및 검증 기능 테스트
///
/// 테스트 목적:
/// 1. 유효한 값으로 Denominator 생성 검증
/// 2. 무효한 값(0)으로 Denominator 생성 실패 검증
/// 3. DomainErrorAssertions를 사용한 타입 안전 에러 검증
/// </summary>
[Trait("Concept-14-Error-Code-Fluent", "DenominatorTests")]
public class DenominatorTests
{
    private sealed record Zero : DomainErrorType.Custom;
    #region 실패 케이스 - 타입 안전 Assertion 사용

    /// <summary>
    /// 0 값으로 Denominator 생성 시 실패해야 한다 (타입 안전 Assertion)
    /// </summary>
    /// <remarks>
    /// DomainErrorAssertions.ShouldBeDomainError 사용:
    /// - 타입 안전: 값 객체 타입과 에러 타입 모두 컴파일 타임 검증
    /// - 간결함: 기존 3-5줄 코드가 1줄로 축약
    /// - 일관성: DomainError.For&lt;T&gt;() 생성 패턴과 동일한 검증 패턴
    /// </remarks>
    [Fact]
    public void Create_ShouldReturnDomainError_WhenValueIsZero()
    {
        // Arrange
        int value = 0;

        // Act
        var actual = Denominator.Create(value);

        // Assert - 타입 안전 Assertion
        // Fin<Denominator>에서 에러 검증
        actual.ShouldBeDomainError<Denominator, Denominator>(new Zero());
    }

    /// <summary>
    /// 0 값으로 Denominator 생성 시 현재 값까지 검증 (더 엄격한 검증)
    /// </summary>
    [Fact]
    public void Create_ShouldReturnDomainErrorWithCurrentValue_WhenValueIsZero()
    {
        // Arrange
        int value = 0;

        // Act
        var actual = Denominator.Create(value);

        // Assert - 에러 타입과 현재 값 모두 검증
        actual.ShouldBeDomainError<Denominator, Denominator, int>(
            new Zero(),
            expectedCurrentValue: 0);
    }

    /// <summary>
    /// Validation 메서드 직접 호출 테스트
    /// </summary>
    [Fact]
    public void Validate_ShouldHaveDomainError_WhenValueIsZero()
    {
        // Arrange
        int value = 0;

        // Act - Validate 메서드 직접 호출
        Validation<Error, int> validation = Denominator.Validate(value);

        // Assert - Validation 결과에 대한 타입 안전 검증
        validation.ShouldHaveDomainError<Denominator, int>(new Zero());
    }

    #endregion

    #region 성공 케이스

    /// <summary>
    /// 0이 아닌 값으로 Denominator 생성 시 성공해야 한다
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Create_ShouldReturnSuccessResult_WhenValueIsNotZero(int value)
    {
        // Act
        var actual = Denominator.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(denominator => ((int)denominator).ShouldBe(value));
    }

    #endregion
}
