using DDDContactExt;
using LanguageExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// Contact Aggregate Root 테스트 (확장)
/// Create, CreateFromValidated, VerifyEmail, UpdateName,
/// AddNote, RemoveNote, Delete, Restore
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
public class ContactTests
{
    private static PersonalName CreateName() =>
        PersonalName.Create("HyungHo", "Ko").ThrowIfFail();

    private static EmailAddress CreateEmail() =>
        EmailAddress.Create("user@example.com").ThrowIfFail();

    private static PostalAddress CreatePostal() =>
        PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

    private static NoteContent CreateNoteContent() =>
        NoteContent.Create("메모 내용").ThrowIfFail();

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

    [Fact]
    public void Create_SetsEmailValue()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreateEmail());

        // Assert
        actual.EmailValue.ShouldBe("user@example.com");
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
    public void Create_SetsEmailValueNull_WithPostalOnly()
    {
        // Act
        var actual = Contact.Create(CreateName(), CreatePostal());

        // Assert
        actual.EmailValue.ShouldBeNull();
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
        var note = ContactNote.Create(CreateNoteContent());

        // Act
        var actual = Contact.CreateFromValidated(
            ContactId.New(),
            CreateName(),
            new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(CreateEmail())),
            [note],
            DateTime.UtcNow,
            Option<DateTime>.None,
            Option<DateTime>.None,
            Option<string>.None);

        // Assert
        actual.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void CreateFromValidated_RestoresNotesAndSoftDelete()
    {
        // Arrange
        var note = ContactNote.Create(CreateNoteContent());
        var deletedAt = new DateTime(2024, 6, 1);

        // Act
        var actual = Contact.CreateFromValidated(
            ContactId.New(),
            CreateName(),
            new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(CreateEmail())),
            [note],
            new DateTime(2024, 1, 1),
            new DateTime(2024, 3, 1),
            deletedAt,
            "admin");

        // Assert
        actual.Notes.Count.ShouldBe(1);
        actual.DeletedAt.IsSome.ShouldBeTrue();
        actual.DeletedBy.IsSome.ShouldBeTrue();
    }

    #endregion

    #region UpdateName

    [Fact]
    public void UpdateName_ReturnsSuccess_WhenNotDeleted()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        var newName = PersonalName.Create("Gildong", "Hong").ThrowIfFail();

        // Act
        var actual = sut.UpdateName(newName);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Name.ShouldBe(newName);
    }

    [Fact]
    public void UpdateName_PublishesNameUpdatedEvent()
    {
        // Arrange
        var oldName = CreateName();
        var sut = Contact.Create(oldName, CreateEmail());
        var newName = PersonalName.Create("Gildong", "Hong").ThrowIfFail();

        // Act
        sut.UpdateName(newName);

        // Assert
        sut.DomainEvents.Count.ShouldBe(2); // CreatedEvent + NameUpdatedEvent
        var evt = sut.DomainEvents[1].ShouldBeOfType<Contact.NameUpdatedEvent>();
        evt.OldName.ShouldBe(oldName);
        evt.NewName.ShouldBe(newName);
    }

    [Fact]
    public void UpdateName_ReturnsFail_WhenDeleted()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");

        // Act
        var actual = sut.UpdateName(PersonalName.Create("Gildong", "Hong").ThrowIfFail());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void UpdateName_SetsUpdatedAt()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.UpdateName(PersonalName.Create("Gildong", "Hong").ThrowIfFail());

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public void VerifyEmail_ReturnsSuccess_WhenUnverified()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        var actual = sut.VerifyEmail(new DateTime(2024, 1, 15));

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void VerifyEmail_TransitionsToVerified()
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
    public void VerifyEmail_PublishesEmailVerifiedEvent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.VerifyEmail(new DateTime(2024, 1, 15));

        // Assert
        sut.DomainEvents.Count.ShouldBe(2);
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
    public void VerifyEmail_ReturnsFail_WhenDeleted()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");

        // Act
        var actual = sut.VerifyEmail(DateTime.UtcNow);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void VerifyEmail_SetsUpdatedAt()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.VerifyEmail(new DateTime(2024, 1, 15));

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
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

    #region AddNote

    [Fact]
    public void AddNote_ReturnsSuccess_WhenNotDeleted()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        var actual = sut.AddNote(CreateNoteContent());

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Notes.Count.ShouldBe(1);
    }

    [Fact]
    public void AddNote_PublishesNoteAddedEvent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.AddNote(CreateNoteContent());

        // Assert
        sut.DomainEvents.Count.ShouldBe(2); // CreatedEvent + NoteAddedEvent
        sut.DomainEvents[1].ShouldBeOfType<Contact.NoteAddedEvent>();
    }

    [Fact]
    public void AddNote_ReturnsFail_WhenDeleted()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");

        // Act
        var actual = sut.AddNote(CreateNoteContent());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void AddNote_SetsUpdatedAt()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.AddNote(CreateNoteContent());

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }

    #endregion

    #region RemoveNote

    [Fact]
    public void RemoveNote_RemovesExistingNote()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.AddNote(CreateNoteContent());
        var noteId = sut.Notes[0].Id;

        // Act
        sut.RemoveNote(noteId);

        // Assert
        sut.Notes.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveNote_PublishesNoteRemovedEvent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.AddNote(CreateNoteContent());
        var noteId = sut.Notes[0].Id;

        // Act
        sut.RemoveNote(noteId);

        // Assert
        sut.DomainEvents.Last().ShouldBeOfType<Contact.NoteRemovedEvent>();
    }

    [Fact]
    public void RemoveNote_IsIdempotent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.AddNote(CreateNoteContent());
        var noteId = sut.Notes[0].Id;
        sut.RemoveNote(noteId);
        var eventCount = sut.DomainEvents.Count;

        // Act
        sut.RemoveNote(noteId);

        // Assert
        sut.DomainEvents.Count.ShouldBe(eventCount);
    }

    [Fact]
    public void RemoveNote_ReturnsFail_WhenDeleted()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.AddNote(CreateNoteContent());
        var noteId = sut.Notes[0].Id;
        sut.Delete("admin");

        // Act
        var actual = sut.RemoveNote(noteId);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void RemoveNote_SetsUpdatedAt()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.AddNote(CreateNoteContent());
        var noteId = sut.Notes[0].Id;

        // Act
        sut.RemoveNote(noteId);

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }

    #endregion

    #region Delete

    [Fact]
    public void Delete_SetsSoftDeleteProperties()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.Delete("admin");

        // Assert
        sut.DeletedAt.IsSome.ShouldBeTrue();
        sut.DeletedBy.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void Delete_PublishesDeletedEvent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());

        // Act
        sut.Delete("admin");

        // Assert
        sut.DomainEvents.Last().ShouldBeOfType<Contact.DeletedEvent>();
    }

    [Fact]
    public void Delete_IsIdempotent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");
        var eventCount = sut.DomainEvents.Count;

        // Act
        sut.Delete("admin");

        // Assert
        sut.DomainEvents.Count.ShouldBe(eventCount);
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_ClearsSoftDeleteProperties()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");

        // Act
        sut.Restore();

        // Assert
        sut.DeletedAt.IsNone.ShouldBeTrue();
        sut.DeletedBy.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Restore_PublishesRestoredEvent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");

        // Act
        sut.Restore();

        // Assert
        sut.DomainEvents.Last().ShouldBeOfType<Contact.RestoredEvent>();
    }

    [Fact]
    public void Restore_IsIdempotent()
    {
        // Arrange
        var sut = Contact.Create(CreateName(), CreateEmail());
        sut.Delete("admin");
        sut.Restore();
        var eventCount = sut.DomainEvents.Count;

        // Act
        sut.Restore();

        // Assert
        sut.DomainEvents.Count.ShouldBe(eventCount);
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
