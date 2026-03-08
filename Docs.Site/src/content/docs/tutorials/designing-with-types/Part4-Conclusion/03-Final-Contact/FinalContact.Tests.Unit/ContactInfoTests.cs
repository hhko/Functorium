using FinalContact;

namespace FinalContact.Tests.Unit;

/// <summary>
/// ContactInfo union 타입 테스트
///
/// 테스트 목적:
/// 1. 3가지 union 케이스 구성
/// 2. 패턴 매칭 확인
/// </summary>
[Trait("Part4-Conclusion", "03-FinalContact")]
public class ContactInfoTests
{
    [Fact]
    public void EmailOnly_ContainsEmailVerificationState()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com")
            .Match(Succ: v => v, Fail: _ => throw new Exception("EmailAddress 생성 실패"));
        var emailState = new EmailVerificationState.Unverified(email);

        // Act
        ContactInfo actual = new ContactInfo.EmailOnly(emailState);

        // Assert
        actual.ShouldBeOfType<ContactInfo.EmailOnly>();
        var emailOnly = (ContactInfo.EmailOnly)actual;
        emailOnly.EmailState.ShouldBeOfType<EmailVerificationState.Unverified>();
    }

    [Fact]
    public void PostalOnly_ContainsPostalAddress()
    {
        // Arrange
        var postal = PostalAddress.Create("123 Main St", "Springfield", "IL", "62701")
            .Match(Succ: v => v, Fail: _ => throw new Exception("PostalAddress 생성 실패"));

        // Act
        ContactInfo actual = new ContactInfo.PostalOnly(postal);

        // Assert
        actual.ShouldBeOfType<ContactInfo.PostalOnly>();
    }

    [Fact]
    public void EmailAndPostal_ContainsBoth()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com")
            .Match(Succ: v => v, Fail: _ => throw new Exception("EmailAddress 생성 실패"));
        var emailState = new EmailVerificationState.Unverified(email);
        var postal = PostalAddress.Create("123 Main St", "Springfield", "IL", "62701")
            .Match(Succ: v => v, Fail: _ => throw new Exception("PostalAddress 생성 실패"));

        // Act
        ContactInfo actual = new ContactInfo.EmailAndPostal(emailState, postal);

        // Assert
        actual.ShouldBeOfType<ContactInfo.EmailAndPostal>();
    }

    [Fact]
    public void PatternMatching_CoversAllCases()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com")
            .Match(Succ: v => v, Fail: _ => throw new Exception("EmailAddress 생성 실패"));
        var emailState = new EmailVerificationState.Unverified(email);
        ContactInfo contactInfo = new ContactInfo.EmailOnly(emailState);

        // Act
        var description = contactInfo switch
        {
            ContactInfo.EmailOnly eo => $"email: {eo.EmailState}",
            ContactInfo.PostalOnly po => $"postal: {po.Address}",
            ContactInfo.EmailAndPostal ep => $"both: {ep.EmailState}, {ep.Address}",
            _ => throw new InvalidOperationException()
        };

        // Assert
        description.ShouldStartWith("email:");
    }
}
