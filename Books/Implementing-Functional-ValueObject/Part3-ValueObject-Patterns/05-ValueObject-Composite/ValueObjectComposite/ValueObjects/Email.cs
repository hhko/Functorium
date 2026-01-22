using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using static LanguageExt.Prelude;

namespace ValueObjectComposite.ValueObjects;

/// <summary>
/// 5. 비교 불가능한 복합 값 객체 - ValueObject
/// 이메일 주소를 나타내는 값 객체 (여러 값 객체 조합)
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class Email : ValueObject
{
    public EmailLocalPart LocalPart { get; }
    public EmailDomain Domain { get; }

    private Email(EmailLocalPart localPart, EmailDomain domain)
    {
        LocalPart = localPart;
        Domain = domain;
    }

    public static Fin<Email> Create(string emailAddress) =>
        CreateFromValidation(Validate(emailAddress), v => new Email(v.LocalPart, v.Domain));

    internal static Email CreateFromValidated((EmailLocalPart LocalPart, EmailDomain Domain) validatedValues) =>
        new(validatedValues.LocalPart, validatedValues.Domain);

    public static Validation<Error, (EmailLocalPart LocalPart, EmailDomain Domain)> Validate(string emailAddress) =>
        from validEmail in ValidateEmailFormat(emailAddress)
        from validParts in ValidateEmailParts(validEmail)
        select validParts;

    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? email
            : DomainError.For<Email>(new DomainErrorType.InvalidFormat(), email,
                $"Email format is invalid. Must contain '@' symbol. Current value: '{email}'");

    private static Validation<Error, (EmailLocalPart LocalPart, EmailDomain Domain)> ValidateEmailParts(string email) =>
        from validParts in ValidateEmailSplit(email)
        from validLocalPart in ValidateLocalPart(validParts.localPart)
        from validDomain in ValidateDomain(validParts.domain)
        select (LocalPart: validLocalPart, Domain: validDomain);

    private static Validation<Error, (string localPart, string domain)> ValidateEmailSplit(string email)
    {
        var parts = email.Split('@');
        return parts.Length == 2
            ? (localPart: parts[0], domain: parts[1])
            : DomainError.For<Email>(new DomainErrorType.InvalidFormat(), email,
                $"Email format is invalid. Must contain '@' symbol. Current value: '{email}'");
    }

    private static Validation<Error, EmailLocalPart> ValidateLocalPart(string localPart) =>
        EmailLocalPart.Validate(localPart).Map(v => EmailLocalPart.CreateFromValidated(v));

    private static Validation<Error, EmailDomain> ValidateDomain(string domain) =>
        EmailDomain.Validate(domain).Map(v => EmailDomain.CreateFromValidated(v));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LocalPart;
        yield return Domain;
    }

    public override string ToString() => $"{LocalPart}@{Domain}";
}
