
namespace DesigningWithTypes.Tests.Unit;

/// <summary>
/// ContactInfo union 타입 테스트
/// </summary>
[Trait("Sample", "DesigningWithTypes")]
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

    [Fact]
    public void Match_CoversAllCases()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        ContactInfo sut = new ContactInfo.EmailOnly(
            new EmailVerificationState.Unverified(email));

        // Act
        var actual = sut.Match(
            emailOnly: _ => "email",
            postalOnly: _ => "postal",
            emailAndPostal: _ => "both");

        // Assert
        actual.ShouldBe("email");
    }

    [Fact]
    public void Switch_CoversAllCases()
    {
        // Arrange
        var postal = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();
        ContactInfo sut = new ContactInfo.PostalOnly(postal);
        string? actual = null;

        // Act
        sut.Switch(
            emailOnly: _ => actual = "email",
            postalOnly: _ => actual = "postal",
            emailAndPostal: _ => actual = "both");

        // Assert
        actual.ShouldBe("postal");
    }
}
