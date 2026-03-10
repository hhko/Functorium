using DDDContactExt;
using LanguageExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// Contact Specification 테스트
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
public class ContactSpecificationTests
{
    private static PersonalName CreateName() =>
        PersonalName.Create("HyungHo", "Ko").ThrowIfFail();

    private static EmailAddress CreateEmail(string email = "user@example.com") =>
        EmailAddress.Create(email).ThrowIfFail();

    #region ContactEmailSpec

    [Fact]
    public void ContactEmailSpec_IsSatisfiedBy_ReturnsTrue_WhenEmailMatches()
    {
        // Arrange
        var email = CreateEmail();
        var contact = Contact.Create(CreateName(), email);
        var spec = new ContactEmailSpec(email);

        // Act
        var actual = spec.IsSatisfiedBy(contact);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void ContactEmailSpec_IsSatisfiedBy_ReturnsFalse_WhenEmailDiffers()
    {
        // Arrange
        var contact = Contact.Create(CreateName(), CreateEmail());
        var spec = new ContactEmailSpec(CreateEmail("other@example.com"));

        // Act
        var actual = spec.IsSatisfiedBy(contact);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void ContactEmailSpec_IsSatisfiedBy_ReturnsFalse_WhenNoEmail()
    {
        // Arrange
        var contact = Contact.Create(CreateName(),
            PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail());
        var spec = new ContactEmailSpec(CreateEmail());

        // Act
        var actual = spec.IsSatisfiedBy(contact);

        // Assert
        actual.ShouldBeFalse();
    }

    #endregion

    #region ContactEmailUniqueSpec

    [Fact]
    public void ContactEmailUniqueSpec_IsSatisfiedBy_ReturnsTrue_WhenEmailMatches()
    {
        // Arrange
        var email = CreateEmail();
        var contact = Contact.Create(CreateName(), email);
        var spec = new ContactEmailUniqueSpec(email);

        // Act
        var actual = spec.IsSatisfiedBy(contact);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void ContactEmailUniqueSpec_WithExcludeId_ReturnsFalse_WhenSelf()
    {
        // Arrange
        var email = CreateEmail();
        var contact = Contact.Create(CreateName(), email);
        var spec = new ContactEmailUniqueSpec(email, contact.Id);

        // Act
        var actual = spec.IsSatisfiedBy(contact);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void ContactEmailUniqueSpec_WithExcludeId_ReturnsTrue_WhenOther()
    {
        // Arrange
        var email = CreateEmail();
        var contact = Contact.Create(CreateName(), email);
        var otherId = ContactId.New();
        var spec = new ContactEmailUniqueSpec(email, otherId);

        // Act
        var actual = spec.IsSatisfiedBy(contact);

        // Assert
        actual.ShouldBeTrue();
    }

    #endregion
}
