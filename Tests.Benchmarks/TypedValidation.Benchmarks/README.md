# Validate<T> 정적 제네릭 클래스 기술 노트

## 개요

이 벤치마크는 값 객체 검증을 위한 기존 `ValidationRules` 패턴과 새로운 `Validate<T>` 패턴을 비교합니다. 리팩터링을 통해 메서드 체이닝 시 반복되는 타입 파라미터를 제거했습니다.

## 문제점

기존 `ValidationRules` 방식은 매번 타입 파라미터를 지정해야 했습니다:

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

정적 제네릭 클래스와 wrapper struct를 조합하여 타입 추론을 가능하게 하고, 타입 파라미터를 한 번만 지정하도록 개선했습니다.

```csharp
// After: <Email>을 한 번만 지정
public static Validation<Error, string> Validate(string? value) =>
    Validate<Email>.NotEmpty(value ?? "")
        .ThenMatches(EmailPattern)
        .ThenMaxLength(254)
        .ThenNormalize(v => v.ToLowerInvariant());
```

### 구현 아키텍처

```
+---------------------------------------------------------------------+
|                           User Code                                 |
|   Validate<Email>.NotEmpty(value).ThenMaxLength(254)                |
+---------------------------------------------------------------------+
                                |
                                v
+---------------------------------------------------------------------+
|                     Validate<TValueObject>                          |
|   Static Generic Class - Validation Entry Point                     |
|   +---------------------------------------------------------------+ |
|   | NotEmpty(value) -> TypedValidation<TValueObject, string>      | |
|   | Positive(value) -> TypedValidation<TValueObject, T>           | |
|   | ...                                                           | |
|   +---------------------------------------------------------------+ |
+---------------------------------------------------------------------+
                                |
                                v
+---------------------------------------------------------------------+
|              TypedValidation<TValueObject, T>                       |
|   Wrapper Struct - Carries Type Information                         |
|   +---------------------------------------------------------------+ |
|   | Value: Validation<Error, T>  (internal validation result)     | |
|   | implicit operator -> Validation<Error, T> (implicit convert)  | |
|   +---------------------------------------------------------------+ |
+---------------------------------------------------------------------+
                                |
                                v
+---------------------------------------------------------------------+
|                 TypedValidationExtensions                           |
|   Extension Methods - Chaining Support                              |
|   +---------------------------------------------------------------+ |
|   | ThenMaxLength(n) - TValueObject inferred                      | |
|   | ThenMatches(pattern) - TValueObject inferred                  | |
|   | ThenNormalize(func) - TValueObject inferred                   | |
|   | ...                                                           | |
|   +---------------------------------------------------------------+ |
+---------------------------------------------------------------------+
                                |
                                v
+---------------------------------------------------------------------+
|                    Validation<Error, T>                             |
|   Final Result - Returned via Implicit Conversion                   |
+---------------------------------------------------------------------+
```

## 구현 상세

### 1. TypedValidation<TValueObject, T> Wrapper Struct

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    internal TypedValidation(Validation<Error, T> value) => Value = value;

    // Validation<Error, T>로 자연스럽게 반환하기 위한 암시적 변환
    public static implicit operator Validation<Error, T>(
        TypedValidation<TValueObject, T> typed) => typed.Value;
}
```

**설계 결정:**
- `readonly struct`: 힙 할당 없이 스택에서 처리되는 값 타입
- `internal` 생성자: 외부 인스턴스화 방지, `Validate<T>`를 통해서만 생성
- 암시적 변환: `Validation<Error, T>` 반환 타입과 자연스러운 호환성

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
- 정적 제네릭 클래스: `Validate<Email>.NotEmpty()`처럼 클래스 수준에서 타입을 한 번만 지정
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
- `Bind`를 사용하여 이전 검증 실패 전파
- `Validate<TValueObject>.Internal` 메서드 재사용으로 로직 중복 방지

## 타입 추론 메커니즘

C# 컴파일러의 타입 추론 규칙을 활용:

```csharp
// 1. Validate<Email>.NotEmpty(value)
//    -> TypedValidation<Email, string> 반환

