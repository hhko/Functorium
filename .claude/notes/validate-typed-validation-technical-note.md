# Validate<T> 정적 제네릭 클래스 기술 노트

## 개요

값 객체 검증 시 타입 파라미터 반복 문제를 해결하기 위해 `ValidationRules` 정적 메서드 방식에서 `Validate<T>` 정적 제네릭 클래스 방식으로 리팩터링했습니다.

## 문제점

기존 `ValidationRules` 방식은 체이닝 시 매번 타입 파라미터를 반복해야 했습니다:

```csharp
// Before: <Email>이 4번 반복됨
public static Validation<Error, string> Validate(string? value) =>
    ValidationRules.NotEmpty<Email>(value ?? "")
        .ThenMatches<Email>(EmailPattern)
        .ThenMaxLength<Email>(254)
        .ThenNormalize(v => v.ToLowerInvariant());
```

**문제점:**
- 코드 중복으로 인한 가독성 저하
- 타입 변경 시 여러 곳 수정 필요
- 실수로 다른 타입 지정 가능성

## 해결책

### 핵심 아이디어

정적 제네릭 클래스와 타입 정보를 전달하는 wrapper struct를 조합하여 타입 파라미터를 한 번만 지정하도록 개선했습니다.

```csharp
// After: <Email>을 한 번만 지정
public static Validation<Error, string> Validate(string? value) =>
    Validate<Email>.NotEmpty(value ?? "")
        .ThenMatches(EmailPattern)
        .ThenMaxLength(254)
        .ThenNormalize(v => v.ToLowerInvariant());
```

### 구현 구조

```
┌─────────────────────────────────────────────────────────────────┐
│                         사용자 코드                              │
│  Validate<Email>.NotEmpty(value).ThenMaxLength(254)             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Validate<TValueObject>                        │
│  정적 제네릭 클래스 - 검증 시작점                                  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ NotEmpty(value) → TypedValidation<TValueObject, string>   │  │
│  │ Positive(value) → TypedValidation<TValueObject, T>        │  │
│  │ ...                                                        │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│            TypedValidation<TValueObject, T>                      │
│  Wrapper Struct - 타입 정보 전달                                  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ Value: Validation<Error, T>  (내부 검증 결과)              │  │
│  │ implicit operator → Validation<Error, T> (암시적 변환)     │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│               TypedValidationExtensions                          │
│  확장 메서드 - 체이닝 지원                                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ThenMaxLength(n) - TValueObject 추론됨                     │  │
│  │ ThenMatches(pattern) - TValueObject 추론됨                 │  │
│  │ ThenNormalize(func) - TValueObject 추론됨                  │  │
│  │ ...                                                        │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 Validation<Error, T>                             │
│  최종 결과 - 암시적 변환으로 반환                                  │
└─────────────────────────────────────────────────────────────────┘
```

## 구현 상세

