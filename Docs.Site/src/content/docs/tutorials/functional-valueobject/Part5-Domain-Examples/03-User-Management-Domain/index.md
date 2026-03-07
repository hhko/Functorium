---
title: "사용자 관리 도메인"
---
## 개요

"User@Example.COM"과 "user@example.com"을 다른 이메일로 취급하면 같은 사용자가 중복 가입합니다. 비밀번호를 `string`으로 다루면 로그나 디버거에 평문이 노출됩니다. 전화번호 "010-1234-5678"과 "+82-10-1234-5678"이 다르게 저장되면 검색이 불가능합니다. 사용자 관리 도메인에서 원시 타입은 보안과 데이터 품질 모두를 위협합니다.

이 장에서는 사용자 인증과 프로필에 필요한 4가지 핵심 개념을 값 객체로 구현하여, 정규화/마스킹/해시를 타입 수준에서 보장합니다.

- **Email**: 이메일 형식 검증과 정규화, 마스킹 기능 제공
- **Password**: 비밀번호 강도 검증과 해시 저장, 검증 기능 제공
- **PhoneNumber**: 전화번호 정규화와 포맷팅, 마스킹 기능 제공
- **Username**: 사용자명 규칙 검증과 예약어 차단 기능 제공

## 학습 목표

### **핵심 학습 목표**
- Password에서 평문을 저장하지 않고 해시만 유지하는 **보안 중심 설계를 구현할 수** 있습니다.
- Email과 PhoneNumber에서 입력값 정규화와 표시용 포맷팅을 **분리할 수** 있습니다.
- Username에서 시스템 예약어를 **차단하는 패턴을 구현할 수** 있습니다.
- 모든 값 객체에서 Masked 속성이나 안전한 ToString()으로 **민감 정보를 보호할 수** 있습니다.

### **실습을 통해 확인할 내용**
- Email의 정규화(소문자 변환)와 LocalPart/Domain 파싱
- Password의 강도 검증(대소문자, 숫자, 특수문자)과 해시 검증
- PhoneNumber의 국제 형식 변환과 국가별 포맷팅
- Username의 형식 규칙과 예약어 검증

## 왜 필요한가?

사용자 관리는 보안과 데이터 품질이 특히 중요한 도메인입니다. 원시 타입으로 사용자 데이터를 다루면 여러 문제가 발생합니다.

비밀번호를 `string`으로 다루면 로그나 디버거에 평문이 노출될 수 있는데, Password 값 객체는 생성 시 해시하고 `ToString()`은 항상 "********"를 반환합니다. "User@Example.COM"과 "user@example.com"이 다른 이메일로 취급되면 중복 가입이 가능한데, Email 값 객체는 항상 소문자로 정규화하여 이를 방지합니다. 전화번호의 다양한 입력 형식("010-1234-5678", "+82-10-1234-5678" 등)도 PhoneNumber가 내부적으로 정규화된 형식을 유지하여 일관된 검색을 보장합니다.

## 핵심 개념

### Email (이메일)

Email은 이메일 주소를 검증하고 정규화합니다. LocalPart, Domain 파싱과 마스킹 기능을 제공합니다.

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Email(string value) : base(value) { }

    public string Address => Value;  // protected Value에 대한 public 접근자
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

항상 소문자로 저장하므로 동등성 비교가 단순해지고, `LocalPart`와 `Domain` 속성으로 구성 요소에 쉽게 접근할 수 있습니다. 정규화와 파싱이 결합된 패턴입니다.

### Password (비밀번호)

Password는 비밀번호 강도를 검증하고 해시하여 저장합니다. 평문은 절대 저장하지 않습니다.

```csharp
public sealed class Password : IEquatable<Password>
{
    public sealed record InsufficientComplexity : DomainErrorType.Custom;

    public const int MinLength = 8;
    public const int MaxLength = 128;

    public string Value { get; }  // 해시된 값

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
    public override string ToString() => "********";  // 절대 평문 노출 안 함
}
```

`Create()` 시점에 평문을 해시하고, 값 객체에는 해시만 저장합니다. `Verify()`로 검증하고, `ToString()`은 항상 마스킹된 값을 반환하여 평문 노출을 원천 차단합니다.

### PhoneNumber (전화번호)

PhoneNumber는 전화번호를 국제 형식으로 정규화합니다. 국가별 포맷팅과 마스킹 기능을 제공합니다.

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

다양한 입력 형식(010-1234-5678, +82-10-1234-5678 등)을 정규화된 국제 형식으로 저장하고, `Formatted`로 표시용 형식을 제공합니다. 입력 형식과 저장 형식의 분리 패턴입니다.

### Username (사용자명)

Username은 사용자명 규칙을 검증하고 예약어를 차단합니다.

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

    public string Name => Value;  // protected Value에 대한 public 접근자

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

사용자명 규칙(영문자 시작, 길이 제한)과 예약어 목록이 값 객체 내부에 정의되어 일관되게 적용됩니다.

## 실전 지침