// 2. .ThenMaxLength(254)
//    -> 확장 메서드: ThenMaxLength<TValueObject>(this TypedValidation<TValueObject, string>, int)
//    -> this 파라미터가 TypedValidation<Email, string>
//    -> TValueObject = Email로 추론됨

// 3. 최종 반환
//    -> TypedValidation<Email, string>
//    -> Validation<Error, string>으로 암시적 변환
```

## 성능 분석

### 1. 메모리 할당 분석

#### TypedValidation Struct 메모리 레이아웃

```
TypedValidation<TValueObject, T>
+-- Value: Validation<Error, T>  (참조, x64에서 8 bytes)
+-- Total Size: 8 bytes

* TValueObject는 phantom type - 런타임 메모리 비용 없음
```

#### 할당 비교: Before vs After

```csharp
// Before: ValidationRules 스타일
ValidationRules.NotEmpty<Email>(value)      // Validation<Error, string> 생성
    .ThenMatches<Email>(pattern)            // 새 Validation 생성 (Bind 결과)
    .ThenMaxLength<Email>(254)              // 새 Validation 생성 (Bind 결과)
    .ThenNormalize(v => v.ToLower());       // 새 Validation 생성 (Map 결과)

// After: Validate<T> 스타일
Validate<Email>.NotEmpty(value)             // TypedValidation (스택) + Validation (힙)
    .ThenMatches(pattern)                   // TypedValidation (스택) + Validation (힙)
    .ThenMaxLength(254)                     // TypedValidation (스택) + Validation (힙)
    .ThenNormalize(v => v.ToLower());       // TypedValidation (스택) + Validation (힙)
```

**결론**: 두 방식 모두 동일한 `Validation<Error, T>` 객체를 생성합니다. `TypedValidation` wrapper는 스택에 할당되어 **추가 힙 할당이 전혀 없습니다**.

### 2. JIT 컴파일 및 제네릭 특수화

#### 제네릭 타입 특수화

.NET JIT는 참조 타입에 대해 코드를 공유하고 값 타입에 대해 특수화합니다:

```
Validate<Email>.NotEmpty()     --+
Validate<Username>.NotEmpty()  --+--> 동일한 네이티브 코드 공유 (참조 타입)
Validate<Password>.NotEmpty()  --+

Validate<int>.Positive()       --> int 전용 네이티브 코드
Validate<decimal>.Positive()   --> decimal 전용 네이티브 코드
```

**참조 타입 공유의 이점:**
- 코드 캐시 효율성 향상
- JIT 컴파일 시간 감소
- Instruction cache hit rate 향상

#### Phantom Type Parameter

`TValueObject`는 런타임에 사용되지 않는 phantom type:

```csharp
// typeof(TValueObject).Name은 오류 메시지 생성 시에만 사용
// JIT가 이를 상수로 최적화 가능
$"{typeof(TValueObject).Name} cannot be empty."

// 릴리스 빌드에서 문자열 보간이 상수 폴딩될 수 있음
```

### 3. 인라이닝 분석

#### AggressiveInlining 효과

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static TypedValidation<TValueObject, string> NotEmpty(string value) =>
    new(NotEmptyInternal(value));
```

**인라이닝 전 (개념적 IL):**
```il
// 호출 지점
call     Validate`1<Email>::NotEmpty(string)
// 메서드 내부
call     Validate`1<Email>::NotEmptyInternal(string)
newobj   TypedValidation`2<Email, string>::.ctor(Validation)
ret
```

**인라이닝 후 (최적화된 코드):**
```il
// 모든 호출이 단일 코드 블록으로 인라인됨
// 호출 오버헤드 제거
// 레지스터 최적화 가능
```

### 4. Short-Circuit (단락 평가) 최적화

#### Short-Circuit이란?

**Short-circuit(단락 평가)**은 조건식에서 결과가 이미 확정되면 나머지 평가를 건너뛰는 최적화 기법입니다. 전기 회로에서 **단락(short circuit)**이 발생하면 전류가 원래 경로를 건너뛰고 짧은 경로로 흐르는 것에서 유래한 용어입니다.

```csharp
// 일반적인 short-circuit 예시

