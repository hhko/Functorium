namespace VerifiedContact.Tests.Unit;

/// <summary>
/// 완전한 Contact 모델 통합 테스트
///
/// 테스트 목적:
/// 1. raw string → 완전한 Contact 생성 성공
/// 2. 잘못된 입력으로 생성 실패
/// </summary>
[Trait("Part2-IllegalStates", "04-VerifiedContact")]
public class ContactTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsValid()
    {
        // Act
        var actual = Contact.Create("HyungHo", "Ko", "user@example.com");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEmailInvalid()
    {
        // Act
        var actual = Contact.Create("HyungHo", "Ko", "not-an-email");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenNameEmpty()
    {
        // Act
        var actual = Contact.Create("", "Ko", "user@example.com");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsContact_WithCorrectContactInfo()
    {
        // Act
        var actual = Contact.Create("HyungHo", "Ko", "user@example.com");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: contact =>
            {
                contact.ContactInfo.ShouldBeOfType<ContactInfo.EmailOnly>();
            },
            Fail: _ => throw new Exception("생성 실패"));
    }
}
