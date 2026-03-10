using static LanguageExt.Prelude;

namespace DDDContactExt;

/// <summary>
/// DDD 패턴이 적용된 Contact Aggregate Root (확장)
/// Entity ID, 도메인 이벤트, 행위 메서드, 이중 팩토리, Soft Delete,
/// Child Entity(Notes), Collection Management, 투영 속성(EmailValue)
/// </summary>
[GenerateEntityId]
public sealed class Contact : AggregateRoot<ContactId>, IAuditable, ISoftDeletableWithUser
{
    #region Error Types

    public sealed record NoEmailToVerify : DomainErrorType.Custom;
    public sealed record AlreadyDeleted : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent(
        ContactId ContactId,
        PersonalName Name,
        ContactInfo ContactInfo) : DomainEvent;

    public sealed record NameUpdatedEvent(
        ContactId ContactId,
        PersonalName OldName,
        PersonalName NewName) : DomainEvent;

    public sealed record EmailVerifiedEvent(
        ContactId ContactId,
        EmailAddress Email,
        DateTime VerifiedAt) : DomainEvent;

    public sealed record NoteAddedEvent(
        ContactId ContactId,
        ContactNoteId NoteId,
        NoteContent Content) : DomainEvent;

    public sealed record NoteRemovedEvent(
        ContactId ContactId,
        ContactNoteId NoteId) : DomainEvent;

    public sealed record DeletedEvent(
        ContactId ContactId,
        string DeletedBy) : DomainEvent;

    public sealed record RestoredEvent(
        ContactId ContactId) : DomainEvent;

    #endregion

    // Value Object 속성
    public PersonalName Name { get; private set; }

    // ContactInfo 설정 시 EmailValue 자동 동기화
    private ContactInfo _contactInfo = null!;
    public ContactInfo ContactInfo
    {
        get => _contactInfo;
        private set
        {
            _contactInfo = value;
            EmailValue = ExtractEmail(value);
        }
    }

    // 이메일 투영 속성 (Specification 지원용)
    public string? EmailValue { get; private set; }

    // 메모 컬렉션 (자식 엔티티)
    private readonly List<ContactNote> _notes = [];
    public IReadOnlyList<ContactNote> Notes => _notes.AsReadOnly();

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // Soft Delete 속성
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Contact(
        ContactId id,
        PersonalName name,
        ContactInfo contactInfo,
        DateTime createdAt)
        : base(id)
    {
        Name = name;
        ContactInfo = contactInfo;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Create: 이메일만 있는 Contact (초기 상태: Unverified)
    /// </summary>
    public static Contact Create(PersonalName name, EmailAddress email, DateTime createdAt)
    {
        var contactInfo = new ContactInfo.EmailOnly(
            new EmailVerificationState.Unverified(email));
        var contact = new Contact(ContactId.New(), name, contactInfo, createdAt);
        contact.AddDomainEvent(new CreatedEvent(contact.Id, name, contactInfo));
        return contact;
    }

    /// <summary>
    /// Create: 우편 주소만 있는 Contact
    /// </summary>
    public static Contact Create(PersonalName name, PostalAddress postal, DateTime createdAt)
    {
        var contactInfo = new ContactInfo.PostalOnly(postal);
        var contact = new Contact(ContactId.New(), name, contactInfo, createdAt);
        contact.AddDomainEvent(new CreatedEvent(contact.Id, name, contactInfo));
        return contact;
    }

    /// <summary>
    /// Create: 이메일 + 우편 주소 모두 있는 Contact (이메일 초기 상태: Unverified)
    /// </summary>
    public static Contact Create(PersonalName name, EmailAddress email, PostalAddress postal, DateTime createdAt)
    {
        var contactInfo = new ContactInfo.EmailAndPostal(
            new EmailVerificationState.Unverified(email), postal);
        var contact = new Contact(ContactId.New(), name, contactInfo, createdAt);
        contact.AddDomainEvent(new CreatedEvent(contact.Id, name, contactInfo));
        return contact;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음, 이벤트 없음)
    /// </summary>
    public static Contact CreateFromValidated(
        ContactId id,
        PersonalName name,
        ContactInfo contactInfo,
        IEnumerable<ContactNote> notes,
        DateTime createdAt,
        Option<DateTime> updatedAt,
        Option<DateTime> deletedAt,
        Option<string> deletedBy)
    {
        var contact = new Contact(id, name, contactInfo, createdAt)
        {
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy
        };
        contact._notes.AddRange(notes);
        return contact;
    }

    /// <summary>
    /// 이름을 변경합니다.
    /// </summary>
    public Fin<Unit> UpdateName(PersonalName newName, DateTime now)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Contact>(
                new AlreadyDeleted(),
                Id.ToString(),
                "삭제된 연락처는 수정할 수 없습니다");

        var oldName = Name;
        Name = newName;
        UpdatedAt = now;
        AddDomainEvent(new NameUpdatedEvent(Id, oldName, newName));
        return unit;
    }