// AND 연산: 첫 번째가 false면 두 번째는 평가 안 함
if (user != null && user.IsActive)  // user가 null이면 IsActive 호출 안 함

// OR 연산: 첫 번째가 true면 두 번째는 평가 안 함
if (cached != null || LoadFromDb())  // cached가 있으면 DB 조회 안 함
```

#### Validation에서의 Short-Circuit

`Bind`는 실패 시 즉시 반환합니다 (Railway-Oriented Programming):

```csharp
// Validation.Bind 내부 동작 (개념적)
public Validation<Error, B> Bind<B>(Func<A, Validation<Error, B>> f) =>
    IsSuccess
        ? f(SuccessValue)  // 성공이면 다음 검증 실행
        : Fail(Errors);    // 실패면 즉시 반환 (f 호출 안 함)
```

#### 검증 체인에서의 동작

```csharp
Validate<Email>.NotEmpty(value)      // 1. Empty string fails here (returns Fail)
    .ThenMatches(EmailPattern)       // 2. If #1 fails -> regex check skipped
    .ThenMaxLength(254)              // 3. If #1 fails -> length check skipped
```

```
Success Path:
+----------+     +-------------+     +---------------+     +---------+
| NotEmpty | --> | ThenMatches | --> | ThenMaxLength | --> | Success |
+----------+     +-------------+     +---------------+     +---------+
    |                 |                     |
    v                 v                     v
  execute           execute              execute

Failure Path - Short-Circuit:
+----------+      +-------------+      +---------------+
| NotEmpty | -X-> | ThenMatches | -X-> | ThenMaxLength |
+----------+      +-------------+      +---------------+
    |
    v
  return Fail (remaining validations skipped)
```

#### 벤치마크로 확인된 효과

| 시나리오 | 시간 | 설명 |
|---------|-----:|------|
| Chain3_Failure_First | ~128 ns | 첫 번째에서 실패 → 나머지 2개 건너뜀 |
| Chain3_Failure_Last | ~313 ns | 마지막에서 실패 → 3개 모두 실행 |

**조기 실패가 약 2.5배 빠름** - 불필요한 정규식 매칭, 문자열 연산을 회피하기 때문입니다.

#### 성능 이점

- 첫 번째 실패 시 나머지 검증 건너뜀
- 불필요한 정규식 매칭, 문자열 연산 회피
- 비용이 큰 검증을 체인 뒤쪽에 배치하면 추가 최적화 가능

## 벤치마크 결과

### 환경

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7623/24H2/2024Update/HudsonValley)
Intel Core i7-1065G7 CPU 1.30GHz (Max: 1.50GHz), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.102
  [Host]   : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  WarmupCount=3
```

### 결과

