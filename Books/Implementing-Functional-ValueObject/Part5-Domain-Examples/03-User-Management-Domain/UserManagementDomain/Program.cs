using Functorium.Abstractions.Errors;
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
        Console.WriteLine("=== 사용자 관리 도메인 값 객체 ===\n");

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
        Console.WriteLine("1. Email (이메일)");
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
        Console.WriteLine("3. PhoneNumber (전화번호)");
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
        Console.WriteLine("4. Username (사용자명)");
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
// 값 객체 구현
// ========================================

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
            return DomainErrors.TooLong(normalized.Length);

        if (!Pattern.IsMatch(normalized))
            return DomainErrors.InvalidFormat(normalized);

        return new Email(normalized);
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

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

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "이메일 주소가 비어있습니다.");
        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: "이메일 주소는 254자를 초과할 수 없습니다.");
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "이메일 형식이 올바르지 않습니다.");
    }
}

public sealed class Password : IEquatable<Password>
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public string Value { get; }

    private Password(string hashedValue) => Value = hashedValue;

    public static Fin<Password> Create(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return DomainErrors.Empty(plainText ?? "null");
        if (plainText.Length < MinLength)
            return DomainErrors.TooShort(plainText.Length);
        if (plainText.Length > MaxLength)
            return DomainErrors.TooLong(plainText.Length);

        var hasUpperCase = plainText.Any(char.IsUpper);
        var hasLowerCase = plainText.Any(char.IsLower);
        var hasDigit = plainText.Any(char.IsDigit);
        var hasSpecialChar = plainText.Any(c => !char.IsLetterOrDigit(c));

        var score = new[] { hasUpperCase, hasLowerCase, hasDigit, hasSpecialChar }.Count(x => x);
        if (score < 3)
            return DomainErrors.WeakPassword(score);

        return new Password(HashPassword(plainText));
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

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Password)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "비밀번호가 비어있습니다.");
        public static Error TooShort(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Password)}.{nameof(TooShort)}",
                errorCurrentValue: length,
                errorMessage: $"비밀번호는 최소 {MinLength}자 이상이어야 합니다.");
        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Password)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"비밀번호는 {MaxLength}자를 초과할 수 없습니다.");
        public static Error WeakPassword(int score) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Password)}.{nameof(WeakPassword)}",
                errorCurrentValue: score,
                errorMessage: "비밀번호가 너무 약합니다. 대문자, 소문자, 숫자, 특수문자 중 3가지 이상을 포함해야 합니다.");
    }
}

public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }
    public string CountryCode { get; }
    public string NationalNumber { get; }

    private PhoneNumber(string countryCode, string nationalNumber)
    {
        CountryCode = countryCode;
        NationalNumber = nationalNumber;
        Value = $"+{countryCode}{nationalNumber}";
    }

    public static Fin<PhoneNumber> Create(string? value, string defaultCountryCode = "82")
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0"))
            digits = digits[1..];

        if (digits.Length < 9 || digits.Length > 11)
            return DomainErrors.InvalidFormat(value);

        return new PhoneNumber(defaultCountryCode, digits);
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

    public bool Equals(PhoneNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PhoneNumber other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(PhoneNumber)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "전화번호가 비어있습니다.");
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(PhoneNumber)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "전화번호 형식이 올바르지 않습니다.");
    }
}

public sealed class Username : IEquatable<Username>
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    private static readonly Regex Pattern = new(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled);

    private static readonly System.Collections.Generic.HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "system", "null", "undefined",
        "api", "www", "mail", "ftp", "support", "help"
    };

    public string Value { get; }

    private Username(string value) => Value = value;

    public static Fin<Username> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length < MinLength)
            return DomainErrors.TooShort(normalized.Length);
        if (normalized.Length > MaxLength)
            return DomainErrors.TooLong(normalized.Length);

        if (!Pattern.IsMatch(normalized))
            return DomainErrors.InvalidFormat(normalized);

        if (ReservedNames.Contains(normalized))
            return DomainErrors.Reserved(normalized);

        return new Username(normalized);
    }

    public bool Equals(Username? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Username other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Username username) => username.Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Username)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "사용자명이 비어있습니다.");
        public static Error TooShort(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Username)}.{nameof(TooShort)}",
                errorCurrentValue: length,
                errorMessage: $"사용자명은 최소 {MinLength}자 이상이어야 합니다.");
        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Username)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"사용자명은 {MaxLength}자를 초과할 수 없습니다.");
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Username)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "사용자명은 영문자로 시작해야 하며, 영문자, 숫자, 밑줄(_), 하이픈(-)만 사용할 수 있습니다.");
        public static Error Reserved(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Username)}.{nameof(Reserved)}",
                errorCurrentValue: value,
                errorMessage: "예약된 사용자명은 사용할 수 없습니다.");
    }
}
