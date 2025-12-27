# 0.1 이 책을 읽어야 하는 이유

> **Part 0: 서론** | [목차](../README.md) | [다음: 0.2 성공 주도 개발이란? →](02-success-driven-development.md)

---

## 개요

이 책은 **함수형 프로그래밍 원칙을 적용한 값 객체(Value Object) 구현**을 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 기본적인 나눗셈 함수에서 시작하여 완성된 패턴까지, **27개의 실습 프로젝트**를 통해 함수형 값 객체의 모든 측면을 체계적으로 학습할 수 있습니다.

---

## 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기본 문법을 알고 함수형 프로그래밍에 입문하려는 개발자 | Part 1 (1장~6장) |
| 🟡 **중급** | 함수형 개념을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 전체 |
| 🔴 **고급** | 프레임워크 설계와 아키텍처에 관심 있는 개발자 | Part 4~5 + 부록 |

---

## 학습 전제 조건

이 책을 효과적으로 학습하기 위해 다음 지식이 필요합니다:

### 필수
- C# 기본 문법 이해 (클래스, 인터페이스, 제네릭)
- 객체지향 프로그래밍 기초 개념
- .NET 프로젝트 실행 경험

### 권장 (있으면 좋음)
- LINQ 기본 문법
- 단위 테스트 경험
- 도메인 주도 설계(DDD) 기초 개념

---

## 기대 효과

이 책을 완료하면 다음을 할 수 있습니다:

### 1. 예외 대신 명시적 결과 타입으로 안전한 코드 작성

```csharp
// ❌ 예외 기반 - 문제가 있는 방식
public User CreateUser(string email, int age)
{
    if (string.IsNullOrEmpty(email))
        throw new ArgumentException("이메일은 필수입니다.");
    return new User(email, age);
}

// ✅ 성공 주도 - 권장 방식
public Fin<User> CreateUser(string email, int age)
{
    return
        from validEmail in Email.Create(email)
        from validAge in Age.Create(age)
        select new User(validEmail, validAge);
}
```

### 2. 도메인 규칙을 타입으로 표현하여 컴파일 타임 검증

```csharp
// ❌ 런타임 검증 - 늦은 발견
public int Divide(int numerator, int denominator)
{
    if (denominator == 0)
        throw new ArgumentException("0으로 나눌 수 없습니다");
    return numerator / denominator;
}

// ✅ 컴파일 타임 보장 - 조기 발견
public int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value; // 검증 불필요!
}
```

### 3. Bind/Apply 패턴을 활용한 유연한 검증 로직 구현

```csharp
// Bind 패턴 - 순차 검증
var result = ValidateEmail(email)
    .Bind(_ => ValidatePassword(password))
    .Bind(_ => ValidateName(name));

// Apply 패턴 - 병렬 검증 (모든 에러 수집)
var result = (ValidateEmail(email), ValidatePassword(password), ValidateName(name))
    .Apply((e, p, n) => new User(e, p, n));
```

### 4. Functorium 프레임워크를 활용한 실전 값 객체 개발

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string value) =>
        CreateFromValidation(Validate(value), val => new Email(val));

    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainErrors.Empty(value)
            : !value.Contains("@")
                ? DomainErrors.InvalidFormat(value)
                : value;
}
```

---

## 세 가지 핵심 관점

이 책은 세 가지 관점을 통합하여 설명합니다:

| 관점 | 핵심 원칙 | 이 책에서의 적용 |
|------|----------|------------------|
| **성공 주도 개발** | 성공 경로를 중심으로 설계 | `Fin<T>`, `Validation<Error, T>` 활용 |
| **함수형 프로그래밍** | 순수 함수, 불변성, 조합 | 모나드 체이닝, LINQ 표현식 |
| **DDD 값 객체** | 도메인 개념의 타입화 | `ValueObject` 프레임워크 타입 |

---

## 학습 경로

```
┌─────────────────────────────────────────────────────────────────┐
│                        학습 로드맵                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  🟢 초급 (Part 1: 1~6장)                                        │
│  ├── 예외 vs 도메인 타입                                         │
│  ├── 방어적 프로그래밍                                           │
│  ├── Fin<T>, Validation<Error, T>                               │
│  └── 항상 유효한 값 객체                                         │
│                                                                  │
│  🟡 중급 (Part 1: 7~14장 + Part 2~3)                            │
│  ├── 값 동등성, 비교 가능성                                      │
│  ├── Bind/Apply 검증 패턴                                        │
│  └── 프레임워크 타입 활용                                        │
│                                                                  │
│  🔴 고급 (Part 4~5 + 부록)                                      │
│  ├── ORM, CQRS 통합                                             │
│  ├── 도메인별 실전 예제                                          │
│  └── 아키텍처 테스트                                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 다음 단계

환경 설정을 완료하고 첫 번째 예제를 실행해보세요.

→ [0.2 성공 주도 개발이란?](02-success-driven-development.md)
