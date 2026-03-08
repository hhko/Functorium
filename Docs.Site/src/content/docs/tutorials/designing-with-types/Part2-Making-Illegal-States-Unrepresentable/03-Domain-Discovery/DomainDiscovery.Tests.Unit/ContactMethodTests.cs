namespace DomainDiscovery.Tests.Unit;

/// <summary>
/// ContactMethod 도메인 발견 테스트
///
/// 테스트 목적:
/// 1. 각 ContactMethod의 패턴 매칭 동작 확인
/// 2. 모든 케이스가 처리됨을 확인
/// </summary>
[Trait("Part2-IllegalStates", "03-DomainDiscovery")]
public class ContactMethodTests
{
    [Fact]
    public void Describe_ReturnsEmailDescription_WhenEmail()
    {
        // Arrange
        ContactMethod method = new ContactMethod.Email("user@example.com");

        // Act
        var actual = ContactMethodHandler.Describe(method);

        // Assert
        actual.ShouldContain("이메일");
        actual.ShouldContain("user@example.com");
    }

    [Fact]
    public void Describe_ReturnsPostalDescription_WhenPostalMail()
    {
        // Arrange
        ContactMethod method = new ContactMethod.PostalMail("123 Main St");

        // Act
        var actual = ContactMethodHandler.Describe(method);

        // Assert
        actual.ShouldContain("우편");
    }

    [Fact]
    public void Describe_ReturnsPhoneDescription_WhenPhone()
    {
        // Arrange
        ContactMethod method = new ContactMethod.Phone("010-1234-5678");

        // Act
        var actual = ContactMethodHandler.Describe(method);

        // Assert
        actual.ShouldContain("전화");
    }
}