### 1. TypedValidation<TValueObject, T> Wrapper Struct

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    internal TypedValidation(Validation<Error, T> value) => Value = value;

    // 암시적 변환으로 Validation<Error, T>로 자연스럽게 반환
    public static implicit operator Validation<Error, T>(
        TypedValidation<TValueObject, T> typed) => typed.Value;
}
```

**설계 결정:**
- `readonly struct`: 값 타입으로 힙 할당 없이 스택에서 처리
- `internal` 생성자: 외부에서 직접 생성 방지, `Validate<T>`를 통해서만 생성
- 암시적 변환: 반환 타입 `Validation<Error, T>`와 자연스럽게 호환

### 2. Validate<TValueObject> 정적 제네릭 클래스

```csharp
public static class Validate<TValueObject>
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> NotEmpty(string value) =>
        new(NotEmptyInternal(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> NotEmptyInternal(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<TValueObject>(
                new Empty(),
                value,
                $"{typeof(TValueObject).Name} cannot be empty.");
}
```

**설계 결정:**
- 정적 제네릭 클래스: `Validate<Email>.NotEmpty()`처럼 타입을 클래스 수준에서 한 번 지정
- Public/Internal 메서드 분리:
  - Public: `TypedValidation` 반환 (체이닝용)
  - Internal: `Validation<Error, T>` 반환 (확장 메서드에서 재사용)
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]`: 성능 최적화

### 3. TypedValidationExtensions 확장 메서드

```csharp
public static class TypedValidationExtensions
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenMaxLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int maxLength) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.MaxLengthInternal(v, maxLength)));
}
```

**설계 결정:**
- 확장 메서드의 `this` 파라미터에서 `TValueObject` 추론
- `Bind`를 사용하여 이전 검증 실패 시 전파
- `Validate<TValueObject>.Internal` 메서드 재사용으로 로직 중복 제거

## 타입 추론 메커니즘

C# 컴파일러의 타입 추론 규칙을 활용:

```csharp
// 1. Validate<Email>.NotEmpty(value)
//    → TypedValidation<Email, string> 반환

// 2. .ThenMaxLength(254)
//    → 확장 메서드: ThenMaxLength<TValueObject>(this TypedValidation<TValueObject, string>, int)
//    → this 파라미터가 TypedValidation<Email, string>이므로
//    → TValueObject = Email로 추론됨

// 3. 최종 반환
//    → TypedValidation<Email, string>
//    → 암시적 변환으로 Validation<Error, string>
```

## API 비교

### 문자열 검증

| 기존 API | 새 API |
|---------|--------|
| `ValidationRules.NotEmpty<T>(value)` | `Validate<T>.NotEmpty(value)` |
| `.ThenMaxLength<T>(n)` | `.ThenMaxLength(n)` |
| `.ThenMinLength<T>(n)` | `.ThenMinLength(n)` |
| `.ThenExactLength<T>(n)` | `.ThenExactLength(n)` |
| `.ThenMatches<T>(regex)` | `.ThenMatches(regex)` |
| `.ThenNormalize(func)` | `.ThenNormalize(func)` |

### 숫자 검증

| 기존 API | 새 API |
|---------|--------|
| `ValidationRules.Positive<T, V>(value)` | `Validate<T>.Positive(value)` |
| `ValidationRules.NonNegative<T, V>(value)` | `Validate<T>.NonNegative(value)` |
| `.ThenAtMost<T, V>(max)` | `.ThenAtMost(max)` |
| `.ThenAtLeast<T, V>(min)` | `.ThenAtLeast(min)` |
| `.ThenBetween<T, V>(min, max)` | `.ThenBetween(min, max)` |

## 사용 예시

### Email 값 객체

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        Validate<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254)
            .ThenNormalize(v => v.ToLowerInvariant());
}
```

### Price 값 객체

```csharp
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    private Price(decimal value) : base(value) { }

    public static Fin<Price> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Price(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        Validate<Price>.Positive(value)
            .ThenAtMost(1_000_000m);
}
```

### 커스텀 검증

```csharp
public static Validation<Error, string> Validate(string? value) =>
    Validate<Currency>.NotEmpty(value ?? "")
        .ThenExactLength(3)
        .ThenMust(
            v => SupportedCurrencies.Contains(v),
            new Custom("Unsupported"),
            v => $"Currency '{v}' is not supported")
        .ThenNormalize(v => v.ToUpperInvariant());
```

## 성능 분석

### 1. 메모리 할당 분석

#### TypedValidation Struct 메모리 레이아웃

```
TypedValidation<TValueObject, T>
├── Value: Validation<Error, T>  (참조, 8 bytes on x64)
└── Total Size: 8 bytes

※ TValueObject는 phantom type으로 런타임에 메모리를 차지하지 않음
```

#### 할당 비교: Before vs After

```csharp
// Before: ValidationRules 방식
ValidationRules.NotEmpty<Email>(value)      // Validation<Error, string> 생성
    .ThenMatches<Email>(pattern)            // 새 Validation 생성 (Bind 결과)
    .ThenMaxLength<Email>(254)              // 새 Validation 생성 (Bind 결과)
    .ThenNormalize(v => v.ToLower());       // 새 Validation 생성 (Map 결과)

// After: Validate<T> 방식
Validate<Email>.NotEmpty(value)             // TypedValidation (스택) + Validation (힙)
    .ThenMatches(pattern)                   // TypedValidation (스택) + Validation (힙)
    .ThenMaxLength(254)                     // TypedValidation (스택) + Validation (힙)
    .ThenNormalize(v => v.ToLower());       // TypedValidation (스택) + Validation (힙)
```

**결론**: 두 방식 모두 `Validation<Error, T>` 객체는 동일하게 생성됨. `TypedValidation` wrapper는 스택에 할당되어 **추가 힙 할당 없음**.

#### Zero-Allocation 패턴

```csharp
// TypedValidation은 readonly struct이므로:
// 1. 박싱(boxing) 없음
// 2. 방어적 복사(defensive copy) 없음
// 3. 스택 할당만 발생

public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }  // 참조만 저장, 복사 없음
}
```

### 2. JIT 컴파일 및 제네릭 특수화

#### 제네릭 타입 특수화 (Generic Specialization)

.NET JIT는 참조 타입에 대해 코드를 공유하고, 값 타입에 대해 특수화합니다:

```
Validate<Email>.NotEmpty()     ─┐
Validate<Username>.NotEmpty()  ─┼─→ 동일한 네이티브 코드 공유 (참조 타입)
Validate<Password>.NotEmpty()  ─┘