| 메서드                        | 평균       | 오차       | 표준편차   | 비율  | 비율SD | Gen0   | 할당량    | 할당 비율 |
|----------------------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|
| Old_Chain3_Success           | 131.389 ns |  73.577 ns |  4.0330 ns | 17.55 |    1.53 | 0.0610 |     256 B |      8.00 |
| New_Chain3_Success           | 126.759 ns |  61.742 ns |  3.3843 ns | 16.93 |    1.46 | 0.0610 |     256 B |      8.00 |
| Old_Chain3_Failure_First     | 127.795 ns |  78.733 ns |  4.3156 ns | 17.07 |    1.50 | 0.1261 |     528 B |     16.50 |
| New_Chain3_Failure_First     | 137.722 ns | 217.089 ns | 11.8994 ns | 18.39 |    2.06 | 0.1261 |     528 B |     16.50 |
| Old_Chain3_Failure_Last      | 351.022 ns | 142.962 ns |  7.8362 ns | 46.88 |    4.00 | 0.0610 |     256 B |      8.00 |
| New_Chain3_Failure_Last      | 313.167 ns | 168.933 ns |  9.2598 ns | 41.83 |    3.64 | 0.0610 |     256 B |      8.00 |
| Old_Chain5_Success           | 178.776 ns |  51.039 ns |  2.7976 ns | 23.88 |    2.01 | 0.0937 |     392 B |     12.25 |
| New_Chain5_Success           | 181.352 ns | 148.907 ns |  8.1621 ns | 24.22 |    2.22 | 0.0937 |     392 B |     12.25 |
| Old_Chain5_Failure_First     | 157.861 ns | 174.756 ns |  9.5789 ns | 21.08 |    2.07 | 0.1588 |     664 B |     20.75 |
| New_Chain5_Failure_First     | 169.212 ns | 228.376 ns | 12.5180 ns | 22.60 |    2.37 | 0.1588 |     664 B |     20.75 |
| Old_NumericChain_Success     |  44.448 ns |  17.921 ns |  0.9823 ns |  5.94 |    0.51 | 0.0382 |     160 B |      5.00 |
| New_NumericChain_Success     |  44.383 ns |  20.613 ns |  1.1299 ns |  5.93 |    0.51 | 0.0382 |     160 B |      5.00 |
| Old_SimpleValidation_Success |   7.536 ns |  13.967 ns |  0.7656 ns |  1.01 |    0.12 | 0.0076 |      32 B |      1.00 |
| New_SimpleValidation_Success |   8.241 ns |   9.808 ns |  0.5376 ns |  1.10 |    0.11 | 0.0076 |      32 B |      1.00 |
| Old_SimpleValidation_Failure | 150.609 ns |  87.477 ns |  4.7949 ns | 20.12 |    1.76 | 0.0782 |     328 B |     10.25 |
| New_SimpleValidation_Failure | 135.455 ns |  88.431 ns |  4.8472 ns | 18.09 |    1.61 | 0.0782 |     328 B |     10.25 |

### 분석

**주요 발견:**

1. **성능 동등성**: 새로운 `Validate<T>` 패턴은 기존 `ValidationRules` 패턴과 동등한 성능을 보임
   - 단순 검증: ~7-8 ns (동일)
   - 3단계 체이닝: ~127-131 ns (오차 범위 내)
   - 5단계 체이닝: ~178-181 ns (오차 범위 내)
   - 숫자 체이닝: ~44 ns (동일)

2. **메모리 할당**: 두 패턴 모두 정확히 동일한 양의 메모리를 할당
   - 단순 검증: 32 B
   - 3단계 체이닝: 256 B
   - 5단계 체이닝: 392 B
   - `TypedValidation` wrapper는 **추가 할당이 전혀 없음** (스택 할당)

3. **Short-Circuit 최적화**: 검증이 초기에 실패하면 두 패턴 모두 short-circuit 동작의 이점을 얻음
   - Chain3_Failure_First (~128-138 ns) vs Chain3_Failure_Last (~313-351 ns)
   - 조기 실패가 후기 실패보다 2.5배 빠름

4. **실패 경로 오버헤드**: 실패 시 에러 객체 생성으로 인해 더 많은 메모리 할당 (528-664 B vs 256-392 B)

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

## 최적화 요약

| 최적화 기법 | 적용 대상 | 효과 |
|------------|----------|------|
| readonly struct | TypedValidation | 힙 할당 제거, 방어적 복사 방지 |
| AggressiveInlining | 모든 public/internal 메서드 | 호출 오버헤드 제거 |
| Phantom Type | TValueObject | 런타임 메모리 영향 없음 |
| 제네릭 코드 공유 | 참조 타입 TValueObject | JIT 효율성 |
| Short-circuit | Bind 사용 | 실패 시 조기 종료 |
| 스택 할당 | TypedValidation 체인 | GC 압력 감소 |

## 결론

`Validate<T>` 패턴은 다음을 제공합니다:

1. **가독성 향상**: 반복적인 타입 파라미터 제거
2. **유지보수성**: 타입 변경 시 단일 지점만 수정
3. **타입 안전성**: 컴파일 타임에 타입 불일치 감지
4. **성능**: struct와 인라이닝으로 최소한의 런타임 오버헤드
5. **호환성**: 기존 `Validation<Error, T>` 호환을 위한 암시적 변환

---

**관련 파일:**
- `Src/Functorium/Domains/ValueObjects/Validate.cs`
- `Src/Functorium/Domains/ValueObjects/TypedValidation.cs`
- `Src/Functorium/Domains/ValueObjects/TypedValidationExtensions.cs`
