using DDDContactExt;
using LanguageExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// ContactEmailCheckService 도메인 서비스 테스트
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
public class ContactEmailCheckServiceTests
{
    private static PersonalName CreateName() =>
        PersonalName.Create("HyungHo", "Ko").ThrowIfFail();

    private static EmailAddress CreateEmail(string email = "user@example.com") =>
        EmailAddress.Create(email).ThrowIfFail();

    private readonly ContactEmailCheckService _sut = new();

    [Fact]
    public void ValidateEmailUnique_ReturnsSuccess_WhenUnique()
    {
        // Arrange
        var contact = Contact.Create(CreateName(), CreateEmail());
        var contacts = Seq.create(contact);
        var newEmail = CreateEmail("new@example.com");

        // Act
        var actual = _sut.ValidateEmailUnique(newEmail, contacts);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ValidateEmailUnique_ReturnsFail_WhenDuplicate()
    {
        // Arrange
        var email = CreateEmail();
        var contact = Contact.Create(CreateName(), email);
        var contacts = Seq.create(contact);

        // Act
        var actual = _sut.ValidateEmailUnique(email, contacts);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ValidateEmailUnique_ExcludesSelf()
    {
        // Arrange
        var email = CreateEmail();
        var contact = Contact.Create(CreateName(), email);
        var contacts = Seq.create(contact);

        // Act
        var actual = _sut.ValidateEmailUnique(email, contacts, contact.Id);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
