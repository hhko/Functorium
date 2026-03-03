using ValueEquality.ValueObjects;

namespace ValueEquality.Tests.Unit;

/// <summary>
/// Denominator 값 객체의 동등성 테스트
/// 
/// 테스트 목적:
/// 1. 값 기반 동등성 비교 검증
/// 2. IEquatable<T> 인터페이스 구현 검증
/// 3. GetHashCode와 Equals의 일관성 검증
/// 4. 연산자 오버로딩 검증
/// </summary>
[Trait("Concept-07-Value-Equality", "EqualityIntegrationTests")]
public class DenominatorTests
{
    // 테스트 시나리오: 같은 값을 가진 두 Denominator 객체는 값 기반으로 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenValuesAreEqual()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     select (a, b);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                a.Equals(b).ShouldBeTrue();
                (a == b).ShouldBeTrue();
                (a != b).ShouldBeFalse();
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 다른 값을 가진 두 Denominator 객체는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenValuesAreDifferent()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(10)
                     select (a, b);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                a.Equals(b).ShouldBeFalse();
                (a == b).ShouldBeFalse();
                (a != b).ShouldBeTrue();
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 같은 값을 가진 두 Denominator 객체는 같은 해시 코드를 가져야 한다
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenValuesAreEqual()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     select (a, b);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                a.GetHashCode().ShouldBe(b.GetHashCode());
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 다른 값을 가진 두 Denominator 객체는 다른 해시 코드를 가져야 한다
    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCode_WhenValuesAreDifferent()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(10)
                     select (a, b);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                a.GetHashCode().ShouldNotBe(b.GetHashCode());
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: null과의 비교에서 false를 반환해야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     select a;

        // Act & Assert
        result.Match(
            Succ: a =>
            {
                a.Equals(null).ShouldBeFalse();
                (a == null).ShouldBeFalse();
                (null == a).ShouldBeFalse();
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 참조 동등성과 값 동등성의 차이를 확인해야 한다
    [Fact]
    public void ReferenceEquals_ShouldReturnFalse_WhenValuesAreEqual()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     select (a, b);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                ReferenceEquals(a, b).ShouldBeFalse();
                a.Equals(b).ShouldBeTrue();
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }
}
