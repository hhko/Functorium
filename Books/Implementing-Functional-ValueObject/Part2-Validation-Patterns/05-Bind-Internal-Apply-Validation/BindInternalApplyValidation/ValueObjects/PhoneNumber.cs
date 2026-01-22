using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace BindInternalApplyValidation.ValueObjects;

/// <summary>
/// PhoneNumber 값 객체 - Bind 외부 + Apply 내부 패턴 예제
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    public string CountryCode { get; }
    public string AreaCode { get; }
    public string LocalNumber { get; }

    private PhoneNumber(string countryCode, string areaCode, string localNumber)
    {
        CountryCode = countryCode;
        AreaCode = areaCode;
        LocalNumber = localNumber;
    }

    public static Fin<PhoneNumber> Create(string phoneNumber) =>
        CreateFromValidation(
            Validate(phoneNumber),
            v => new PhoneNumber(v.CountryCode, v.AreaCode, v.LocalNumber));

    internal static PhoneNumber CreateFromValidated((string CountryCode, string AreaCode, string LocalNumber) v) =>
        new(v.CountryCode, v.AreaCode, v.LocalNumber);

    // 중첩 검증 - Bind 외부 + Apply 내부 패턴
    public static Validation<Error, (string CountryCode, string AreaCode, string LocalNumber)> Validate(string phoneNumber) =>
        // 외부 Bind - 전화번호 형식을 먼저 검증
        ValidatePhoneNumberFormat(phoneNumber)
            // 내부 Apply - 형식이 유효하면 구성 요소들을 병렬로 검증
            .Bind(validFormat =>
                (ValidateCountryCode(validFormat), ValidateAreaCode(validFormat), ValidateLocalNumber(validFormat))
                    .Apply((c, a, l) => (c, a, l))
                    .As());

    private static Validation<Error, string> ValidatePhoneNumberFormat(string phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10
            ? phoneNumber
            : DomainError.For<PhoneNumber>(new DomainErrorType.TooShort(), phoneNumber,
                $"Phone number is too short. Minimum length is 10 characters. Current value: '{phoneNumber}'");

    private static Validation<Error, string> ValidateCountryCode(string phoneNumber) =>
        phoneNumber.StartsWith("+82") || phoneNumber.StartsWith("+1")
            ? phoneNumber.Substring(0, 3)
            : DomainError.For<PhoneNumber>(new DomainErrorType.Custom("CountryCodeUnsupported"), phoneNumber,
                $"Country code is not supported. Only '+82' (Korea) and '+1' (USA) are allowed. Current value: '{phoneNumber}'");

    private static Validation<Error, string> ValidateAreaCode(string phoneNumber) =>
        phoneNumber.Length >= 6 && phoneNumber.Substring(3, 3).All(char.IsDigit)
            ? phoneNumber.Substring(3, 3)
            : DomainError.For<PhoneNumber>(new DomainErrorType.InvalidFormat(), phoneNumber,
                $"Area code is invalid. Must be 3 digits. Current value: '{phoneNumber}'");

    private static Validation<Error, string> ValidateLocalNumber(string phoneNumber) =>
        phoneNumber.Length >= 10 && phoneNumber.Substring(6).All(char.IsDigit)
            ? phoneNumber.Substring(6)
            : DomainError.For<PhoneNumber>(new DomainErrorType.InvalidFormat(), phoneNumber,
                $"Local number is invalid. Must be digits only. Current value: '{phoneNumber}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return AreaCode;
        yield return LocalNumber;
    }
}
