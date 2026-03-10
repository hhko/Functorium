using System.Linq.Expressions;

namespace DDDContactExt;

/// <summary>
/// 이메일 고유성 확인 Specification
/// ExcludeId가 지정되면 해당 Contact는 제외하고 검사합니다 (업데이트 시 자기 자신 제외).
/// </summary>
public sealed class ContactEmailUniqueSpec : ExpressionSpecification<Contact>
{
    public EmailAddress Email { get; }
    public Option<ContactId> ExcludeId { get; }

    public ContactEmailUniqueSpec(EmailAddress email, Option<ContactId> excludeId = default)
    {
        Email = email;
        ExcludeId = excludeId;
    }

    public override Expression<Func<Contact, bool>> ToExpression()
    {
        string emailStr = Email;
        string? excludeIdStr = ExcludeId.Match<string?>(id => id.ToString(), () => null);
        return contact => contact.EmailValue == emailStr &&
                          (excludeIdStr == null || contact.Id.ToString() != excludeIdStr);
    }
}
