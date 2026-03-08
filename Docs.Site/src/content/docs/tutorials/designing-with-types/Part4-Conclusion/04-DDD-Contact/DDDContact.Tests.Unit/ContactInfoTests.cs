using DDDContact;

namespace DDDContact.Tests.Unit;

/// <summary>
/// ContactInfo union 타입 테스트
///
/// 테스트 목적:
/// 1. 3가지 union case 생성
/// 2. 패턴 매칭 완전성 확인
/// </summary>
[Trait("Part4-Conclusion", "04-DDDContact")]
public class ContactInfoTests
{
    [Fact]
    public void EmailOnly_CreatesWithEmailState()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        var state = new EmailVerificationState.Unverified(email);

        // Act
        ContactInfo actual = new ContactInfo.EmailOnly(state);

        // Assert
        actual.ShouldBeOfType<ContactInfo.EmailOnly>();
    }

    [Fact]
    public void PostalOnly_CreatesWithAddress()
    {
        // Arrange
        var postal = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

        // Act
        ContactInfo actual = new ContactInfo.PostalOnly(postal);

        // Assert
        actual.ShouldBeOfType<ContactInfo.PostalOnly>();
    }

    [Fact]
    public void EmailAndPostal_CreatesWithBoth()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        var state = new EmailVerificationState.Unverified(email);
        var postal = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

        // Act
        ContactInfo actual = new ContactInfo.EmailAndPostal(state, postal);

        // Assert
        actual.ShouldBeOfType<ContactInfo.EmailAndPostal>();
    }

    [Fact]
    public void PatternMatch_CoversAllCases()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        var postal = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

        ContactInfo[] cases =
        [
            new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(email)),
            new ContactInfo.PostalOnly(postal),
            new ContactInfo.EmailAndPostal(new EmailVerificationState.Unverified(email), postal),
        ];

        // Act & Assert
        foreach (var contactInfo in cases)
        {
            var actual = contactInfo switch
            {
                ContactInfo.EmailOnly => "email",
                ContactInfo.PostalOnly => "postal",
                ContactInfo.EmailAndPostal => "both",
                _ => throw new InvalidOperationException()
            };
            actual.ShouldNotBeNullOrEmpty();
        }
    }
}
