using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace UserManagementDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 사용자 관리 도메인 값 객체 (Functorium 프레임워크 기반) ===\n");

        // 1. Email
        DemonstrateEmail();

        // 2. Password
        DemonstratePassword();

        // 3. PhoneNumber
        DemonstratePhoneNumber();

        // 4. Username
        DemonstrateUsername();
    }

    static void DemonstrateEmail()
    {
        Console.WriteLine("1. Email (이메일) - SimpleValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var email = Email.Create("User@Example.COM");
        email.Match(
            Succ: e =>
            {
                Console.WriteLine($"   정규화: {e}");
                Console.WriteLine($"   로컬 파트: {e.LocalPart}");
                Console.WriteLine($"   도메인: {e.Domain}");
                Console.WriteLine($"   마스킹: {e.Masked}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        var invalid = Email.Create("invalid-email");
        invalid.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   잘못된 형식: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstratePassword()
    {
        Console.WriteLine("2. Password (비밀번호)");
        Console.WriteLine("─".PadRight(40, '─'));

        var password = Password.Create("MySecure@Pass123");
        password.Match(
            Succ: p =>
            {
                Console.WriteLine($"   비밀번호 생성: 성공");
                Console.WriteLine($"   표시: {p}");
                Console.WriteLine($"   검증(맞음): {p.Verify("MySecure@Pass123")}");
                Console.WriteLine($"   검증(틀림): {p.Verify("WrongPassword")}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        var weak = Password.Create("weak");
        weak.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   약한 비밀번호: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstratePhoneNumber()
    {
        Console.WriteLine("3. PhoneNumber (전화번호) - ValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var phone = PhoneNumber.Create("010-1234-5678");
        phone.Match(
            Succ: p =>
            {
                Console.WriteLine($"   정규화: {p}");
                Console.WriteLine($"   포맷팅: {p.Formatted}");
                Console.WriteLine($"   국가 코드: +{p.CountryCode}");
                Console.WriteLine($"   마스킹: {p.Masked}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateUsername()
    {
        Console.WriteLine("4. Username (사용자명) - SimpleValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var username = Username.Create("john_doe123");
        username.Match(
            Succ: u => Console.WriteLine($"   사용자명: {u}"),
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        var reserved = Username.Create("admin");
        reserved.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   예약어: {e.Message}")
        );

        var invalid = Username.Create("123start");
        invalid.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   잘못된 형식: {e.Message}")
        );

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 구현 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 2. Private 생성자 - 단순 대입만 처리
    private Email(string value) : base(value) { }

    /// <summary>
    /// 이메일 주소에 대한 public 접근자
    /// </summary>
    public string Address => Value;

    // 파생 속성
    public string LocalPart => Value.Split('@')[0];
    public string Domain => Value.Split('@')[1];

    public string Masked
    {
        get
        {
            var local = LocalPart;
            if (local.Length <= 2)
                return $"**@{Domain}";
            return $"{local[0]}***{local[^1]}@{Domain}";
        }
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    // 정규화된 값으로 직접 생성 (ORM용)
    internal static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    // 5. Public Validate 메서드 - 순차 검증
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateNotTooLong(value.Trim()))
            .Bind(normalized => ValidateFormat(normalized));

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 길이 검증
    private static Validation<Error, string> ValidateNotTooLong(string value)
    {
        var normalized = value.ToLowerInvariant();
        return normalized.Length <= 254
            ? normalized
            : DomainErrors.TooLong(normalized.Length);
    }

    // 5.3 형식 검증
    private static Validation<Error, string> ValidateFormat(string value) =>
        Pattern.IsMatch(value)
            ? value
            : DomainErrors.InvalidFormat(value);

    public static implicit operator string(Email email) => email.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Email address cannot be empty. Current value: '{value}'");

        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"Email address cannot exceed 254 characters. Current length: '{length}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid email format. Current value: '{value}'");
    }
}

/// <summary>
/// Password 값 객체 (해시 저장이므로 특수 처리)
/// </summary>
public sealed class Password : IEquatable<Password>
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public string Value { get; }

    // 2. Private 생성자 - 해시된 값으로 생성
    private Password(string hashedValue) => Value = hashedValue;

    // 3. Public Create 메서드 - 검증과 해시 생성을 연결
    public static Fin<Password> Create(string? plainText)
    {
        var validation = Validate(plainText ?? "null");
        return validation.Match<Fin<Password>>(
            Succ: validPlainText => new Password(HashPassword(validPlainText)),
            Fail: errors => Error.Many(errors));
    }

    // 5. Public Validate 메서드 - 순차 검증 후 강도 검증
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateMinLength(value))
            .Bind(_ => ValidateMaxLength(value))
            .Bind(_ => ValidateStrength(value))
            .Map(_ => value);

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 최소 길이 검증
    private static Validation<Error, string> ValidateMinLength(string value) =>
        value.Length >= MinLength
            ? value
            : DomainErrors.TooShort(value.Length);

    // 5.3 최대 길이 검증
    private static Validation<Error, string> ValidateMaxLength(string value) =>
        value.Length <= MaxLength
            ? value
            : DomainErrors.TooLong(value.Length);

    // 5.4 강도 검증
    private static Validation<Error, string> ValidateStrength(string value)
    {
        var hasUpperCase = value.Any(char.IsUpper);
        var hasLowerCase = value.Any(char.IsLower);
        var hasDigit = value.Any(char.IsDigit);
        var hasSpecialChar = value.Any(c => !char.IsLetterOrDigit(c));

        var score = new[] { hasUpperCase, hasLowerCase, hasDigit, hasSpecialChar }.Count(x => x);
        return score >= 3
            ? value
            : DomainErrors.WeakPassword(score);
    }

    private static string HashPassword(string plainText)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(plainText + "salt");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string plainText)
    {
        var hashedInput = HashPassword(plainText);
        return Value == hashedInput;
    }

    public bool Equals(Password? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Password other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => "********";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Password)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Password cannot be empty. Current value: '{value}'");

        public static Error TooShort(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Password)}.{nameof(TooShort)}",
                errorCurrentValue: length,
                errorMessage: $"Password must be at least {MinLength} characters. Current length: '{length}'");

        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Password)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"Password cannot exceed {MaxLength} characters. Current length: '{length}'");

        public static Error WeakPassword(int score) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Password)}.{nameof(WeakPassword)}",
                errorCurrentValue: score,
                errorMessage: $"Password is too weak. Must contain at least 3 of: uppercase, lowercase, digits, special characters. Current score: '{score}'");
    }
}

/// <summary>
/// PhoneNumber 값 객체 (ValueObject 기반)
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    // 1.1 속성 선언
    public string CountryCode { get; }
    public string NationalNumber { get; }

    // 파생 속성
    public string FullNumber => $"+{CountryCode}{NationalNumber}";

    // 2. Private 생성자 - 단순 대입만 처리
    private PhoneNumber(string countryCode, string nationalNumber)
    {
        CountryCode = countryCode;
        NationalNumber = nationalNumber;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<PhoneNumber> Create(string? value, string defaultCountryCode = "82") =>
        CreateFromValidation(
            Validate(value ?? "null", defaultCountryCode),
            validValues => new PhoneNumber(validValues.CountryCode, validValues.NationalNumber));

    // 5. Public Validate 메서드 - 순차 검증
    public static Validation<Error, (string CountryCode, string NationalNumber)> Validate(string value, string countryCode) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateDigits(value))
            .Map(digits => (countryCode, digits));

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 숫자 추출 및 형식 검증
    private static Validation<Error, string> ValidateDigits(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0"))
            digits = digits[1..];

        return digits.Length >= 9 && digits.Length <= 11
            ? digits
            : DomainErrors.InvalidFormat(value);
    }

    public string Formatted
    {
        get
        {
            if (CountryCode == "82" && NationalNumber.StartsWith("10"))
            {
                if (NationalNumber.Length == 10)
                    return $"0{NationalNumber[..2]}-{NationalNumber[2..6]}-{NationalNumber[6..]}";
                return $"0{NationalNumber[..2]}-{NationalNumber[2..5]}-{NationalNumber[5..]}";
            }
            return $"+{CountryCode} {NationalNumber}";
        }
    }

    public string Masked => $"+{CountryCode} ***-****-{NationalNumber[^4..]}";

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return NationalNumber;
    }

    public override string ToString() => FullNumber;

    public static implicit operator string(PhoneNumber phone) => phone.FullNumber;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PhoneNumber)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Phone number cannot be empty. Current value: '{value}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PhoneNumber)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid phone number format. Current value: '{value}'");
    }
}

