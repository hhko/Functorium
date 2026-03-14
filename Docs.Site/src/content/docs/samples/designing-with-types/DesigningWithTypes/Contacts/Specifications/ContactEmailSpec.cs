using System.Linq.Expressions;

namespace DesigningWithTypes.Contacts.Specifications;

/// <summary>
/// 이메일로 Contact를 검색하는 Specification
/// </summary>
public sealed class ContactEmailSpec : ExpressionSpecification<Contact>
{
    public EmailAddress Email { get; }

    public ContactEmailSpec(EmailAddress email) => Email = email;

    public override Expression<Func<Contact, bool>> ToExpression()
    {
        string emailStr = Email;
        return contact => contact.EmailValue == emailStr;
    }
}