Validate<int>.Positive()       ─→ int 전용 네이티브 코드
Validate<decimal>.Positive()   ─→ decimal 전용 네이티브 코드
```

**참조 타입 공유의 이점:**
- 코드 캐시 효율성 향상
- JIT 컴파일 시간 감소
- Instruction cache hit rate 향상

#### Phantom Type Parameter

`TValueObject`는 런타임에 사용되지 않는 phantom type:

```csharp
// typeof(TValueObject).Name은 에러 메시지 생성 시에만 사용
// JIT는 이를 상수로 최적화 가능
$"{typeof(TValueObject).Name} cannot be empty."

// 릴리스 빌드에서 문자열 보간이 상수 폴딩될 수 있음
```

### 3. 인라이닝 분석

#### AggressiveInlining 적용 효과

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static TypedValidation<TValueObject, string> NotEmpty(string value) =>
    new(NotEmptyInternal(value));
```

**인라이닝 전 (개념적 IL):**
```il
// 호출 시
call     Validate`1<Email>::NotEmpty(string)
// 메서드 내부
call     Validate`1<Email>::NotEmptyInternal(string)
newobj   TypedValidation`2<Email, string>::.ctor(Validation)
ret
```

**인라이닝 후 (최적화된 코드):**
```il
// 모든 호출이 인라인되어 단일 코드 블록으로
// call 오버헤드 제거
// 레지스터 최적화 가능
```

#### 인라이닝 조건

JIT가 인라이닝을 수행하는 조건:
- 메서드 바이트코드 크기 < 32 bytes (기본값)
- `AggressiveInlining` 힌트로 임계값 상향
- 호출 깊이 제한 내

```csharp
// 우리 메서드들은 매우 작아서 인라이닝에 적합
public static TypedValidation<TValueObject, string> NotEmpty(string value) =>
    new(NotEmptyInternal(value));  // ~10 bytes IL
```

### 4. 체이닝 깊이별 성능 특성

#### 검증 체인 실행 흐름

```
Validate<Email>.NotEmpty(value)
    │
    ▼ TypedValidation<Email, string> (스택: 8 bytes)
    │
.ThenMatches(pattern)
    │ Bind 호출 → 성공 시 계속, 실패 시 short-circuit
    ▼ TypedValidation<Email, string> (스택: 8 bytes, 이전 것 재사용 가능)
    │
.ThenMaxLength(254)
    │ Bind 호출
    ▼ TypedValidation<Email, string> (스택: 8 bytes)
    │
.ThenNormalize(...)
    │ Map 호출
    ▼ TypedValidation<Email, string> (스택: 8 bytes)
    │
implicit operator
    │
    ▼ Validation<Error, string> (최종 결과)
```

#### Short-Circuit 최적화

`Bind`는 실패 시 즉시 반환 (Railway-Oriented Programming):

```csharp
// Validation.Bind 내부 동작 (개념적)
public Validation<Error, B> Bind<B>(Func<A, Validation<Error, B>> f) =>
    IsSuccess ? f(SuccessValue) : Fail(Errors);  // 실패 시 f 호출 안 함
```

**성능 이점:**
- 첫 번째 검증 실패 시 나머지 검증 스킵
- 불필요한 정규식 매칭, 문자열 연산 회피

### 5. 캐시 친화성

#### 데이터 지역성 (Data Locality)

```
스택 프레임 (L1 캐시에 적합):
┌─────────────────────────────┐
│ value (string ref)          │ 8 bytes
│ TypedValidation (temp)      │ 8 bytes
│ pattern (Regex ref)         │ 8 bytes
│ maxLength (int)             │ 4 bytes
└─────────────────────────────┘
Total: ~28 bytes - L1 캐시 라인(64 bytes) 내 적합
```

#### 코드 지역성 (Code Locality)

인라이닝으로 검증 로직이 연속된 메모리에 배치:
- Instruction cache miss 감소
- Branch prediction 향상

### 6. Allocation-Free 경로 분석

#### 성공 경로 (Happy Path)

```csharp
// 검증 성공 시 할당
Validate<Email>.NotEmpty("test@example.com")  // Validation.Success 생성
    .ThenMaxLength(254)                        // 새 Validation.Success 생성
