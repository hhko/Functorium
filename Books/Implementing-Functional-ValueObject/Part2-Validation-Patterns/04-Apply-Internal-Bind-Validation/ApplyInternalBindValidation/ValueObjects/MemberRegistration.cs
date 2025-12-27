using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyInternalBindValidation.ValueObjects;

// 1. public sealed 클래스 선언 - ValueObject 상속
public sealed class MemberRegistration : ValueObject
{
    // 1.1 readonly 속성 선언 - 불변성 보장
    public string Username { get; }
    public string Email { get; }
    public string Password { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private MemberRegistration(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }

    // 3. Public Create 메서드
    public static Fin<MemberRegistration> Create(string username, string email, string password) =>
        CreateFromValidation(
            Validate(username, email, password),
            validValues => new MemberRegistration(validValues.Username, validValues.Email, validValues.Password));

    // 4. Internal CreateFromValidated 메서드
    internal static MemberRegistration CreateFromValidated((string Username, string Email, string Password) validatedValues) =>
        new MemberRegistration(validatedValues.Username, validatedValues.Email, validatedValues.Password);

    // 5. Public Validate 메서드 - 중첩 검증 패턴 구현 (Apply 내부에서 Bind 사용)
    public static Validation<Error, (string Username, string Email, string Password)> Validate(
        string username, string email, string password) =>
        // 5.1 외부 Apply - 3개 필드를 병렬로 검증하되, 각각 내부에서 Bind를 사용
        (ValidateUsername(username), ValidateEmail(email), ValidatePassword(password))
            .Apply((validUsername, validEmail, validPassword) => 
                (Username: validUsername, Email: validEmail, Password: validPassword))
            .As();

    // 5.2 사용자명 검증 (독립) - 내부에서 Bind 사용 (2단계 검증)
    private static Validation<Error, string> ValidateUsername(string username) =>
        ValidateUsernameFormat(username)
            .Bind(_ => ValidateUsernameAvailability(username));

    // 5.3 이메일 검증 (독립) - 내부에서 Bind 사용 (2단계 검증)
    private static Validation<Error, string> ValidateEmail(string email) =>
        ValidateEmailFormat(email)
            .Bind(_ => ValidateEmailDomain(email));

    // 5.4 비밀번호 검증 (독립) - 내부에서 Bind 사용 (2단계 검증)
    private static Validation<Error, string> ValidatePassword(string password) =>
        ValidatePasswordStrength(password)
            .Bind(_ => ValidatePasswordHistory(password));

    // 5.5 사용자명 세부 검증 메서드들
    private static Validation<Error, string> ValidateUsernameFormat(string username) =>
        !string.IsNullOrWhiteSpace(username) && username.Length >= 3
            ? username
            : DomainErrors.UsernameTooShort(username);

    private static Validation<Error, string> ValidateUsernameAvailability(string username) =>
        !username.StartsWith("admin")
            ? username
            : DomainErrors.UsernameNotAvailable(username);

    // 5.6 이메일 세부 검증 메서드들
    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@")
            ? email
            : DomainErrors.EmailMissingAt(email);

    private static Validation<Error, string> ValidateEmailDomain(string email) =>
        email.EndsWith(".com") || email.EndsWith(".co.kr")
            ? email
            : DomainErrors.EmailDomainUnsupported(email);

    // 5.7 비밀번호 세부 검증 메서드들
    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 6 && password.Any(char.IsDigit)
            ? password
            : DomainErrors.PasswordTooWeak(password);

    private static Validation<Error, string> ValidatePasswordHistory(string password) =>
        password != "password123"
            ? password
            : DomainErrors.PasswordInHistory(password);

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Username;
        yield return Email;
        yield return Password;
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateUsernameFormat 메서드와 1:1 매핑되는 에러 - 사용자명이 너무 짧음
        public static Error UsernameTooShort(string username) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MemberRegistration)}.{nameof(UsernameTooShort)}",
                errorCurrentValue: username,
                errorMessage: "");

        // ValidateUsernameAvailability 메서드와 1:1 매핑되는 에러 - 사용자명을 사용할 수 없음
        public static Error UsernameNotAvailable(string username) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MemberRegistration)}.{nameof(UsernameNotAvailable)}",
                errorCurrentValue: username,
                errorMessage: "");

        // ValidateEmailFormat 메서드와 1:1 매핑되는 에러 - 이메일에 @ 기호가 누락됨
        public static Error EmailMissingAt(string email) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MemberRegistration)}.{nameof(EmailMissingAt)}",
                errorCurrentValue: email,
                errorMessage: "");

        // ValidateEmailDomain 메서드와 1:1 매핑되는 에러 - 이메일 도메인이 지원되지 않음
        public static Error EmailDomainUnsupported(string email) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MemberRegistration)}.{nameof(EmailDomainUnsupported)}",
                errorCurrentValue: email,
                errorMessage: "");

        // ValidatePasswordStrength 메서드와 1:1 매핑되는 에러 - 비밀번호가 너무 약함
        public static Error PasswordTooWeak(string password) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MemberRegistration)}.{nameof(PasswordTooWeak)}",
                errorCurrentValue: password,
                errorMessage: "");

        // ValidatePasswordHistory 메서드와 1:1 매핑되는 에러 - 비밀번호가 이전에 사용됨
        public static Error PasswordInHistory(string password) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MemberRegistration)}.{nameof(PasswordInHistory)}",
                errorCurrentValue: password,
                errorMessage: "");
    }
}
