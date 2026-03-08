using DDDContact;

namespace DDDContact.Tests.Unit;

/// <summary>
/// Contact Aggregate Root 테스트
///
/// 테스트 목적:
/// 1. 3가지 Create 팩토리 + 도메인 이벤트 발행
/// 2. VerifyEmail 행위 메서드 (성공/실패)
/// 3. CreateFromValidated 이벤트 미발행
/// </summary>
[Trait("Part4-Conclusion", "04-DDDContact")]
public class ContactTests
{
    private static PersonalName CreateName() =>
        PersonalName.Create("HyungHo", "Ko").ThrowIfFail();

    private static EmailAddress CreateEmail() =>
        EmailAddress.Create("user@example.com").ThrowIfFail();

    private static PostalAddress CreatePostal() =>
        PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

    #region Create — Email Only

    [Fact]
    public void Create_ReturnsContact_WithEmailOnly()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreateEmail());

        // Assert
        actual.ContactInfo.ShouldBeOfType<ContactInfo.EmailOnly>();
    }

    [Fact]
    public void Create_PublishesCreatedEvent_WithEmailOnly()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreateEmail());

        // Assert
        actual.DomainEvents.Count.ShouldBe(1);
        actual.DomainEvents[0].ShouldBeOfType<Contact.CreatedEvent>();
    }

    [Fact]
    public void Create_SetsInitialEmailState_ToUnverified()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreateEmail());

        // Assert
        var emailOnly = (ContactInfo.EmailOnly)actual.ContactInfo;
        emailOnly.EmailState.ShouldBeOfType<EmailVerificationState.Unverified>();
    }

    #endregion

    #region Create — Postal Only

    [Fact]
    public void Create_ReturnsContact_WithPostalOnly()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreatePostal());

        // Assert
        actual.ContactInfo.ShouldBeOfType<ContactInfo.PostalOnly>();
    }

    [Fact]
    public void Create_PublishesCreatedEvent_WithPostalOnly()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreatePostal());

        // Assert
        actual.DomainEvents.Count.ShouldBe(1);
        actual.DomainEvents[0].ShouldBeOfType<Contact.CreatedEvent>();
    }

    #endregion

    #region Create — Email And Postal

    [Fact]
    public void Create_ReturnsContact_WithEmailAndPostal()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreateEmail(), CreatePostal());

        // Assert
        actual.ContactInfo.ShouldBeOfType<ContactInfo.EmailAndPostal>();
    }

    [Fact]
    public void Create_PublishesCreatedEvent_WithEmailAndPostal()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreateEmail(), CreatePostal());

        // Assert
        actual.DomainEvents.Count.ShouldBe(1);
        actual.DomainEvents[0].ShouldBeOfType<Contact.CreatedEvent>();
    }

    #endregion

    #region CreateFromValidated

    [Fact]
    public void CreateFromValidated_DoesNotPublishEvents()
    {
        // Arrange
        var name = CreateName();
        var email = CreateEmail();
        var contactInfo = new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(email));

        // Act
        var actual = Contact.CreateFromValidated(
            ContactId.New(),
            name,
            contactInfo,
            DateTime.UtcNow,
            LanguageExt.Option<DateTime>.None);

        // Assert
        actual.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void CreateFromValidated_RestoresAuditProperties()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);
        var updatedAt = new DateTime(2024, 6, 15);

        // Act
        var actual = Contact.CreateFromValidated(
            ContactId.New(),
            CreateName(),
            new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(CreateEmail())),
            createdAt,
            updatedAt);

        // Assert
        actual.CreatedAt.ShouldBe(createdAt);
        actual.UpdatedAt.IsSome.ShouldBeTrue();
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public void VerifyEmail_ReturnsSuccess_WhenUnverified()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        var verifiedAt = new DateTime(2024, 1, 15);

        // Act
        var actual = sut.VerifyEmail(verifiedAt);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void VerifyEmail_TransitionsToVerified_WhenUnverified()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.VerifyEmail(new DateTime(2024, 1, 15));

        // Assert
        var emailOnly = (ContactInfo.EmailOnly)sut.ContactInfo;
        var verified = emailOnly.EmailState.ShouldBeOfType<EmailVerificationState.Verified>();
        verified.VerifiedAt.ShouldBe(new DateTime(2024, 1, 15));
    }

    [Fact]
    public void VerifyEmail_PublishesEmailVerifiedEvent_WhenUnverified()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.VerifyEmail(new DateTime(2024, 1, 15));

        // Assert
        sut.DomainEvents.Count.ShouldBe(2); // CreatedEvent + EmailVerifiedEvent
        sut.DomainEvents[1].ShouldBeOfType<Contact.EmailVerifiedEvent>();
    }

    [Fact]
    public void VerifyEmail_ReturnsFail_WhenAlreadyVerified()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.VerifyEmail(DateTime.UtcNow);

        // Act
        var actual = sut.VerifyEmail(DateTime.UtcNow);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void VerifyEmail_ReturnsFail_WhenNoEmail()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreatePostal());

        // Act
        var actual = sut.VerifyEmail(DateTime.UtcNow);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void VerifyEmail_WorksWithEmailAndPostal()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail(), CreatePostal());

        // Act
        var actual = sut.VerifyEmail(new DateTime(2024, 1, 15));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var ep = (ContactInfo.EmailAndPostal)sut.ContactInfo;
        ep.EmailState.ShouldBeOfType<EmailVerificationState.Verified>();
    }

    #endregion

    #region Entity ID

    [Fact]
    public void Create_AssignsUniqueId()
    {
        // Act
        var contact1 = Contact.Create(CreateName(), CreateEmail());
        var contact2 = Contact.Create(CreateName(), CreateEmail());

        // Assert
        contact1.Id.ShouldNotBe(contact2.Id);
    }

    #endregion
}
