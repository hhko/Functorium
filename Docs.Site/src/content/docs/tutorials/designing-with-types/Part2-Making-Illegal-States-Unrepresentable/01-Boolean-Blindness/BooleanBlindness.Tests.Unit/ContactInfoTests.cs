namespace BooleanBlindness.Tests.Unit;

/// <summary>
/// Boolean Blindness — 불법 상태 생성 가능 증명
///
/// 테스트 목적:
/// 1. "둘 다 없는" 불법 상태가 실제로 생성 가능함을 증명
/// 2. 런타임 검증에 의존해야 함을 시연
/// </summary>
[Trait("Part2-IllegalStates", "01-BooleanBlindness")]
public class ContactInfoTests
{
    [Fact]
    public void ContactInfo_AllowsIllegalState_WhenBothAreNull()
    {
        // Act — 불법 상태: 이메일도 우편 주소도 없음
        var illegal = new ContactInfo();

        // Assert — 컴파일러가 이를 허용함
        illegal.EmailAddress.ShouldBeNull();
        illegal.PostalAddress.ShouldBeNull();
        illegal.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void ContactInfo_IsValid_WhenEmailOnly()
    {
        // Act
        var contact = new ContactInfo { EmailAddress = "user@example.com" };

        // Assert
        contact.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void ContactInfo_IsValid_WhenPostalOnly()
    {
        // Act
        var contact = new ContactInfo { PostalAddress = "123 Main St" };

        // Assert
        contact.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void ContactInfo_IsValid_WhenBothPresent()
    {
        // Act
        var contact = new ContactInfo
        {
            EmailAddress = "user@example.com",
            PostalAddress = "123 Main St"
        };

        // Assert
        contact.IsValid().ShouldBeTrue();
    }
}
