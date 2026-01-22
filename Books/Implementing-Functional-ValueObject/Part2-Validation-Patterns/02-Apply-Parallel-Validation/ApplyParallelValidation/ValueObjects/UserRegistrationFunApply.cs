using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace ApplyParallelValidation.ValueObjects;

/// <summary>
/// fun 기반 개별 Apply 패턴을 사용하는 사용자 등록 값 객체입니다.
/// UserRegistration과 동일한 검증 로직을 fun + Apply 체이닝 방식으로 구현합니다.
/// </summary>
public sealed class UserRegistrationFunApply : ValueObject
{
    public string Email { get; }
    public string Password { get; }
    public string Name { get; }
    public int Age { get; }

    private UserRegistrationFunApply(string email, string password, string name, int age)
    {
        Email = email;
        Password = password;
        Name = name;
        Age = age;
    }

    public static Fin<UserRegistrationFunApply> Create(string email, string password, string name, string ageInput) =>
        CreateFromValidation(
            Validate(email, password, name, ageInput),
            validValues => new UserRegistrationFunApply(
                validValues.Email,
                validValues.Password,
                validValues.Name,
                validValues.Age));

    internal static UserRegistrationFunApply CreateFromValidated((string Email, string Password, string Name, int Age) validatedValues) =>
        new UserRegistrationFunApply(validatedValues.Email, validatedValues.Password, validatedValues.Name, validatedValues.Age);

    // ============================================================
    // fun 기반 개별 Apply 패턴 구현
    // ============================================================
    //
    // 튜플 기반 Apply와 동일한 결과를 반환하지만,
    // Currying을 통해 단계적으로 Apply를 적용하는 방식입니다.
    //
    // 장점:
    // - Applicative Functor 패턴에 충실한 구현
    // - 동적으로 검증 개수를 조절할 때 유용
    // - 함수형 프로그래밍의 고급 합성 패턴 적용 가능
    //
    // 단점:
    // - 튜플 기반보다 코드가 장황함
    // - Currying 개념 이해 필요
    // ============================================================

    /// <summary>
    /// fun 기반 개별 Apply를 사용한 검증입니다.
    /// 각 Apply 호출이 Currying을 통해 단계적으로 함수를 적용합니다.
    /// </summary>
    public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
        string email, string password, string name, string ageInput) =>
        // fun으로 결과 생성 함수를 감싸고,
        // Success로 Validation 컨텍스트에 넣은 후,
        // 각 검증 결과를 개별 Apply로 적용합니다.
        Success<Error, Func<string, string, string, int, (string, string, string, int)>>(
            fun((string e, string p, string n, int a) => (Email: e, Password: p, Name: n, Age: a)))
            .Apply(ValidateEmailFormat(email))
            .Apply(ValidatePasswordStrength(password))
            .Apply(ValidateNameFormat(name))
            .Apply(ValidateAgeFormat(ageInput));

    /// <summary>
    /// 튜플 기반 Apply와의 비교를 위한 대안 구현입니다.
    /// (UserRegistration.Validate와 동일한 방식)
    /// </summary>
    public static Validation<Error, (string Email, string Password, string Name, int Age)> ValidateTupleStyle(
        string email, string password, string name, string ageInput) =>
        (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
            .Apply((validEmail, validPassword, validName, validAge) =>
                (Email: validEmail, Password: validPassword, Name: validName, Age: validAge))
            .As();

    // 검증 메서드들 (UserRegistration과 동일)
    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".")
            ? email
            : DomainError.For<UserRegistrationFunApply>(new DomainErrorType.InvalidFormat(), email, $"Email is missing '@' symbol or '.' character. Current value: '{email}'");

    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 8
            ? password
            : DomainError.For<UserRegistrationFunApply>(new DomainErrorType.TooShort(8), password, $"Password is too short. Minimum length is 8 characters. Current value: '{password}'");

    private static Validation<Error, string> ValidateNameFormat(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length >= 2
            ? name
            : DomainError.For<UserRegistrationFunApply>(new DomainErrorType.TooShort(2), name, $"Name is too short. Minimum length is 2 characters. Current value: '{name}'");

    private static Validation<Error, int> ValidateAgeFormat(string ageInput) =>
        int.TryParse(ageInput, out var age)
            ? age
            : DomainError.For<UserRegistrationFunApply>(new DomainErrorType.InvalidFormat(), ageInput, $"Age must be a numeric value. Current value: '{ageInput}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return Password;
        yield return Name;
        yield return Age;
    }
}
