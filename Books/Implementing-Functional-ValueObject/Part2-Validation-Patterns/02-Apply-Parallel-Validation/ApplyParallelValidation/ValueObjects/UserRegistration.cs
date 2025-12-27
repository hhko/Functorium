using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyParallelValidation.ValueObjects;

// 1. public sealed 클래스 선언 - ValueObject 상속
public sealed class UserRegistration : ValueObject
{
    // 1.1 readonly 속성 선언 - 불변성 보장
    public string Email { get; }
    public string Password { get; }
    public string Name { get; }
    public int Age { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private UserRegistration(string email, string password, string name, int age)
    {
        Email = email;
        Password = password;
        Name = name;
        Age = age;
    }

    // 3. Public Create 메서드
    public static Fin<UserRegistration> Create(string email, string password, string name, string ageInput) =>
        CreateFromValidation(
            Validate(email, password, name, ageInput),
            validValues => new UserRegistration(
                validValues.Email,
                validValues.Password,
                validValues.Name,
                validValues.Age));

    // 4. Internal CreateFromValidated 메서드
    internal static UserRegistration CreateFromValidated((string Email, string Password, string Name, int Age) validatedValues) =>
        new UserRegistration(validatedValues.Email, validatedValues.Password, validatedValues.Name, validatedValues.Age);

    // 5. Public Validate 메서드 - 독립 검증 패턴 구현 (핵심 검증만 병렬로 실행)
    public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
        string email, string password, string name, string ageInput) =>
        // 핵심 검증 규칙들을 병렬로 실행 (독립적 유효성 검사)
        (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
            .Apply((validEmail, validPassword, validName, validAge) => 
                (Email: validEmail, Password: validPassword, Name: validName, Age: validAge))
            .As();

    // 5.1 이메일 형식 검증 (독립) - DomainErrors 단위로 Validate 접두사 사용
    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".")
            ? email                                 // 성공: 값 반환
            : DomainErrors.EmailMissingAt(email);   // 실패: 에러 반환

    // 5.2 비밀번호 강도 검증 (독립) - DomainErrors 단위로 Validate 접두사 사용
    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 8
            ? password                                 // 성공: 값 반환
            : DomainErrors.PasswordTooShort(password); // 실패: 에러 반환

    // 5.3 이름 형식 검증 (독립) - DomainErrors 단위로 Validate 접두사 사용
    private static Validation<Error, string> ValidateNameFormat(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length >= 2
            ? name                                 // 성공: 값 반환
            : DomainErrors.NameTooShort(name);     // 실패: 에러 반환

    // 5.4 나이 형식 검증 (독립) - DomainErrors 단위로 Validate 접두사 사용
    private static Validation<Error, int> ValidateAgeFormat(string ageInput) =>
        int.TryParse(ageInput, out var age)
            ? age                                 // 성공: 값 반환
            : DomainErrors.AgeNotNumeric(ageInput); // 실패: 에러 반환


    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return Password;
        yield return Name;
        yield return Age;
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateEmailFormat 메서드와 1:1 매핑되는 에러 - 이메일에 @ 기호가 누락됨
        public static Error EmailMissingAt(string email) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserRegistration)}.{nameof(EmailMissingAt)}",
                errorCurrentValue: email,
                errorMessage: "");

        // ValidatePasswordStrength 메서드와 1:1 매핑되는 에러 - 비밀번호가 너무 짧음
        public static Error PasswordTooShort(string password) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserRegistration)}.{nameof(PasswordTooShort)}",
                errorCurrentValue: password,
                errorMessage: "");

        // ValidateNameFormat 메서드와 1:1 매핑되는 에러 - 이름이 너무 짧음
        public static Error NameTooShort(string name) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserRegistration)}.{nameof(NameTooShort)}",
                errorCurrentValue: name,
                errorMessage: "");

        // ValidateAgeFormat 메서드와 1:1 매핑되는 에러 - 나이가 숫자가 아님
        public static Error AgeNotNumeric(string ageInput) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserRegistration)}.{nameof(AgeNotNumeric)}",
                errorCurrentValue: ageInput,
                errorMessage: "");

    }
}
