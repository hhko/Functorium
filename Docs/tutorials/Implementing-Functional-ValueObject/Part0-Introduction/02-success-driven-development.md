# 0.2 성공 주도 개발이란?

> **Part 0: 서론** | [← 이전: 0.1 이 책을 읽어야 하는 이유](01-why-this-book.md) | [목차](../README.md) | [다음: 0.3 환경 설정 →](03-environment-setup.md)

---

## 개요

**성공 주도 개발(Success-Driven Development)**은 예외 대신 명시적인 결과 타입을 사용하여 성공 경로를 중심으로 코드를 설계하는 패러다임입니다.

---

## 예외 중심 vs 성공 중심 개발

### 예외 중심 개발의 문제점

전통적인 개발에서는 **예외(Exception)**를 사용하여 오류를 처리합니다:

```csharp
// ❌ 예외 중심 개발 - 문제가 있는 방식
public User CreateUser(string email, int age)
{
    if (string.IsNullOrEmpty(email))
        throw new ArgumentException("이메일은 필수입니다.");
    if (!email.Contains("@"))
        throw new ArgumentException("이메일 형식이 올바르지 않습니다.");
    if (age < 0 || age > 150)
        throw new ArgumentException("나이가 유효하지 않습니다.");

    return new User(email, age);
}

// 호출 측에서 예외 처리를 "기억해야" 함
try
{
    var user = CreateUser("invalid", -5);
}
catch (ArgumentException ex)
{
    // 예외 처리... 하지만 깜빡하면?
}
```

**문제점:**

| 문제 | 설명 |
|------|------|
| **깜빡할 수 있음** | 호출자가 예외 처리를 생략해도 컴파일러가 강제하지 않음 |
| **시그니처에서 알 수 없음** | 어떤 예외가 발생할지 함수 시그니처에서 알 수 없음 |
| **성능 비용** | 예외는 스택 트레이스 생성 등 높은 성능 비용 발생 |
| **순수 함수 위반** | 예외 발생은 부작용(Side Effect)으로 순수성 위반 |

---

### 성공 주도 개발의 해결책

**성공 주도 개발**은 이 문제를 해결합니다:

```csharp
// ✅ 성공 주도 개발 - 권장 방식
public Fin<User> CreateUser(string email, int age)
{
    return
        from validEmail in Email.Create(email)
        from validAge in Age.Create(age)
        select new User(validEmail, validAge);
}

// 호출 측에서 결과 처리가 "강제됨"
var result = CreateUser("user@example.com", 25);
result.Match(
    Succ: user => Console.WriteLine($"사용자 생성: {user.Email}"),
    Fail: error => Console.WriteLine($"실패: {error.Message}")
);
```

**장점:**

| 장점 | 설명 |
|------|------|
| **타입 시스템이 강제** | 결과 처리를 컴파일러가 강제 |
| **실패 가능성 명시** | 함수 시그니처가 실패 가능성을 명시 |
| **순수 함수 유지** | 예외 없이 순수 함수 유지 |
| **성능 최적화** | 예외 스택 트레이스 없음 |

---

## LanguageExt 라이브러리

이 책에서는 [LanguageExt](https://github.com/louthy/language-ext) 라이브러리를 사용합니다. LanguageExt는 C#에서 함수형 프로그래밍을 가능하게 하는 강력한 라이브러리입니다.

### 설치

```bash
dotnet add package LanguageExt.Core
```

### 기본 using 문

```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
```

---

## 핵심 타입: Fin<T>와 Validation<Error, T>

### Fin<T> - 최종 결과

`Fin<T>`는 **성공(Success)** 또는 **실패(Fail)**를 나타내는 타입입니다.

```csharp
// 성공 케이스
Fin<int> success = 42;                    // 암시적 변환
Fin<int> success2 = Fin<int>.Succ(42);    // 명시적 생성

// 실패 케이스
Fin<int> fail = Error.New("값이 유효하지 않습니다");
Fin<int> fail2 = Fin<int>.Fail(Error.New("오류"));

// 결과 처리
var result = Fin<int>.Succ(42);
var output = result.Match(
    Succ: value => $"성공: {value}",
    Fail: error => $"실패: {error.Message}"
);
```

### Validation<Error, T> - 검증 결과 (에러 누적)

`Validation<Error, T>`는 **모든 검증 오류를 수집**할 수 있는 타입입니다.

```csharp
// 단일 검증
Validation<Error, string> ValidateEmail(string email) =>
    email.Contains("@")
        ? email
        : Error.New("이메일에 @가 필요합니다");

// Apply를 통한 병렬 검증 (모든 에러 수집)
var result = (ValidateEmail(email), ValidateAge(age), ValidateName(name))
    .Apply((e, a, n) => new User(e, a, n));
// 실패 시 모든 에러가 ManyErrors로 수집됨
```

---

## Bind vs Apply 비교

| 구분 | Bind (순차 검증) | Apply (병렬 검증) |
|------|------------------|-------------------|
| **실행 방식** | 순차 실행 | 병렬 실행 |
| **에러 처리** | 첫 번째 에러에서 중단 | 모든 에러 수집 |
| **사용 시기** | 의존성 있는 검증 | 독립적인 검증 |
| **성능** | 조기 중단으로 효율적 | 모든 검증 실행 |
| **UX** | 하나씩 오류 표시 | 모든 오류 한 번에 표시 |

### Bind 패턴 예시

```csharp
// 순차 검증: 도로명 → 도시 → 우편번호 → 국가 일치
public static Validation<Error, Address> Validate(
    string street, string city, string postalCode, string country) =>
    ValidateStreetFormat(street)
        .Bind(_ => ValidateCityFormat(city))
        .Bind(_ => ValidatePostalCodeFormat(postalCode))
        .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
        .Map(_ => new Address(street, city, postalCode, country));
```

### Apply 패턴 예시

```csharp
// 병렬 검증: 모든 에러 수집
public static Validation<Error, User> Validate(
    string email, string password, string name) =>
    (ValidateEmail(email), ValidatePassword(password), ValidateName(name))
        .Apply((e, p, n) => new User(e, p, n))
        .As();
```

---

## 예외를 사용해야 하는 경우 vs 결과 타입을 사용해야 하는 경우

### 결과 타입 사용 (예상 가능한 실패)

- 사용자 입력 오류 (잘못된 이메일, 음수 나이 등)
- 비즈니스 규칙 위반 (0으로 나누기, 잘못된 날짜 등)
- 도메인 제약 조건 (최대값 초과, 최소값 미달 등)

### 예외 사용 (예측 불가능한 실패)

- 시스템 리소스 부족 (메모리 부족, 디스크 공간 부족)
- 외부 시스템 오류 (네트워크 연결 실패, 데이터베이스 연결 실패)
- 예상치 못한 시스템 오류 (파일 삭제, 권한 부족)

---

## 다음 단계

환경 설정을 완료하고 첫 번째 예제를 실행해보세요.

→ [0.3 환경 설정](03-environment-setup.md)
