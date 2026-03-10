using DDDContactExt;
using LanguageExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// ContactEmailCheckService 도메인 서비스 테스트
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
public class ContactEmailCheckServiceTests
{
    private static readonly DateTime Now = new(2024, 1, 1);

    private static EmailAddress CreateEmail(string email = "user@example.com") =>
        EmailAddress.Create(email).ThrowIfFail();

    private readonly ContactEmailCheckService _sut = new();

    private static (ContactId Id, string? EmailValue) CreateContactData(
        string email = "user@example.com")
    {
        var contact = Contact.Create(
            PersonalName.Create("HyungHo", "Ko").ThrowIfFail(),
            CreateEmail(email),
            Now);
        return (contact.Id, contact.EmailValue);
    }

    [Fact]
    public void ValidateEmailUnique_ReturnsSuccess_WhenUnique()
    {
        // Arrange
        var data = CreateContactData();
        var contacts = Seq.create(data);
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
        var data = CreateContactData();
        var contacts = Seq.create(data);

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
        var data = CreateContactData();
        var contacts = Seq.create(data);

        // Act
        var actual = _sut.ValidateEmailUnique(email, contacts, data.Id);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
