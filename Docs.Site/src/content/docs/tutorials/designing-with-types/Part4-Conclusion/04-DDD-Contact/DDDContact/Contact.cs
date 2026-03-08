using static LanguageExt.Prelude;

namespace DDDContact;

/// <summary>
/// DDD 패턴이 적용된 Contact Aggregate Root
/// Entity ID, 도메인 이벤트, 행위 메서드, 이중 팩토리 패턴
/// </summary>
[GenerateEntityId]
public sealed class Contact : AggregateRoot<ContactId>, IAuditable
{
    #region Error Types

    public sealed record AlreadyVerified : DomainErrorType.Custom;
    public sealed record NoEmailToVerify : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent(
        ContactId ContactId,
        PersonalName Name,
        ContactInfo ContactInfo) : DomainEvent;

    public sealed record EmailVerifiedEvent(
        ContactId ContactId,
        EmailAddress Email,
        DateTime VerifiedAt) : DomainEvent;

    #endregion

    // Value Object 속성
    public PersonalName Name { get; private set; }
    public ContactInfo ContactInfo { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Contact(
        ContactId id,
        PersonalName name,
        ContactInfo contactInfo)
        : base(id)
    {
        Name = name;
        ContactInfo = contactInfo;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이메일만 있는 Contact (초기 상태: Unverified)
    /// </summary>
    public static Contact Create(PersonalName name, EmailAddress email)
    {
        var contactInfo = new ContactInfo.EmailOnly(
            new EmailVerificationState.Unverified(email));
        var contact = new Contact(ContactId.New(), name, contactInfo);
        contact.AddDomainEvent(new CreatedEvent(contact.Id, name, contactInfo));
        return contact;
    }

    /// <summary>
    /// Create: 우편 주소만 있는 Contact
    /// </summary>
    public static Contact Create(PersonalName name, PostalAddress postal)
    {
        var contactInfo = new ContactInfo.PostalOnly(postal);
        var contact = new Contact(ContactId.New(), name, contactInfo);
        contact.AddDomainEvent(new CreatedEvent(contact.Id, name, contactInfo));
        return contact;
    }

    /// <summary>
    /// Create: 이메일 + 우편 주소 모두 있는 Contact (이메일 초기 상태: Unverified)
    /// </summary>
    public static Contact Create(PersonalName name, EmailAddress email, PostalAddress postal)
    {
        var contactInfo = new ContactInfo.EmailAndPostal(
            new EmailVerificationState.Unverified(email), postal);
        var contact = new Contact(ContactId.New(), name, contactInfo);
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
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        return new Contact(id, name, contactInfo)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// 이메일 인증: Unverified → Verified 전이
    /// Aggregate가 상태 전이를 소유
    /// </summary>
    public Fin<Unit> VerifyEmail(DateTime verifiedAt)
    {
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

        if (emailState is EmailVerificationState.Verified)
            return DomainError.For<Contact>(
                new AlreadyVerified(),
                Id.ToString(),
                "이미 인증된 이메일입니다");

        var unverified = (EmailVerificationState.Unverified)emailState;
        var verified = new EmailVerificationState.Verified(unverified.Email, verifiedAt);

        ContactInfo = ContactInfo switch
        {
            ContactInfo.EmailOnly => new ContactInfo.EmailOnly(verified),
            ContactInfo.EmailAndPostal ep => new ContactInfo.EmailAndPostal(verified, ep.Address),
            _ => throw new InvalidOperationException()
        };

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new EmailVerifiedEvent(Id, unverified.Email, verifiedAt));
        return unit;
    }

    public override string ToString() => $"{Name} ({ContactInfo})";
}
