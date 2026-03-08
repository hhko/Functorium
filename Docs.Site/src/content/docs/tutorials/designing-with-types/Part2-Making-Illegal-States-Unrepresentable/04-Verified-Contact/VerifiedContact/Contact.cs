using LanguageExt;

namespace VerifiedContact;

/// <summary>
/// 완전한 type-safe Contact 모델
/// 모든 구성요소가 검증된 상태에서만 생성 가능
/// </summary>
public sealed record Contact
{
    public required PersonalName Name { get; init; }
    public required ContactInfo ContactInfo { get; init; }

    private Contact() { }

    /// <summary>
    /// raw string → 완전한 Contact 생성
    /// 이메일만 있는 ContactInfo로 생성
    /// </summary>
    public static Fin<Contact> Create(string firstName, string lastName, string email)
    {
        var nameResult = PersonalName.Create(firstName, lastName);
        var emailResult = EmailAddress.Create(email);

        if (nameResult.IsFail)
            return nameResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));
        if (emailResult.IsFail)
            return emailResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));

        var name = nameResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());
        var emailAddr = emailResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());

        return new Contact
        {
            Name = name,
            ContactInfo = new ContactInfo.EmailOnly(emailAddr)
        };
    }

    public override string ToString() => $"{Name} ({ContactInfo})";
}
