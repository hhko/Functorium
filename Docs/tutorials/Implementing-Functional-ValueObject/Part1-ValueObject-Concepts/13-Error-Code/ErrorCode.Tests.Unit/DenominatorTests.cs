using ErrorCode.ValueObjects.Comparable.PrimitiveValueObjects;


/// <summary>
/// Denominator 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 값으로 Denominator 생성 검증
/// 2. 무효한 값(0)으로 Denominator 생성 실패 검증
/// 3. 검증 메서드 동작 검증
/// 4. 비교 연산자 동작 검증
/// </summary>
[Trait("Concept-13-Error-Code", "DenominatorTests")]
public class DenominatorTests
{
    // 테스트 시나리오: 0 값으로 Denominator 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenValueIsZero()
    {
        // Arrange
        int value = 0;
        string expectedErrorCode = $"{nameof(Denominator.DomainErrors)}.{nameof(Denominator)}.{nameof(Denominator.DomainErrors.Zero)}";

        // Act
        var actual = Denominator.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error =>
        {
            error.Count.ShouldBe(1);
            error.ShouldBeOfType<ErrorCodeExpected>();
            var errorCodeExpected = error as ErrorCodeExpected;
            errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
            errorCodeExpected.ErrorCurrentValue.ShouldBe("0");
        });
    }
}