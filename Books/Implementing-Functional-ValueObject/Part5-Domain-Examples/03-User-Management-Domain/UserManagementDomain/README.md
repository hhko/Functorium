# 사용자 관리 도메인 값 객체

사용자 관리 도메인에서 자주 사용되는 값 객체 구현 예제입니다.

## 학습 목표

1. **Email** - 형식 검증 및 정규화가 필요한 단순 값 객체
2. **Password** - 보안 해시와 검증 로직을 캡슐화한 값 객체
3. **PhoneNumber** - 국제 형식을 지원하는 전화번호 값 객체
4. **Username** - 예약어 검증이 포함된 사용자명 값 객체

## 실행

```bash
dotnet run
```

## 예상 출력

```
=== 사용자 관리 도메인 값 객체 ===

1. Email (이메일)
────────────────────────────────────────
   정규화: user@example.com
   로컬 파트: user
   도메인: example.com
   마스킹: u***r@example.com
   잘못된 형식: 유효한 이메일 형식이 아닙니다.

2. Password (비밀번호)
────────────────────────────────────────
   비밀번호 생성: 성공
   표시: ********
   검증(맞음): True
   검증(틀림): False
   약한 비밀번호: 비밀번호는 대문자, 소문자, 숫자, 특수문자 중 3가지 이상을 포함해야 합니다.

3. PhoneNumber (전화번호)
────────────────────────────────────────
   정규화: +821012345678
   포맷팅: 010-1234-5678
   국가 코드: +82
   마스킹: +82 ***-****-5678

4. Username (사용자명)
────────────────────────────────────────
   사용자명: john_doe123
   예약어: 'admin'은(는) 예약된 사용자명입니다.
   잘못된 형식: 사용자명은 영문자로 시작하고, 영문자, 숫자, 밑줄(_), 하이픈(-)만 포함할 수 있습니다.
```

## 값 객체 설명

### Email

이메일 주소를 표현하는 단순 값 객체입니다.

**특징:**
- 정규 표현식을 통한 형식 검증
- 소문자로 정규화하여 일관성 유지
- LocalPart와 Domain 분리 접근
- 마스킹 기능 (개인정보 보호)

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

비밀번호를 표현하는 값 객체입니다. 평문이 아닌 해시값을 저장합니다.

**특징:**
- 비밀번호 강도 검증 (대/소문자, 숫자, 특수문자)
- 해시 기반 저장 (평문 노출 방지)
- 검증 메서드 제공
- ToString에서 마스킹 출력

```csharp
public static Fin<Password> Create(string? plainText)
{
    // 강도 검증
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

국제 형식을 지원하는 전화번호 값 객체입니다.

**특징:**
- 국가 코드와 국내 번호 분리
- 다양한 입력 형식 정규화
- 로케일별 포맷팅
- 마스킹 기능

```csharp
public static Fin<PhoneNumber> Create(string? value, string defaultCountryCode = "82")
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainErrors.Empty;

    var digits = new string(value.Where(char.IsDigit).ToArray());

    // 국내 번호 0 제거
    if (digits.StartsWith("0"))
        digits = digits[1..];

    if (digits.Length < 9 || digits.Length > 11)
        return DomainErrors.InvalidFormat;

    return new PhoneNumber(defaultCountryCode, digits);
}
```

### Username

예약어 검증이 포함된 사용자명 값 객체입니다.

**특징:**
- 형식 규칙 (영문자로 시작, 특수문자 제한)
- 예약어 목록 검증
- 소문자로 정규화
- 길이 제한

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

## 핵심 패턴

### 1. 정규화 (Normalization)

입력값을 일관된 형식으로 변환하여 동등성 비교의 정확성을 보장합니다.

```csharp
// Email: 소문자 정규화
var normalized = value.Trim().ToLowerInvariant();

// PhoneNumber: 숫자만 추출
var digits = new string(value.Where(char.IsDigit).ToArray());
```

### 2. 마스킹 (Masking)

개인정보 보호를 위해 민감한 정보를 가립니다.

```csharp
public string Masked => $"{local[0]}***{local[^1]}@{Domain}";
```

### 3. 보안 해싱 (Security Hashing)

비밀번호와 같은 민감한 데이터는 해시로 저장합니다.

```csharp
private static string HashPassword(string plainText)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(plainText + "salt");
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

### 4. 예약어 검증 (Reserved Word Validation)

시스템에서 특별한 의미를 가지는 값을 제외합니다.

```csharp
private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
{
    "admin", "administrator", "root", "system"
};
```
