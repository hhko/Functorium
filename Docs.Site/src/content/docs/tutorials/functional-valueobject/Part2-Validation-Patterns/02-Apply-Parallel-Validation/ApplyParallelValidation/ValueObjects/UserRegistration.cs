using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyParallelValidation.ValueObjects;

/// <summary>
/// UserRegistration 값 객체 - 병렬 검증(Apply) 패턴 예제
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class UserRegistration : ValueObject
{
    public string Email { get; }
    public string Password { get; }
    public string Name { get; }
    public int Age { get; }

    private UserRegistration(string email, string password, string name, int age)
    {
        Email = email;
        Password = password;
        Name = name;
        Age = age;
    }

    public static Fin<UserRegistration> Create(string email, string password, string name, string ageInput) =>
        CreateFromValidation(
            Validate(email, password, name, ageInput),
            v => new UserRegistration(v.Email, v.Password, v.Name, v.Age));

    public static UserRegistration CreateFromValidated((string Email, string Password, string Name, int Age) v) =>
        new(v.Email, v.Password, v.Name, v.Age);

    // 병렬 검증 - Apply 패턴 (독립적 검증 규칙들을 병렬로 실행)
    public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
        string email, string password, string name, string ageInput) =>
        (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
            .Apply((e, p, n, a) => (Email: e, Password: p, Name: n, Age: a));

    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".")
            ? email
            : DomainError.For<UserRegistration>(new DomainErrorType.InvalidFormat(), email,
                $"Email is missing '@' symbol or '.' character. Current value: '{email}'");

    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 8
            ? password
            : DomainError.For<UserRegistration>(new DomainErrorType.TooShort(), password,
                $"Password is too short. Minimum length is 8 characters. Current value: '{password}'");

    private static Validation<Error, string> ValidateNameFormat(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length >= 2
            ? name
            : DomainError.For<UserRegistration>(new DomainErrorType.TooShort(), name,
                $"Name is too short. Minimum length is 2 characters. Current value: '{name}'");

    private static Validation<Error, int> ValidateAgeFormat(string ageInput) =>
        int.TryParse(ageInput, out var age)
            ? age
            : DomainError.For<UserRegistration>(new DomainErrorType.InvalidFormat(), ageInput,
                $"Age must be a numeric value. Current value: '{ageInput}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return Password;
        yield return Name;
        yield return Age;
    }
}
