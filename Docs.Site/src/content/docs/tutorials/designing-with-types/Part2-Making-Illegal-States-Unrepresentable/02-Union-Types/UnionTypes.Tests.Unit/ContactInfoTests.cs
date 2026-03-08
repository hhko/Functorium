namespace UnionTypes.Tests.Unit;

/// <summary>
/// Union Type ContactInfo 테스트
///
/// 테스트 목적:
/// 1. 세 가지 유효한 상태만 생성 가능함을 확인
/// 2. switch 식에서 모든 케이스가 처리됨을 확인
/// </summary>
[Trait("Part2-IllegalStates", "02-UnionTypes")]
public class ContactInfoTests
{
    [Fact]
    public void EmailOnly_ContainsEmail()
    {
        // Arrange & Act
        var contact = new ContactInfo.EmailOnly("user@example.com");

        // Assert
        contact.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public void PostalOnly_ContainsAddress()
    {
        // Arrange & Act
        var contact = new ContactInfo.PostalOnly("123 Main St");

        // Assert
        contact.Address.ShouldBe("123 Main St");
    }

    [Fact]
    public void EmailAndPostal_ContainsBoth()
    {
        // Arrange & Act
        var contact = new ContactInfo.EmailAndPostal("user@example.com", "123 Main St");

        // Assert
        contact.Email.ShouldBe("user@example.com");
        contact.Address.ShouldBe("123 Main St");
    }

    [Fact]
    public void Switch_CoversAllCases()
    {
        // Arrange
        ContactInfo contact = new ContactInfo.EmailOnly("test@test.com");

        // Act
        var result = contact switch
        {
            ContactInfo.EmailOnly => "email",
            ContactInfo.PostalOnly => "postal",
            ContactInfo.EmailAndPostal => "both",
            _ => throw new InvalidOperationException()
        };

        // Assert
        result.ShouldBe("email");
    }
}
