namespace TestingStrategies.Tests.Unit;

/// <summary>
/// 동등성 테스트 패턴 검증
///
/// 테스트 목적:
/// 1. 값 객체 동등성 패턴 학습
/// 2. GetHashCode 일관성 검증
/// 3. == 연산자 검증
/// </summary>
[Trait("Part4-Testing-Strategies", "EqualityPatternTests")]
public class EqualityPatternTests
{
    #region Equals 메서드 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenValuesAreEqual()
    {
        // Arrange
        var email1 = Email.CreateFromValidated("user@example.com");
        var email2 = Email.CreateFromValidated("user@example.com");

        // Act & Assert
        email1.Equals(email2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenValuesAreDifferent()
    {
        // Arrange
        var email1 = Email.CreateFromValidated("user1@example.com");
        var email2 = Email.CreateFromValidated("user2@example.com");

        // Act & Assert
        email1.Equals(email2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenComparingWithNull()
    {
        // Arrange
        var email = Email.CreateFromValidated("user@example.com");

        // Act & Assert
        email.Equals(null).ShouldBeFalse();
    }

    #endregion

    #region GetHashCode 테스트

    [Fact]
    public void GetHashCode_ReturnsSameValue_WhenValuesAreEqual()
    {
        // Arrange
        var email1 = Email.CreateFromValidated("user@example.com");
        var email2 = Email.CreateFromValidated("user@example.com");

        // Act & Assert
        email1.GetHashCode().ShouldBe(email2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValue_WhenValuesAreDifferent()
    {
        // Arrange
        var email1 = Email.CreateFromValidated("user1@example.com");
        var email2 = Email.CreateFromValidated("user2@example.com");

        // Act & Assert
        // 참고: 해시 충돌 가능성이 있으므로 다를 수 있음
        // 이 테스트는 일반적인 경우에만 적용
        email1.GetHashCode().ShouldNotBe(email2.GetHashCode());
    }

    #endregion

    #region 연산자 테스트

    [Fact]
    public void EqualityOperator_ReturnsTrue_WhenValuesAreEqual()
    {
        // Arrange
        var email1 = Email.CreateFromValidated("user@example.com");
        var email2 = Email.CreateFromValidated("user@example.com");

        // Act & Assert
        (email1 == email2).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenValuesAreDifferent()
    {
        // Arrange
        var email1 = Email.CreateFromValidated("user1@example.com");
        var email2 = Email.CreateFromValidated("user2@example.com");

        // Act & Assert
        (email1 != email2).ShouldBeTrue();
    }

    #endregion
}
