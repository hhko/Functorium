---
title: "User Management Domain"
---
## Overview

"User@Example.COM"과 "user@example.com"을 다른 이메days로 취급하면 같은 사용자가 중복 가입합니다. Handling passwords as `string` exposes plain text in logs and debuggers. If phone numbers "010-1234-5678" and "+82-10-1234-5678" are stored differently, search becomes impossible. 사용자 관리 도메인에서  won시 타입은 보안과 데이터 품질 모두를 위협합니다.

In this chapter, we implement 4 core concepts needed for user authentication and profiles as value objects, guaranteeing normalization/masking/hashing at the type level.

- **Email**: 이메days 형식 검증과 정규화, Masking 기능 제공
- **Password**: Provides password strength validation, hash storage, and verification
- **PhoneNumber**: Provides phone number normalization, formatting, and masking
- **Username**: Provides username rule validation and reserved word blocking

## Learning Objectives

### **Core Learning Objectives**
- You can **implement a security-focused design** that stores only hashes without saving plain text in Password.
- Email과 PhoneNumber에서 입력값 정규화와 표시용 포맷팅을 **minutes리할 수** 있습니다.
- Username에서 시스템 예약어를 **차단하는 Pattern을 구현할 수** 있습니다.
- You can **protect sensitive information** with Masked properties or safe ToString() in all value objects.

### **What You Will Verify Through Practice**
- Email normalization (lowercase conversion) and LocalPart/Domain parsing
- Password strength validation (uppercase, lowercase, digits, special characters) and hash verification
- PhoneNumber international format conversion and per-country formatting
- Username format rules and reserved word validation

## Why Is This Needed?

사용자 관리는 보안과 데이터 품질이 특히 중요한 도메인입니다.  won시 타입으로 사용자 데이터를 다루면 여러 문제가 발생합니다.

Handling passwords as `string` can expose plain text in logs and debuggers, but the Password value object hashes at creation and `ToString()` always returns "********". "User@Example.COM"과 "user@example.com"이 다른 이메days로 취급되면 중복 가입이 가능한데, Email value object는 항상 소문자로 정규화하여 이를 방지합니다. 전화번호의 다양한 입력 형식("010-1234-5678", "+82-10-1234-5678" 등)도 PhoneNumber가 내부적으로 정규화된 형식을 유지하여 days관된 검색을 guarantees.

## Core Concepts

### Email (이메days)

Email은 이메days 주소를 검증하고 정규화합니다. LocalPart, Domain 파싱과 Masking 기능을 provides.

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Email(string value) : base(value) { }

    public string Address => Value;  // Public accessor for protected Value
    public string LocalPart => Value.Split('@')[0];    // "user"
    public string Domain => Value.Split('@')[1];       // "example.com"

    public string Masked
    {
        get
        {
            var local = LocalPart;
            if (local.Length <= 2)
                return $"**@{Domain}";
            return $"{local[0]}***{local[^1]}@{Domain}";  // "u***r@example.com"
        }
    }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateNotTooLong(value.Trim()))
            .Bind(normalized => ValidateFormat(normalized));

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<Email>(new DomainErrorType.Empty(), value,
                $"Email address cannot be empty. Current value: '{value}'");

    public static implicit operator string(Email email) => email.Value;
}
```

항상 소문자로 저장하므로 동등성 비교가 단순해지고, `LocalPart`와 `Domain` 속성으로 구성 요소에 쉽게 접근할 수 있습니다. 정규화와 파싱이 결합된 Pattern입니다.

### Password (비밀번호)

Password validates password strength and stores hashed values. Plain text is never stored.

```csharp
public sealed class Password : IEquatable<Password>
{
    public sealed record InsufficientComplexity : DomainErrorType.Custom;

    public const int MinLength = 8;
    public const int MaxLength = 128;

    public string Value { get; }  // Hashed value

    private Password(string hashedValue) => Value = hashedValue;