```

- `Validation<Error, T>`는 LanguageExt의 discriminated union
- 성공 시에도 객체 생성 필요 (LanguageExt 라이브러리 특성)
- **TypedValidation wrapper 자체는 할당 없음**

#### 실패 경로

```csharp
// 검증 실패 시 할당
Validate<Email>.NotEmpty("")  // Validation.Fail + Error 객체 생성
```

- `DomainError` 객체 생성 (에러 정보 포함)
- 에러 메시지 문자열 할당
- **실패는 예외적 상황이므로 허용 가능**

### 7. 벤치마크 시나리오

#### 측정 대상

```csharp
// Scenario 1: 단순 검증 (1단계)
Validate<Price>.Positive(100m);

// Scenario 2: 체이닝 (3단계)
Validate<Email>.NotEmpty(value)
    .ThenMatches(pattern)
    .ThenMaxLength(254);

// Scenario 3: 복잡한 체이닝 (5단계 + 정규화)
Validate<Email>.NotEmpty(value)
    .ThenMatches(pattern)
    .ThenMinLength(5)
    .ThenMaxLength(254)
    .ThenNormalize(v => v.ToLowerInvariant());
```

#### 예상 성능 특성

| 시나리오 | 힙 할당 | 스택 사용 | 상대 성능 |
|---------|--------|----------|----------|
| 단순 검증 | ~1 객체 | ~16 bytes | 기준 |
| 체이닝 3단계 | ~3 객체 | ~32 bytes | ~1.5x |
| 체이닝 5단계 | ~5 객체 | ~48 bytes | ~2x |

※ 힙 할당은 `Validation<Error, T>` 체인에서 발생 (LanguageExt 특성)
※ TypedValidation wrapper는 추가 할당 없음

### 8. 최적화 요약

| 최적화 기법 | 적용 | 효과 |
|------------|------|------|
| readonly struct | TypedValidation | 힙 할당 제거, 방어적 복사 방지 |
| AggressiveInlining | 모든 public/internal 메서드 | 호출 오버헤드 제거 |
| Phantom Type | TValueObject | 런타임 메모리 영향 없음 |
| 제네릭 코드 공유 | 참조 타입 TValueObject | JIT 효율성 |
| Short-circuit | Bind 사용 | 실패 시 조기 종료 |
| 스택 할당 | TypedValidation 체인 | GC 압력 감소 |

### 9. 잠재적 성능 이슈 및 회피

#### 피해야 할 패턴

```csharp
// BAD: 루프 내에서 매번 Regex 생성
for (var i = 0; i < 1000; i++)
{
    Validate<Email>.NotEmpty(emails[i])
        .ThenMatches(new Regex(@"pattern"));  // 매번 새 Regex 생성
}

// GOOD: Regex를 static readonly로 캐싱
private static readonly Regex EmailPattern = new(@"pattern", RegexOptions.Compiled);

for (var i = 0; i < 1000; i++)
{
    Validate<Email>.NotEmpty(emails[i])
        .ThenMatches(EmailPattern);  // 캐싱된 Regex 재사용
}
```

#### 람다 캡처 주의

```csharp
// BAD: 클로저로 인한 할당
var minLen = GetMinLength();
Validate<Name>.NotEmpty(value)
    .ThenMust(v => v.Length >= minLen, ...);  // 클로저 객체 생성

// GOOD: 파라미터로 전달
Validate<Name>.NotEmpty(value)
    .ThenMinLength(minLen);  // 클로저 없음
```

## Breaking Changes

이 변경은 **Breaking Change**입니다:

```csharp
// 마이그레이션 필요
// Before
ValidationRules.NotEmpty<Email>(value).ThenMaxLength<Email>(254)

// After
Validate<Email>.NotEmpty(value).ThenMaxLength(254)
```

## 파일 구조

```
Src/Functorium/Domains/ValueObjects/
├── TypedValidation.cs          # Wrapper struct 정의
├── Validate.cs                 # 정적 제네릭 클래스 + 검증 로직
└── TypedValidationExtensions.cs # 체이닝 확장 메서드
```

## 결론

`Validate<T>` 패턴은 다음과 같은 이점을 제공합니다:

1. **가독성 향상**: 타입 파라미터 반복 제거로 코드가 간결해짐
2. **유지보수성**: 타입 변경 시 한 곳만 수정
3. **타입 안전성**: 컴파일 타임에 타입 불일치 감지
4. **성능**: struct와 인라이닝으로 런타임 오버헤드 최소화
5. **호환성**: 암시적 변환으로 기존 `Validation<Error, T>` 타입과 호환

---

**관련 파일:**
- `Src/Functorium/Domains/ValueObjects/Validate.cs`
- `Src/Functorium/Domains/ValueObjects/TypedValidation.cs`
- `Src/Functorium/Domains/ValueObjects/TypedValidationExtensions.cs`
- `.claude/guides/valueobject-implementation-guide.md`
