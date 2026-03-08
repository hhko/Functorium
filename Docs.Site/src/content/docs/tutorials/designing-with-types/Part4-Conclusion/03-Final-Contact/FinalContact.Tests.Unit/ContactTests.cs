using FinalContact;

namespace FinalContact.Tests.Unit;

/// <summary>
/// 최종 Contact 모델 통합 테스트
///
/// 테스트 목적:
/// 1. 3가지 팩토리(email only, postal only, both) 생성 성공
/// 2. 무효 입력으로 생성 실패
/// 3. 이메일 초기 상태가 Unverified인지 확인
/// </summary>
[Trait("Part4-Conclusion", "03-FinalContact")]
public class ContactTests
{
    [Fact]
    public void Create_ReturnsSuccess_WithEmailOnly()
    {
        // Act
        var actual = Contact.Create("HyungHo", "Ko", "user@example.com");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: contact => contact.ContactInfo.ShouldBeOfType<ContactInfo.EmailOnly>(),
            Fail: _ => throw new Exception("생성 실패"));
    }

    [Fact]
    public void CreateWithPostal_ReturnsSuccess_WithPostalOnly()
    {
        // Act
        var actual = Contact.CreateWithPostal("Jane", "Doe", "456 Oak Ave", "Chicago", "IL", "60601");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: contact => contact.ContactInfo.ShouldBeOfType<ContactInfo.PostalOnly>(),
            Fail: _ => throw new Exception("생성 실패"));
    }

    [Fact]
    public void CreateWithEmailAndPostal_ReturnsSuccess_WithBoth()
    {
        // Act
        var actual = Contact.CreateWithEmailAndPostal(
            "Bob", "Smith", "bob@example.com",
            "789 Pine Rd", "Springfield", "IL", "62701");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: contact => contact.ContactInfo.ShouldBeOfType<ContactInfo.EmailAndPostal>(),
            Fail: _ => throw new Exception("생성 실패"));
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
    public void CreateWithPostal_ReturnsFail_WhenZipInvalid()
    {
        // Act
        var actual = Contact.CreateWithPostal("Jane", "Doe", "456 Oak Ave", "Chicago", "IL", "bad");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_InitialEmailState_IsUnverified()
    {
        // Act
        var actual = Contact.Create("HyungHo", "Ko", "user@example.com");

        // Assert
        actual.Match(
            Succ: contact =>
            {
                var emailOnly = (ContactInfo.EmailOnly)contact.ContactInfo;
                emailOnly.EmailState.ShouldBeOfType<EmailVerificationState.Unverified>();
            },
            Fail: _ => throw new Exception("생성 실패"));
    }

    [Fact]
    public void Create_SupportsMiddleInitial()
    {
        // Act
        var actual = Contact.Create("HyungHo", "Ko", "user@example.com", "J");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: contact => contact.Name.MiddleInitial.ShouldBe("J"),
            Fail: _ => throw new Exception("생성 실패"));
    }
}
