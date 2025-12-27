# 사용자 관리 도메인

> **Part 5: 도메인별 실전 예제** | [← 이전: 금융 도메인](../02-Finance-Domain/README.md) | [목차](../../README.md) | [다음: 일정/예약 도메인 →](../04-Scheduling-Domain/README.md)

---

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 사용자 관리 도메인에서 자주 사용되는 4가지 핵심 값 객체를 구현합니다. 이메일, 비밀번호, 전화번호, 사용자명 등 사용자 인증과 프로필에 필요한 개념을 타입 안전하게 표현합니다.

구현되는 값 객체:
- **Email**: 이메일 형식 검증과 정규화, 마스킹 기능 제공
- **Password**: 비밀번호 강도 검증과 해시 저장, 검증 기능 제공
- **PhoneNumber**: 전화번호 정규화와 포맷팅, 마스킹 기능 제공
- **Username**: 사용자명 규칙 검증과 예약어 차단 기능 제공

## 학습 목표

### **핵심 학습 목표**
1. **보안 중심 설계**: Password에서 평문을 저장하지 않고 해시만 유지하는 패턴을 학습합니다.
2. **정규화와 포맷팅**: Email과 PhoneNumber에서 입력값 정규화와 표시용 포맷팅을 분리합니다.
3. **예약어 차단**: Username에서 시스템 예약어를 차단하는 패턴을 구현합니다.
4. **민감 정보 보호**: 모든 값 객체에서 Masked 속성이나 안전한 ToString()을 제공합니다.

### **실습을 통해 확인할 내용**
- Email의 정규화(소문자 변환)와 LocalPart/Domain 파싱
- Password의 강도 검증(대소문자, 숫자, 특수문자)과 해시 검증
- PhoneNumber의 국제 형식 변환과 국가별 포맷팅
- Username의 형식 규칙과 예약어 검증

## 왜 필요한가?

사용자 관리는 보안과 데이터 품질이 특히 중요한 도메인입니다. 원시 타입으로 사용자 데이터를 다루면 여러 문제가 발생합니다.

**첫 번째 문제는 보안 위험입니다.** 비밀번호를 `string`으로 다루면 로그나 디버거에 평문이 노출될 수 있습니다. Password 값 객체는 생성 시 해시하고, `ToString()`은 항상 "********"를 반환합니다.

**두 번째 문제는 데이터 중복입니다.** "User@Example.COM"과 "user@example.com"이 다른 이메일로 취급되면 같은 사용자가 중복 가입할 수 있습니다. Email 값 객체는 항상 소문자로 정규화합니다.

**세 번째 문제는 형식 불일치입니다.** 전화번호 "010-1234-5678", "01012345678", "+82-10-1234-5678"이 모두 같은 번호인데 다르게 저장되면 검색이 어렵습니다. PhoneNumber는 내부적으로 정규화된 형식을 유지합니다.

## 핵심 개념

### 첫 번째 개념: Email (이메일)

Email은 이메일 주소를 검증하고 정규화합니다. LocalPart, Domain 파싱과 마스킹 기능을 제공합니다.

```csharp
public sealed class Email : IEquatable<Email>
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

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
}
```

**핵심 아이디어는 "정규화와 파싱의 결합"입니다.** 항상 소문자로 저장하므로 동등성 비교가 단순해지고, `LocalPart`와 `Domain` 속성으로 구성 요소에 쉽게 접근할 수 있습니다.

### 두 번째 개념: Password (비밀번호)

Password는 비밀번호 강도를 검증하고 해시하여 저장합니다. 평문은 절대 저장하지 않습니다.

```csharp
public sealed class Password : IEquatable<Password>
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public string Value { get; }  // 해시된 값

    public static Fin<Password> Create(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return DomainErrors.Empty(plainText ?? "null");
        if (plainText.Length < MinLength)
            return DomainErrors.TooShort(plainText.Length);
        if (plainText.Length > MaxLength)
            return DomainErrors.TooLong(plainText.Length);

        // 강도 검사: 대문자, 소문자, 숫자, 특수문자 중 3가지 이상
        var hasUpperCase = plainText.Any(char.IsUpper);
        var hasLowerCase = plainText.Any(char.IsLower);
        var hasDigit = plainText.Any(char.IsDigit);
        var hasSpecialChar = plainText.Any(c => !char.IsLetterOrDigit(c));

        var score = new[] { hasUpperCase, hasLowerCase, hasDigit, hasSpecialChar }.Count(x => x);
        if (score < 3)
            return DomainErrors.WeakPassword(score);

        return new Password(HashPassword(plainText));
    }

    public bool Verify(string plainText) =>
        Value == HashPassword(plainText);

    public override string ToString() => "********";  // 절대 평문 노출 안 함
}
```

**핵심 아이디어는 "평문의 즉시 해시화"입니다.** `Create()` 시점에 평문을 해시하고, 값 객체에는 해시만 저장합니다. `Verify()`로 검증하고, `ToString()`은 항상 마스킹된 값을 반환합니다.

### 세 번째 개념: PhoneNumber (전화번호)

PhoneNumber는 전화번호를 국제 형식으로 정규화합니다. 국가별 포맷팅과 마스킹 기능을 제공합니다.