### 예상 출력
```
=== 사용자 관리 도메인 값 객체 ===

1. Email (이메일)
────────────────────────────────────────
   정규화: user@example.com
   로컬 파트: user
   도메인: example.com
   마스킹: u***r@example.com
   잘못된 형식: 이메일 형식이 올바르지 않습니다.

2. Password (비밀번호)
────────────────────────────────────────
   비밀번호 생성: 성공
   표시: ********
   검증(맞음): True
   검증(틀림): False
   약한 비밀번호: 비밀번호가 너무 약합니다. 대문자, 소문자, 숫자, 특수문자 중 3가지 이상을 포함해야 합니다.

3. PhoneNumber (전화번호)
────────────────────────────────────────
   정규화: +821012345678
   포맷팅: 010-1234-5678
   국가 코드: +82
   마스킹: +82 ***-****-5678

4. Username (사용자명)
────────────────────────────────────────
   사용자명: john_doe123
   예약어: 예약된 사용자명은 사용할 수 없습니다.
   잘못된 형식: 사용자명은 영문자로 시작해야 하며, 영문자, 숫자, 밑줄(_), 하이픈(-)만 사용할 수 있습니다.
```

## 프로젝트 설명

### 프로젝트 구조
```
03-User-Management-Domain/
├── UserManagementDomain/
│   ├── Program.cs                      # 메인 실행 파일 (4개 값 객체 구현)
│   └── UserManagementDomain.csproj     # 프로젝트 파일
└── README.md                           # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### 값 객체별 프레임워크 타입

각 값 객체가 상속하는 프레임워크 타입과 주요 특징을 정리한 것입니다.

| 값 객체 | 프레임워크 타입 | 특징 |
|--------|---------------|------|
| Email | SimpleValueObject\<string\> | 순차 검증, 정규화, 파싱 |
| Password | IEquatable\<Password\> (독립 구현) | 강도 검증, 해시 저장, 검증 |
| PhoneNumber | ValueObject | 정규화, 포맷팅, 마스킹 |
| Username | SimpleValueObject\<string\> | 순차 검증, 예약어 차단 |

## 한눈에 보는 정리

### 사용자 관리 값 객체 요약

각 값 객체의 속성, 검증 규칙, 보안 기능을 한눈에 비교할 수 있습니다.

| 값 객체 | 주요 속성 | 검증 규칙 | 보안 기능 |
|--------|----------|----------|----------|
| Email | Value | 이메일 형식, 254자 이하 | Masked |
| Password | Value (해시) | 8~128자, 강도 3/4 이상 | 해시 저장, ToString 마스킹 |
| PhoneNumber | Value, CountryCode | 9~11자리 숫자 | Masked |
| Username | Value | 3~30자, 영문 시작, 예약어 금지 | 없음 |

### 보안 패턴

사용자 관리 도메인에서 활용된 보안 패턴을 유형별로 분류하면 다음과 같습니다.

| 패턴 | 값 객체 | 설명 |
|------|--------|------|
| 해시 저장 | Password | 평문을 저장하지 않음 |
| 마스킹 | Email, PhoneNumber | 민감 정보 일부만 표시 |
| 예약어 차단 | Username | 시스템 사용 이름 금지 |
| 안전한 ToString | Password | 항상 "********" 반환 |

## FAQ

### Q1: Password에서 더 강력한 해시 알고리즘을 사용하려면?

실제 프로덕션에서는 SHA256 대신 bcrypt나 Argon2를 사용해야 합니다. bcrypt는 솔트를 자동으로 생성하고, work factor로 계산 시간을 조절할 수 있어 브루트포스 공격에 더 강합니다.

```csharp
// BCrypt 사용 예시 (BCrypt.Net-Next 패키지)
private static string HashPassword(string plainText)
{
    return BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);
}

public bool Verify(string plainText)
{
    return BCrypt.Net.BCrypt.Verify(plainText, Value);
}
```

### Q2: Email에서 도메인별로 추가 검증을 하려면?

도메인 기반 검증 로직을 추가하여 특정 도메인을 차단하거나 허용 목록만 통과시킬 수 있습니다.

```csharp
public static Fin<Email> Create(string? value, EmailValidationOptions? options = null)
{
    // 기본 검증...

    if (options?.BlockedDomains?.Contains(domain) == true)
        return DomainError.For<Email>(new BlockedDomain(), domain,
            $"Blocked email domain. Current value: '{domain}'");

    if (options?.AllowedDomains is not null && !options.AllowedDomains.Contains(domain))
        return DomainError.For<Email>(new NotAllowedDomain(), domain,
            $"Not allowed email domain. Current value: '{domain}'");

    return new Email(normalized);
}
```

### Q3: PhoneNumber에서 여러 국가 형식을 지원하려면?

국가별 포맷터를 별도로 정의합니다.

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

사용자 관리 도메인의 값 객체 구현을 살펴보았습니다. 다음 장에서는 날짜 범위, 시간 슬롯, 반복 규칙 등 시간 관련 로직이 복잡한 일정/예약 도메인의 값 객체를 구현합니다.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd UserManagementDomain.Tests.Unit
dotnet test
```

### 테스트 구조
```
UserManagementDomain.Tests.Unit/
├── EmailTests.cs      # 이메일 형식 검증 테스트
├── PasswordTests.cs   # 비밀번호 강도 검증 테스트
├── PhoneNumberTests.cs # 전화번호 형식/마스킹 테스트
└── UsernameTests.cs   # 사용자명 규칙 검증 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| EmailTests | 형식 검증, 정규화, 도메인 추출 |
| PasswordTests | 강도 규칙, HashedValue, 문자 요구사항 |
| PhoneNumberTests | 형식 검증, 국가 코드, 마스킹 |
| UsernameTests | 길이 제한, 예약어 검사, 정규화 |