    public static Fin<Password> Create(string? plainText)
    {
        var validation = Validate(plainText ?? "null");
        return validation.Match<Fin<Password>>(
            Succ: validPlainText => new Password(HashPassword(validPlainText)),
            Fail: errors => Error.Many(errors));
    }

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateMinLength(value))
            .Bind(_ => ValidateMaxLength(value))
            .Bind(_ => ValidateStrength(value))
            .Map(_ => value);

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<Password>(new DomainErrorType.Empty(), value,
                $"Password cannot be empty. Current value: '{value}'");

    public bool Verify(string plainText) => Value == HashPassword(plainText);
    public override string ToString() => "********";  // Never expose plain text
}
```

`Create()` 시점에 평문을 Hash하고, value object에는 Hash만 저장합니다. `Verify()`로 검증하고, `ToString()`은 항상 Masking된 값을 반환하여 평문 노출을  won천 차단합니다.

### PhoneNumber (전화번호)

PhoneNumber normalizes phone numbers to international format. It provides per-country formatting and masking functionality.

```csharp
public sealed class PhoneNumber : ValueObject
{
    public string CountryCode { get; }   // "82"
    public string NationalNumber { get; }  // "1012345678"
    public string FullNumber => $"+{CountryCode}{NationalNumber}";

    private PhoneNumber(string countryCode, string nationalNumber)
    {
        CountryCode = countryCode;
        NationalNumber = nationalNumber;
    }

    public static Fin<PhoneNumber> Create(string? value, string defaultCountryCode = "82") =>
        CreateFromValidation(
            Validate(value ?? "null", defaultCountryCode),
            validValues => new PhoneNumber(validValues.CountryCode, validValues.NationalNumber));

    public static Validation<Error, (string CountryCode, string NationalNumber)> Validate(
        string value, string countryCode) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateDigits(value))
            .Map(digits => (countryCode, digits));

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<PhoneNumber>(new DomainErrorType.Empty(), value,
                $"Phone number cannot be empty. Current value: '{value}'");

    public string Masked => $"+{CountryCode} ***-****-{NationalNumber[^4..]}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return NationalNumber;
    }
}
```

다양한 입력 형식(010-1234-5678, +82-10-1234-5678 등)을 정규화된 국제 형식으로 저장하고, `Formatted`로 표시용 형식을 provides. 입력 형식과 저장 형식의 minutes리 Pattern입니다.

### Username (사용자명)

Username validates username rules and blocks reserved words.

```csharp
public sealed class Username : SimpleValueObject<string>
{
    public sealed record Reserved : DomainErrorType.Custom;

    public const int MinLength = 3;
    public const int MaxLength = 30;

    private static readonly Regex Pattern = new(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "system", "null", "undefined",
        "api", "www", "mail", "ftp", "support", "help"
    };

    private Username(string value) : base(value) { }

    public string Name => Value;  // Public accessor for protected Value

    public static Fin<Username> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Username(validValue));

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(normalized => ValidateMinLength(normalized))
            .Bind(normalized => ValidateMaxLength(normalized))
            .Bind(normalized => ValidateFormat(normalized))
            .Bind(normalized => ValidateNotReserved(normalized));

    private static Validation<Error, string> ValidateNotReserved(string value) =>
        !ReservedNames.Contains(value)
            ? value
            : DomainError.For<Username>(new Reserved(), value,
                $"This username is reserved. Current value: '{value}'");

    public static implicit operator string(Username username) => username.Value;
}
```

사용자명 규칙(영문자 시작, 길이 제한)과 예약어 목록이 value object 내부에 정의되어 days관되게 적용됩니다.

## Practical Guidelines

### Expected Output
```
=== User Management Domain Value Objects ===

1. Email
────────────────────────────────────────
   Normalized: user@example.com
   Local part: user
   Domain: example.com
   Masked: u***r@example.com
   Invalid format: Email format is invalid.

2. Password
────────────────────────────────────────
   Password creation: Success
   Display: ********
   Verification (correct): True
   Verification (incorrect): False
   약한 비밀Number: Password is too weak. Must contain at least 3 of: uppercase, lowercase, digits, special characters.

3. PhoneNumber (Phone Number)
────────────────────────────────────────
   Normalized: +821012345678
   Formatted: 010-1234-5678
   Country code: +82
   Masked: +82 ***-****-5678

4. Username
────────────────────────────────────────
   Username: john_doe123
   Reserved: Reserved usernames cannot be used.
   Invalid format: Username must start with a letter and can only contain letters, digits, underscores (_), and hyphens (-).
```

## Project Description

### Project Structure
```
03-User-Management-Domain/
├── UserManagementDomain/
│   ├── Program.cs                      # 메인 실행 파days (4개 값 객체 구현)
│   └── UserManagementDomain.csproj     # 프로젝트 파days
└── README.md                           # 프로젝트 문서
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### value object별 Framework Type

각 value object가 상속하는 Framework Type과 주요 Characteristics을 정리한 것입니다.