```csharp
public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }         // "+821012345678"
    public string CountryCode { get; }   // "82"
    public string NationalNumber { get; }  // "1012345678"

    public static Fin<PhoneNumber> Create(string? value, string defaultCountryCode = "82")
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0"))
            digits = digits[1..];  // 선행 0 제거

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
                // 한국 휴대폰: 010-XXXX-XXXX
                if (NationalNumber.Length == 10)
                    return $"0{NationalNumber[..2]}-{NationalNumber[2..6]}-{NationalNumber[6..]}";
                return $"0{NationalNumber[..2]}-{NationalNumber[2..5]}-{NationalNumber[5..]}";
            }
            return $"+{CountryCode} {NationalNumber}";
        }
    }

    public string Masked => $"+{CountryCode} ***-****-{NationalNumber[^4..]}";
}
```

**핵심 아이디어는 "입력 형식과 저장 형식의 분리"입니다.** 다양한 입력 형식(010-1234-5678, +82-10-1234-5678 등)을 정규화된 국제 형식으로 저장하고, `Formatted`로 표시용 형식을 제공합니다.

### 네 번째 개념: Username (사용자명)

Username은 사용자명 규칙을 검증하고 예약어를 차단합니다.

```csharp
public sealed class Username : IEquatable<Username>
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    private static readonly Regex Pattern = new(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "system", "null", "undefined",
        "api", "www", "mail", "ftp", "support", "help"
    };

    public string Value { get; }

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
}
```

**핵심 아이디어는 "비즈니스 규칙의 캡슐화"입니다.** 사용자명 규칙(영문자 시작, 길이 제한)과 예약어 목록이 값 객체 내부에 정의되어 일관되게 적용됩니다.

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

| 값 객체 | 프레임워크 타입 | 특징 |
|--------|---------------|------|
| Email | SimpleValueObject 패턴 | 정규식 검증, 정규화, 파싱 |
| Password | SimpleValueObject 패턴 | 강도 검증, 해시 저장, 검증 |
| PhoneNumber | ValueObject 패턴 | 정규화, 포맷팅, 마스킹 |
| Username | SimpleValueObject 패턴 | 규칙 검증, 예약어 차단 |

## 한눈에 보는 정리

### 사용자 관리 값 객체 요약

| 값 객체 | 주요 속성 | 검증 규칙 | 보안 기능 |
|--------|----------|----------|----------|
| Email | Value | 이메일 형식, 254자 이하 | Masked |
| Password | Value (해시) | 8~128자, 강도 3/4 이상 | 해시 저장, ToString 마스킹 |
| PhoneNumber | Value, CountryCode | 9~11자리 숫자 | Masked |
| Username | Value | 3~30자, 영문 시작, 예약어 금지 | 없음 |

### 보안 패턴

| 패턴 | 값 객체 | 설명 |
|------|--------|------|
| 해시 저장 | Password | 평문을 저장하지 않음 |
| 마스킹 | Email, PhoneNumber | 민감 정보 일부만 표시 |
| 예약어 차단 | Username | 시스템 사용 이름 금지 |
| 안전한 ToString | Password | 항상 "********" 반환 |

## FAQ

### Q1: Password에서 더 강력한 해시 알고리즘을 사용하려면?
**A**: 실제 프로덕션에서는 SHA256 대신 bcrypt나 Argon2를 사용해야 합니다.

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

bcrypt는 솔트를 자동으로 생성하고, work factor로 계산 시간을 조절할 수 있어 브루트포스 공격에 더 강합니다.

### Q2: Email에서 도메인별로 추가 검증을 하려면?
**A**: 도메인 기반 검증 로직을 추가할 수 있습니다.

```csharp
public static Fin<Email> Create(string? value, EmailValidationOptions? options = null)
{
    // 기본 검증...

    if (options?.BlockedDomains?.Contains(domain) == true)
        return DomainErrors.BlockedDomain(domain);

    if (options?.AllowedDomains is not null && !options.AllowedDomains.Contains(domain))
        return DomainErrors.NotAllowedDomain(domain);

    // MX 레코드 검증 (선택적)
    if (options?.ValidateMx == true && !HasValidMxRecord(domain))
        return DomainErrors.InvalidMxRecord(domain);

    return new Email(normalized);
}
```

### Q3: PhoneNumber에서 여러 국가 형식을 지원하려면?
**A**: 국가별 포맷터를 별도로 정의합니다.

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

### Q4: Username 예약어 목록을 동적으로 관리하려면?
**A**: 데이터베이스나 설정 파일에서 로드하는 방식을 사용합니다.

```csharp
public sealed class Username
{
    private static HashSet<string>? _reservedNames;

    private static HashSet<string> GetReservedNames()
    {
        return _reservedNames ??= LoadReservedNamesFromConfig();
    }

    private static HashSet<string> LoadReservedNamesFromConfig()
    {
        // 설정 파일이나 데이터베이스에서 로드
        var names = ConfigurationManager.GetSection("ReservedUsernames") as string[];
        return new HashSet<string>(names ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public static void RefreshReservedNames()
    {
        _reservedNames = null;  // 다음 호출 시 재로드
    }
}
```

### Q5: 값 객체에서 국제화(i18n) 에러 메시지를 지원하려면?
**A**: ErrorCodeFactory의 errorCode를 리소스 키로 사용합니다.

```csharp
internal static class DomainErrors
{
    public static Error Empty(string value) =>
        ErrorCodeFactory.Create(
            errorCode: "Email.Empty",  // 리소스 키
            errorCurrentValue: value,
            errorMessage: GetLocalizedMessage("Email.Empty"));

    private static string GetLocalizedMessage(string key)
    {
        // 현재 문화권에 맞는 메시지 반환
        return ResourceManager.GetString(key, CultureInfo.CurrentCulture)
            ?? $"Error: {key}";
    }
}
```

클라이언트에서는 errorCode를 키로 사용하여 로컬 언어의 메시지를 표시할 수 있습니다.

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