    /// <summary>
    /// 이메일 인증: Unverified → Verified 전이
    /// Aggregate가 가드 후 상태 전이를 EmailVerificationState에 위임
    /// </summary>
    public Fin<Unit> VerifyEmail(DateTime verifiedAt)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Contact>(
                new AlreadyDeleted(),
                Id.ToString(),
                "삭제된 연락처의 이메일을 인증할 수 없습니다");

        // ContactInfo에서 EmailState 추출
        var emailState = ContactInfo switch
        {
            ContactInfo.EmailOnly eo => (EmailVerificationState?)eo.EmailState,
            ContactInfo.EmailAndPostal ep => ep.EmailState,
            _ => null
        };

        if (emailState is null)
            return DomainError.For<Contact>(
                new NoEmailToVerify(),
                Id.ToString(),
                "이메일이 없는 연락처입니다");

        // 상태 전이를 EmailVerificationState에 위임
        return emailState.Verify(verifiedAt).Map(verified =>
        {
            ContactInfo = ContactInfo switch
            {
                ContactInfo.EmailOnly => new ContactInfo.EmailOnly(verified),
                ContactInfo.EmailAndPostal ep => new ContactInfo.EmailAndPostal(verified, ep.Address),
                _ => throw new InvalidOperationException()
            };
            UpdatedAt = verifiedAt;
            AddDomainEvent(new EmailVerifiedEvent(Id, verified.Email, verifiedAt));
            return unit;
        });
    }

    /// <summary>
    /// 메모를 추가합니다.
    /// </summary>
    public Fin<Unit> AddNote(NoteContent content, DateTime now)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Contact>(
                new AlreadyDeleted(),
                Id.ToString(),
                "삭제된 연락처에 메모를 추가할 수 없습니다");

        var note = ContactNote.Create(content, now);
        _notes.Add(note);
        UpdatedAt = now;
        AddDomainEvent(new NoteAddedEvent(Id, note.Id, content));
        return unit;
    }

    /// <summary>
    /// 메모를 제거합니다. (삭제된 Contact 차단, 존재하지 않는 Note는 멱등)
    /// </summary>
    public Fin<Unit> RemoveNote(ContactNoteId noteId, DateTime now)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Contact>(
                new AlreadyDeleted(),
                Id.ToString(),
                "삭제된 연락처에서 메모를 제거할 수 없습니다");

        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note is null)
            return unit;

        _notes.Remove(note);
        UpdatedAt = now;
        AddDomainEvent(new NoteRemovedEvent(Id, noteId));
        return unit;
    }

    /// <summary>
    /// 연락처를 삭제합니다. (멱등성 보장)
    /// </summary>
    public Contact Delete(string deletedBy, DateTime now)
    {
        if (DeletedAt.IsSome)
            return this;

        DeletedAt = now;
        DeletedBy = deletedBy;
        AddDomainEvent(new DeletedEvent(Id, deletedBy));
        return this;
    }

    /// <summary>
    /// 삭제된 연락처를 복원합니다. (멱등성 보장)
    /// </summary>
    public Contact Restore()
    {
        if (DeletedAt.IsNone)
            return this;

        DeletedAt = Option<DateTime>.None;
        DeletedBy = Option<string>.None;
        AddDomainEvent(new RestoredEvent(Id));
        return this;
    }

    public override string ToString() => $"{Name} ({ContactInfo})";

    // 헬퍼: ContactInfo에서 이메일 문자열 추출
    private static string? ExtractEmail(ContactInfo contactInfo) => contactInfo switch
    {
        ContactInfo.EmailOnly eo => GetEmailString(eo.EmailState),
        ContactInfo.EmailAndPostal ep => GetEmailString(ep.EmailState),
        _ => null
    };

    private static string? GetEmailString(EmailVerificationState state) => state switch
    {
        EmailVerificationState.Unverified u => (string)u.Email,
        EmailVerificationState.Verified v => (string)v.Email,
        _ => null
    };
}
