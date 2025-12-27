using ValueObjectComposite.ValueObjects;
using LanguageExt;

namespace ValueObjectComposite.Tests.Unit;

/// <summary>
/// Email 값 객체의 ValueObject (복합 값 객체) 패턴 테스트
///
/// 학습 목표:
/// 1. 비교 불가능한 복합 값 객체 패턴 이해
/// 2. 여러 값 객체 조합의 값 객체 생성 검증
/// 3. 동등성 비교만 제공하는 패턴 확인
/// </summary>
[Trait("Part3-Patterns", "05-ValueObject-Composite")]
public class EmailTests
{
    // 테스트 시나리오: 유효한 이메일로 Email 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenEmailIsValid()
    {
        // Arrange
        string emailAddress = "user@example.com";

        // Act
        Fin<Email> actual = Email.Create(emailAddress);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: email =>
            {
                email.ToString().ShouldBe(emailAddress);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: @ 기호가 없는 이메일 실패
    [Fact]
    public void Create_ReturnsFail_WhenEmailMissingAt()
    {
        // Arrange
        string emailAddress = "userexample.com";

        // Act
        Fin<Email> actual = Email.Create(emailAddress);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 빈 문자열 이메일 실패
    [Fact]
    public void Create_ReturnsFail_WhenEmailIsEmpty()
    {
        // Arrange
        string emailAddress = "";

        // Act
        Fin<Email> actual = Email.Create(emailAddress);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 동일한 이메일의 두 Email은 동등해야 함
    [Fact]
    public void Equals_ReturnsTrue_WhenEmailsAreEqual()
    {
        // Arrange
        var email1 = Email.Create("user@example.com").Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));
        var email2 = Email.Create("user@example.com").Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        email1.Equals(email2).ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 이메일의 두 Email은 동등하지 않아야 함
    [Fact]
    public void Equals_ReturnsFalse_WhenEmailsAreDifferent()
    {
        // Arrange
        var email1 = Email.Create("user1@example.com").Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));
        var email2 = Email.Create("user2@example.com").Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        email1.Equals(email2).ShouldBeFalse();
    }

    // 테스트 시나리오: LocalPart와 Domain이 올바르게 분리됨
    [Fact]
    public void Email_HasCorrectComponents_WhenCreated()
    {
        // Arrange
        string emailAddress = "john.doe@company.com";

        // Act
        var email = Email.Create(emailAddress).Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));

        // Assert - 명시적 형변환을 통해 Value 접근
        ((string)email.LocalPart).ShouldBe("john.doe");
        ((string)email.Domain).ShouldBe("company.com");
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string emailAddress = "user@example.com";

        // Act
        Fin<Email> actual1 = Email.Create(emailAddress);
        Fin<Email> actual2 = Email.Create(emailAddress);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식 반환
    [Fact]
    public void ToString_ReturnsCorrectFormat_WhenEmailIsValid()
    {
        // Arrange
        string emailAddress = "test@domain.org";
        var email = Email.Create(emailAddress).Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        string actual = email.ToString();

        // Assert
        actual.ShouldBe(emailAddress);
    }
}