/// <summary>
/// Username 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class Username : SimpleValueObject<string>
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    private static readonly Regex Pattern = new(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled);

    private static readonly System.Collections.Generic.HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "system", "null", "undefined",
        "api", "www", "mail", "ftp", "support", "help"
    };

    // 2. Private 생성자 - 단순 대입만 처리
    private Username(string value) : base(value) { }

    /// <summary>
    /// 사용자명에 대한 public 접근자
    /// </summary>
    public string Name => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Username> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Username(validValue));

    // 5. Public Validate 메서드 - 순차 검증
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(normalized => ValidateMinLength(normalized))
            .Bind(normalized => ValidateMaxLength(normalized))
            .Bind(normalized => ValidateFormat(normalized))
            .Bind(normalized => ValidateNotReserved(normalized));

    // 5.1 빈 값 검증 및 정규화
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value.Trim().ToLowerInvariant()
            : DomainErrors.Empty(value);

    // 5.2 최소 길이 검증
    private static Validation<Error, string> ValidateMinLength(string value) =>
        value.Length >= MinLength
            ? value
            : DomainErrors.TooShort(value.Length);

    // 5.3 최대 길이 검증
    private static Validation<Error, string> ValidateMaxLength(string value) =>
        value.Length <= MaxLength
            ? value
            : DomainErrors.TooLong(value.Length);

    // 5.4 형식 검증
    private static Validation<Error, string> ValidateFormat(string value) =>
        Pattern.IsMatch(value)
            ? value
            : DomainErrors.InvalidFormat(value);

    // 5.5 예약어 검증
    private static Validation<Error, string> ValidateNotReserved(string value) =>
        !ReservedNames.Contains(value)
            ? value
            : DomainErrors.Reserved(value);

    public static implicit operator string(Username username) => username.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Username)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Username cannot be empty. Current value: '{value}'");

        public static Error TooShort(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Username)}.{nameof(TooShort)}",
                errorCurrentValue: length,
                errorMessage: $"Username must be at least {MinLength} characters. Current length: '{length}'");

        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Username)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"Username cannot exceed {MaxLength} characters. Current length: '{length}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Username)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Username must start with a letter and contain only letters, numbers, underscores, and hyphens. Current value: '{value}'");

        public static Error Reserved(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Username)}.{nameof(Reserved)}",
                errorCurrentValue: value,
                errorMessage: $"This username is reserved and cannot be used. Current value: '{value}'");
    }
}
