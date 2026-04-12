---
title: "User Management Domain Value Objects"
---

Implementation examples of value objects commonly used in the user management domain.

## Learning Objectives

1. **Email** - Simple value object requiring format validation and normalization
2. **Password** - Value object encapsulating security hashing and validation logic
3. **PhoneNumber** - Phone number value object supporting international formats
4. **Username** - Username value object with reserved word validation

## Run

```bash
dotnet run
```

## Expected Output

```
=== User Management Domain Value Objects ===

1. Email
────────────────────────────────────────
   Normalized: user@example.com
   Local part: user
   Domain: example.com
   Masked: u***r@example.com
   Invalid format: Not a valid email format.

2. Password
────────────────────────────────────────
   Password creation: Success
   Display: ********
   Verify (correct): True
   Verify (incorrect): False
   Weak password: Password must contain at least 3 of: uppercase, lowercase, digits, special characters.

3. PhoneNumber
────────────────────────────────────────
   Normalized: +821012345678
   Formatted: 010-1234-5678
   Country code: +82
   Masked: +82 ***-****-5678

4. Username
────────────────────────────────────────
   Username: john_doe123
   Reserved word: 'admin' is a reserved username.
   Invalid format: Username must start with a letter and may only contain letters, digits, underscores (_), and hyphens (-).
```

## Value Object Descriptions

### Email

A simple value object representing an email address.

**Features:**
- Format validation via regular expression
- Normalization to lowercase for consistency
- Separate access to LocalPart and Domain
- Masking feature (for privacy protection)

```csharp
public static Fin<Email> Create(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainErrors.Empty;

    var normalized = value.Trim().ToLowerInvariant();

    if (normalized.Length > 254)
        return DomainErrors.TooLong;

    if (!Pattern.IsMatch(normalized))
        return DomainErrors.InvalidFormat;

    return new Email(normalized);
}
```

### Password

A value object representing a password. Stores hashed values, not plaintext.

**Features:**
- Password strength validation (uppercase, lowercase, digits, special characters)
- Hash-based storage (prevents plaintext exposure)
- Verification method provided
- Masked output from ToString

```csharp
public static Fin<Password> Create(string? plainText)
{
    // Strength validation
    var hasUpperCase = plainText.Any(char.IsUpper);
    var hasLowerCase = plainText.Any(char.IsLower);
    var hasDigit = plainText.Any(char.IsDigit);
    var hasSpecialChar = plainText.Any(c => !char.IsLetterOrDigit(c));

    var score = new[] { hasUpperCase, hasLowerCase, hasDigit, hasSpecialChar }
        .Count(x => x);

    if (score < 3)
        return DomainErrors.WeakPassword;

    return new Password(HashPassword(plainText));
}
```

### PhoneNumber

A phone number value object supporting international formats.

**Features:**
- Separation of country code and national number
- Normalization of various input formats
- Locale-specific formatting
- Masking feature

```csharp
public static Fin<PhoneNumber> Create(string? value, string defaultCountryCode = "82")
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainErrors.Empty;

    var digits = new string(value.Where(char.IsDigit).ToArray());

    // Remove leading 0 from national number
    if (digits.StartsWith("0"))
        digits = digits[1..];

    if (digits.Length < 9 || digits.Length > 11)
        return DomainErrors.InvalidFormat;

    return new PhoneNumber(defaultCountryCode, digits);
}
```

### Username

A username value object with reserved word validation.

**Features:**
- Format rules (must start with a letter, restricted special characters)
- Reserved word list validation
- Normalization to lowercase
- Length limits

```csharp
private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
{
    "admin", "administrator", "root", "system", "null", "undefined",
    "api", "www", "mail", "ftp", "support", "help"
};

public static Fin<Username> Create(string? value)
{
    // ...
    if (ReservedNames.Contains(normalized))
        return DomainErrors.Reserved(normalized);

    return new Username(normalized);
}
```

## Key Patterns

### 1. Normalization

Converts input values to a consistent format to guarantee accurate equality comparison.

```csharp
// Email: lowercase normalization
var normalized = value.Trim().ToLowerInvariant();

// PhoneNumber: extract digits only
var digits = new string(value.Where(char.IsDigit).ToArray());
```

### 2. Masking

Conceals sensitive information for privacy protection.

```csharp
public string Masked => $"{local[0]}***{local[^1]}@{Domain}";
```

### 3. Security Hashing

Sensitive data such as passwords are stored as hashes.

```csharp
private static string HashPassword(string plainText)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(plainText + "salt");
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

### 4. Reserved Word Validation

Excludes values that have special meaning in the system.

```csharp
private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
{
    "admin", "administrator", "root", "system"
};
```

## FAQ

### Q1: Why does `Email` normalize to lowercase?
**A**: According to the RFC standard, the local part of an email can be case-sensitive, but most mail servers do not distinguish case. Normalizing to lowercase ensures that `User@Example.com` and `user@example.com` are recognized as the same value object, making equality comparison accurate.

### Q2: Why does the `Password` value object store a hash value?
**A**: Keeping a plaintext password in memory risks exposure through log output or serialization. When the value object converts to a hash at creation time, the original password is never exposed even through `ToString()` or a debugger.

### Q3: Why does `Username` manage the reserved word list with a `HashSet`?
**A**: Reserved word validation runs every time a user registers. `List.Contains` is O(n), but `HashSet.Contains` is O(1), so performance remains constant even as the reserved word list grows. `StringComparer.OrdinalIgnoreCase` is used for case-insensitive comparison.

### Q4: Is the masking feature needed for all value objects?
**A**: No. It is only needed for value objects containing personal information (`Email`, `PhoneNumber`, `AccountNumber`, etc.). Non-sensitive data like `ProductCode` or `OrderStatus` does not require masking.

---

The next chapter covers value objects in the scheduling/reservation domain.

-> [5.4 Scheduling Domain](../../04-Scheduling-Domain/SchedulingDomain/)
