using LanguageExt;

namespace FinalContact;

/// <summary>
/// Part 1~3 통합 최종 Contact 모델
/// 모든 구성요소가 검증된 상태에서만 생성 가능
/// </summary>
public sealed record Contact
{
    public required PersonalName Name { get; init; }
    public required ContactInfo ContactInfo { get; init; }

    private Contact() { }

    /// <summary>
    /// 이메일만 있는 Contact 생성 (초기 상태: Unverified)
    /// </summary>
    public static Fin<Contact> Create(
        string firstName, string lastName, string email, string? middleInitial = null)
    {
        var nameResult = PersonalName.Create(firstName, lastName, middleInitial);
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
            ContactInfo = new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(emailAddr))
        };
    }

    /// <summary>
    /// 우편 주소만 있는 Contact 생성
    /// </summary>
    public static Fin<Contact> CreateWithPostal(
        string firstName, string lastName,
        string address1, string city, string state, string zip,
        string? middleInitial = null)
    {
        var nameResult = PersonalName.Create(firstName, lastName, middleInitial);
        var postalResult = PostalAddress.Create(address1, city, state, zip);

        if (nameResult.IsFail)
            return nameResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));
        if (postalResult.IsFail)
            return postalResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));

        var name = nameResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());
        var postal = postalResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());

        return new Contact
        {
            Name = name,
            ContactInfo = new ContactInfo.PostalOnly(postal)
        };
    }

    /// <summary>
    /// 이메일 + 우편 주소 모두 있는 Contact 생성 (이메일 초기 상태: Unverified)
    /// </summary>
    public static Fin<Contact> CreateWithEmailAndPostal(
        string firstName, string lastName,
        string email,
        string address1, string city, string state, string zip,
        string? middleInitial = null)
    {
        var nameResult = PersonalName.Create(firstName, lastName, middleInitial);
        var emailResult = EmailAddress.Create(email);
        var postalResult = PostalAddress.Create(address1, city, state, zip);

        if (nameResult.IsFail)
            return nameResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));
        if (emailResult.IsFail)
            return emailResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));
        if (postalResult.IsFail)
            return postalResult.Match<Fin<Contact>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<Contact>(e));

        var name = nameResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());
        var emailAddr = emailResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());
        var postal = postalResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException());

        return new Contact
        {
            Name = name,
            ContactInfo = new ContactInfo.EmailAndPostal(
                new EmailVerificationState.Unverified(emailAddr), postal)
        };
    }

    public override string ToString() => $"{Name} ({ContactInfo})";
}
