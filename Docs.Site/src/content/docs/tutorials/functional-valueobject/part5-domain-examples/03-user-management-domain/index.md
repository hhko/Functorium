---
title: "User Management Domain"
---
## Overview

If "User@Example.COM" and "user@example.com" are treated as different emails, the same user can register multiple times. Handling passwords as `string` exposes plain text in logs and debuggers. If phone numbers "010-1234-5678" and "+82-10-1234-5678" are stored differently, search becomes impossible. In the user management domain, primitive types threaten both security and data quality.

In this chapter, we implement 4 core concepts needed for user authentication and profiles as value objects, guaranteeing normalization/masking/hashing at the type level.

- **Email**: Provides email format validation, normalization, and masking
- **Password**: Provides password strength validation, hash storage, and verification
- **PhoneNumber**: Provides phone number normalization, formatting, and masking
- **Username**: Provides username rule validation and reserved word blocking

## Learning Objectives

### **Core Learning Objectives**
- You can **implement a security-focused design** that stores only hashes without saving plain text in Password.
- You can **separate** input normalization and display formatting in Email and PhoneNumber.
- You can **implement a reserved word blocking pattern** in Username.
- You can **protect sensitive information** with Masked properties or safe ToString() in all value objects.

### **What You Will Verify Through Practice**
- Email normalization (lowercase conversion) and LocalPart/Domain parsing
- Password strength validation (uppercase, lowercase, digits, special characters) and hash verification
- PhoneNumber international format conversion and per-country formatting
- Username format rules and reserved word validation

## Why Is This Needed?

User management is a domain where security and data quality are particularly important. Handling user data with primitive types causes several problems.

Handling passwords as `string` can expose plain text in logs and debuggers, but the Password value object hashes at creation and `ToString()` always returns "********". If "User@Example.COM" and "user@example.com" are treated as different emails, duplicate registration becomes possible, but the Email value object always normalizes to lowercase to prevent this. Various phone number input formats ("010-1234-5678", "+82-10-1234-5678", etc.) are also handled by PhoneNumber maintaining a normalized internal format to guarantee consistent search.

## Core Concepts

### Email

Email validates and normalizes email addresses. It provides LocalPart and Domain parsing along with masking functionality.

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
            : DomainError.For<Email>(new DomainErrorKind.Empty(), value,
                $"Email address cannot be empty. Current value: '{value}'");

    public static implicit operator string(Email email) => email.Value;
}
```

Since it is always stored in lowercase, equality comparison becomes simple, and components can be easily accessed via the `LocalPart` and `Domain` properties. This is a pattern where normalization and parsing are combined.

### Password

Password validates password strength and stores hashed values. Plain text is never stored.

```csharp
public sealed class Password : IEquatable<Password>
{
    public sealed record InsufficientComplexity : DomainErrorKind.Custom;

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
            : DomainError.For<Password>(new DomainErrorKind.Empty(), value,
                $"Password cannot be empty. Current value: '{value}'");

    public bool Verify(string plainText) => Value == HashPassword(plainText);
    public override string ToString() => "********";  // Never expose plain text
}
```

At the time of `Create()`, the plain text is hashed, and only the hash is stored in the value object. Verification is done via `Verify()`, and `ToString()` always returns a masked value, completely blocking plain text exposure.

### PhoneNumber

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
            : DomainError.For<PhoneNumber>(new DomainErrorKind.Empty(), value,
                $"Phone number cannot be empty. Current value: '{value}'");

    public string Masked => $"+{CountryCode} ***-****-{NationalNumber[^4..]}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return NationalNumber;
    }
}
```

Various input formats (010-1234-5678, +82-10-1234-5678, etc.) are stored in a normalized international format, and `Formatted` provides a display format. This is a pattern for separating input format from storage format.

### Username

Username validates username rules and blocks reserved words.

```csharp
public sealed class Username : SimpleValueObject<string>
{
    public sealed record Reserved : DomainErrorKind.Custom;

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

Username rules (must start with a letter, length limits) and the reserved word list are defined within the value object and applied consistently.

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
   Weak password: Password is too weak. Must contain at least 3 of: uppercase, lowercase, digits, special characters.

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
│   ├── Program.cs                      # Main executable (4 value object implementations)
│   └── UserManagementDomain.csproj     # Project file
└── README.md                           # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### Framework Type per Value Object

Summarizes the framework type each value object inherits and its key characteristics.

| value object | Framework Type | Characteristics |
|--------|---------------|------|
| Email | SimpleValueObject\<string\> | Sequential validation, normalization, parsing |
| Password | IEquatable\<Password\> (independent implementation) | Strength validation, hash storage, verification |
| PhoneNumber | ValueObject | Normalization, formatting, masking |
| Username | SimpleValueObject\<string\> | Sequential validation, reserved word blocking |

## Summary at a Glance

### User Management Value Object Summary

You can compare the properties, validation rules, and security features of each value object at a glance.

| value object | Key Properties | Validation Rules | Security Features |
|--------|----------|----------|----------|
| Email | Value | Email format, 254 chars or less | Masked |
| Password | Value (Hash) | 8-128 chars, strength 3/4+ | Hash storage, ToString masking |
| PhoneNumber | Value, CountryCode | 9-11 digit number | Masked |
| Username | Value | 3-30 chars, starts with letter, reserved words prohibited | None |

### Security Patterns

The following classifies the security patterns used in the user management domain by type.

| Pattern | value object | Description |
|------|--------|------|
| Hash storage | Password | Does not store plain text |
| Masking | Email, PhoneNumber | Display only partial sensitive information |
| Reserved word blocking | Username | System-used names prohibited |
| Safe ToString | Password | Always returns "********" |

## FAQ

### Q1: How to use a stronger hash algorithm in Password?

In production, bcrypt or Argon2 should be used instead of SHA256. bcrypt automatically generates salts and allows adjusting computation time via work factor, making it more resistant to brute-force attacks.

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

### Q3: How to support multiple country formats in PhoneNumber?

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

We have explored the value object implementation for the user management domain. In the next chapter, we implement value objects for the scheduling/reservation domain where time-related logic is complex, including date ranges, time slots, and recurrence rules.

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd UserManagementDomain.Tests.Unit
dotnet test
```

### Test Structure
```
UserManagementDomain.Tests.Unit/
├── EmailTests.cs      # Email format validation tests
├── PasswordTests.cs   # Password strength validation tests
├── PhoneNumberTests.cs # Phone number format/masking tests
└── UsernameTests.cs   # Username rule validation tests
```

### Key Test Cases

| Test Class | Test Content |
|-------------|-----------|
| EmailTests | Format validation, normalization, domain extraction |
| PasswordTests | Strength rules, HashedValue, character requirements |
| PhoneNumberTests | Format validation, country code, masking |
| UsernameTests | Length limits, reserved word check, normalization |

---

We have implemented the user management domain value objects. In the next chapter, we cover time-based value objects in the scheduling/reservation domain, including date ranges, time slots, and recurrence rules.

→ [Chapter 4: Scheduling/Reservation Domain](../04-Scheduling-Domain/)
