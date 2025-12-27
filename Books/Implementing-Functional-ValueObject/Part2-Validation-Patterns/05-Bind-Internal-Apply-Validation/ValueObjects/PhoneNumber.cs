using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace BindInternalApplyValidation.ValueObjects;

// 1. public sealed 클래스 선언 - ValueObject 상속
public sealed class PhoneNumber : ValueObject
{
    // 1.1 readonly 속성 선언 - 불변성 보장
    public string CountryCode { get; }
    public string AreaCode { get; }
    public string LocalNumber { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private PhoneNumber(string countryCode, string areaCode, string localNumber)
    {
        CountryCode = countryCode;
        AreaCode = areaCode;
        LocalNumber = localNumber;
    }

    // 3. Public Create 메서드
    public static Fin<PhoneNumber> Create(string phoneNumber) =>
        CreateFromValidation(
            Validate(phoneNumber),
            validValues => new PhoneNumber(validValues.CountryCode, validValues.AreaCode, validValues.LocalNumber));

    // 4. Internal CreateFromValidated 메서드
    internal static PhoneNumber CreateFromValidated((string CountryCode, string AreaCode, string LocalNumber) validatedValues) =>
        new PhoneNumber(validatedValues.CountryCode, validatedValues.AreaCode, validatedValues.LocalNumber);

    // 5. Public Validate 메서드 - 중첩 검증 패턴 구현 (Bind 내부에서 Apply 사용)
    public static Validation<Error, (string CountryCode, string AreaCode, string LocalNumber)> Validate(string phoneNumber) =>
        // 5.1 외부 Bind - 전화번호 형식을 먼저 검증
        ValidatePhoneNumberFormat(phoneNumber)
            // 5.2 내부 Apply - 형식이 유효하면 구성 요소들을 병렬로 검증
            .Bind(validFormat => 
                (ValidateCountryCode(validFormat), ValidateAreaCode(validFormat), ValidateLocalNumber(validFormat))
                    .Apply((countryCode, areaCode, localNumber) => (countryCode, areaCode, localNumber))
                    .As());

    // 5.3 전화번호 형식 검증 (의존) - 먼저 실행되어야 함
    private static Validation<Error, string> ValidatePhoneNumberFormat(string phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10
            ? phoneNumber
            : DomainErrors.PhoneNumberTooShort(phoneNumber);

    // 5.4 국가 코드 검증 (독립) - Apply 내부에서 병렬 실행
    private static Validation<Error, string> ValidateCountryCode(string phoneNumber) =>
        phoneNumber.StartsWith("+82") || phoneNumber.StartsWith("+1")
            ? phoneNumber.Substring(0, 3)
            : DomainErrors.CountryCodeUnsupported(phoneNumber);

    // 5.5 지역 코드 검증 (독립) - Apply 내부에서 병렬 실행
    private static Validation<Error, string> ValidateAreaCode(string phoneNumber) =>
        phoneNumber.Length >= 6 && phoneNumber.Substring(3, 3).All(char.IsDigit)
            ? phoneNumber.Substring(3, 3)
            : DomainErrors.AreaCodeInvalid(phoneNumber);

    // 5.6 로컬 번호 검증 (독립) - Apply 내부에서 병렬 실행
    private static Validation<Error, string> ValidateLocalNumber(string phoneNumber) =>
        phoneNumber.Length >= 10 && phoneNumber.Substring(6).All(char.IsDigit)
            ? phoneNumber.Substring(6)
            : DomainErrors.LocalNumberInvalid(phoneNumber);

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return AreaCode;
        yield return LocalNumber;
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidatePhoneNumberFormat 메서드와 1:1 매핑되는 에러 - 전화번호가 너무 짧음
        public static Error PhoneNumberTooShort(string phoneNumber) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PhoneNumber)}.{nameof(PhoneNumberTooShort)}",
                errorCurrentValue: phoneNumber,
                errorMessage: "");

        // ValidateCountryCode 메서드와 1:1 매핑되는 에러 - 국가 코드가 지원되지 않음
        public static Error CountryCodeUnsupported(string phoneNumber) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PhoneNumber)}.{nameof(CountryCodeUnsupported)}",
                errorCurrentValue: phoneNumber,
                errorMessage: "");

        // ValidateAreaCode 메서드와 1:1 매핑되는 에러 - 지역 코드가 유효하지 않음
        public static Error AreaCodeInvalid(string phoneNumber) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PhoneNumber)}.{nameof(AreaCodeInvalid)}",
                errorCurrentValue: phoneNumber,
                errorMessage: "");

        // ValidateLocalNumber 메서드와 1:1 매핑되는 에러 - 로컬 번호가 유효하지 않음
        public static Error LocalNumberInvalid(string phoneNumber) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PhoneNumber)}.{nameof(LocalNumberInvalid)}",
                errorCurrentValue: phoneNumber,
                errorMessage: "");
    }
}
