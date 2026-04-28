using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyInternalBindValidation.ValueObjects;

/// <summary>
/// MemberRegistration 값 객체 - Apply 내부 Bind 패턴 예제
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class MemberRegistration : ValueObject
{
    public sealed record UsernameNotAvailable : DomainErrorKind.Custom;
    public sealed record EmailDomainUnsupported : DomainErrorKind.Custom;
    public sealed record PasswordTooWeak : DomainErrorKind.Custom;
    public sealed record PasswordInHistory : DomainErrorKind.Custom;
    public string Username { get; }
    public string Email { get; }
    public string Password { get; }

    private MemberRegistration(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }

    public static Fin<MemberRegistration> Create(string username, string email, string password) =>
        CreateFromValidation(
            Validate(username, email, password),
            v => new MemberRegistration(v.Username, v.Email, v.Password));

    public static MemberRegistration CreateFromValidated((string Username, string Email, string Password) v) =>
        new(v.Username, v.Email, v.Password);

    // 중첩 검증 - Apply 외부 + Bind 내부 패턴
    public static Validation<Error, (string Username, string Email, string Password)> Validate(
        string username, string email, string password) =>
        (ValidateUsername(username), ValidateEmail(email), ValidatePassword(password))
            .Apply((u, e, p) => (Username: u, Email: e, Password: p));

    // 사용자명 검증 - 내부 Bind (2단계 검증)
    private static Validation<Error, string> ValidateUsername(string username) =>
        ValidateUsernameFormat(username)
            .Bind(_ => ValidateUsernameAvailability(username));

    // 이메일 검증 - 내부 Bind (2단계 검증)
    private static Validation<Error, string> ValidateEmail(string email) =>
        ValidateEmailFormat(email)
            .Bind(_ => ValidateEmailDomain(email));

    // 비밀번호 검증 - 내부 Bind (2단계 검증)
    private static Validation<Error, string> ValidatePassword(string password) =>
        ValidatePasswordStrength(password)
            .Bind(_ => ValidatePasswordHistory(password));

    private static Validation<Error, string> ValidateUsernameFormat(string username) =>
        !string.IsNullOrWhiteSpace(username) && username.Length >= 3
            ? username
            : DomainError.For<MemberRegistration>(new DomainErrorKind.TooShort(), username,
                $"Username is too short. Minimum length is 3 characters. Current value: '{username}'");

    private static Validation<Error, string> ValidateUsernameAvailability(string username) =>
        !username.StartsWith("admin")
            ? username
            : DomainError.For<MemberRegistration>(new UsernameNotAvailable(), username,
                $"Username is not available. Reserved usernames cannot start with 'admin'. Current value: '{username}'");

    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@")
            ? email
            : DomainError.For<MemberRegistration>(new DomainErrorKind.InvalidFormat(), email,
                $"Email is missing '@' symbol. Current value: '{email}'");

    private static Validation<Error, string> ValidateEmailDomain(string email) =>
        email.EndsWith(".com") || email.EndsWith(".co.kr")
            ? email
            : DomainError.For<MemberRegistration>(new EmailDomainUnsupported(), email,
                $"Email domain is not supported. Only '.com' and '.co.kr' are allowed. Current value: '{email}'");

    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 6 && password.Any(char.IsDigit)
            ? password
            : DomainError.For<MemberRegistration>(new PasswordTooWeak(), password,
                $"Password is too weak. Must be at least 6 characters and contain a digit. Current value: '{password}'");

    private static Validation<Error, string> ValidatePasswordHistory(string password) =>
        password != "password123"
            ? password
            : DomainError.For<MemberRegistration>(new PasswordInHistory(), password,
                $"Password was previously used. Please choose a different password. Current value: '{password}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Username;
        yield return Email;
        yield return Password;
    }
}