| value object | Framework Type | Characteristics |
|--------|---------------|------|
| Email | SimpleValueObject\<string\> | Sequential validation, normalization, parsing |
| Password | IEquatable\<Password\> (independent implementation) | Strength validation, hash storage, verification |
| PhoneNumber | ValueObject | Normalization, formatting, masking |
| Username | SimpleValueObject\<string\> | Sequential validation, reserved word blocking |

## Summary at a Glance

### User Management Value Object Summary

각 value object의 속성, Validation Rules, Security Features을 한눈에 비교할 수 있습니다.

| value object | Key Properties | Validation Rules | Security Features |
|--------|----------|----------|----------|
| Email | Value | 이메days 형식, 254자 이하 | Masked |
| Password | Value (Hash) | 8-128 chars, strength 3/4+ | Hash storage, ToString masking |
| PhoneNumber | Value, CountryCode | 9-11 digit number | Masked |
| Username | Value | 3-30 chars, starts with letter, reserved words prohibited | None |

### 보안 Pattern

사용자 관리 도메인에서 활용된 보안 Pattern을 유형별로 minutes류하면 다음과 같습니다.

| Pattern | value object | Description |
|------|--------|------|
| Hash storage | Password | Does not store plain text |
| Masking | Email, PhoneNumber | 민감 정보 days부만 표시 |
| Reserved word blocking | Username | System-used names prohibited |
| Safe ToString | Password | Always returns "********" |

## FAQ

### Q1: How to use a stronger hash algorithm in Password?

In production, bcrypt or Argon2 should be used instead of SHA256. bcrypt는 솔트를 자동으로 생성하고, work factor로 계산  hours을 조절할 수 있어 브루트포스 공격에 더 강합니다.

```csharp
// BCrypt usage example (BCrypt.Net-Next package)
private static string HashPassword(string plainText)
{
    return BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);
}

public bool Verify(string plainText)
{
    return BCrypt.Net.BCrypt.Verify(plainText, Value);
}
```

### Q2: How to add per-domain validation in Email?

Add domain-based validation logic to block specific domains or allow only whitelisted ones.

```csharp
public static Fin<Email> Create(string? value, EmailValidationOptions? options = null)
{
    // Basic validation...

    if (options?.BlockedDomains?.Contains(domain) == true)
        return DomainError.For<Email>(new BlockedDomain(), domain,
            $"Blocked email domain. Current value: '{domain}'");

    if (options?.AllowedDomains is not null && !options.AllowedDomains.Contains(domain))
        return DomainError.For<Email>(new NotAllowedDomain(), domain,
            $"Not allowed email domain. Current value: '{domain}'");

    return new Email(normalized);
}
```

### Q3: PhoneNumber에서 여러 국가 형식을 지 won하려면?

Define separate formatters per country.

```csharp
private static readonly Dictionary<string, Func<string, string>> Formatters = new()
{
    ["82"] = FormatKorean,
    ["1"] = FormatUS,
    ["44"] = FormatUK
};

private static string FormatKorean(string number)
{
    if (number.StartsWith("10") && number.Length == 10)
        return $"0{number[..2]}-{number[2..6]}-{number[6..]}";
    // ...
}

private static string FormatUS(string number)
{
    if (number.Length == 10)
        return $"({number[..3]}) {number[3..6]}-{number[6..]}";
    // ...
}
```

We have explored the value object implementation for the user management domain. Next chapter에서는 날짜 범위,  hours 슬롯, 반복 규칙 등  hours 관련 로직이 복잡한 days정/예약 도메인의 value object를 implements.

---

## Tests

This project includes unit tests.

### Tests 실행
```bash
cd UserManagementDomain.Tests.Unit
dotnet test
```

### Tests 구조
```
UserManagementDomain.Tests.Unit/
├── EmailTests.cs      # 이메days 형식 검증 테스트
├── PasswordTests.cs   # Password strength validation tests
├── PhoneNumberTests.cs # 전화번호 형식/Masking 테스트
└── UsernameTests.cs   # Username rule validation tests
```

### Key Test Cases

| Test Class | Test Content |
|-------------|-----------|
| EmailTests | Format validation, normalization, domain extraction |
| PasswordTests | Strength rules, HashedValue, character requirements |
| PhoneNumberTests | 형식 검증, 국가 코드, Masking |
| UsernameTests | Length limits, reserved word check, normalization |

---

We have implemented the user management domain value objects. Next chapter에서는 days정/예약 도메인에서 날짜 범위,  hours 슬롯, 반복 규칙 등  hours 기반 value object를 다룹니다.

→ [4장: days정/예약 도메인](../04-Scheduling-Domain/)
