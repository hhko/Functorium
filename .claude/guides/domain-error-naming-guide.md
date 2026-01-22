# Domain Error 네이밍 규칙 가이드

## 1. 개요

이 문서는 Functorium 프로젝트의 `DomainErrorType` 에러 이름을 정의할 때 따라야 할 명명 규칙을 정의합니다.
실용성과 일관성을 고려하여 **실용적 혼합 체계**를 채택하였습니다.

---

## 빠른 참조: 네이밍 규칙 요약

| 규칙 | 적용 조건 | 패턴 | 예시 |
|------|----------|------|------|
| **R1** | 상태가 자명한 문제 | 상태 그대로 | `Empty`, `Null`, `Negative`, `Duplicate` |
| **R2** | 기준 대비 비교 | `Too-` / `Below-` / `Above-` / `OutOf-` | `TooShort`, `BelowMinimum`, `AboveMaximum`, `OutOfRange` |
| **R3** | 기대 조건 불충족 | `Not-` + 기대 | `NotPositive`, `NotUpperCase`, `NotLowerCase`, `NotFound` |
| **R4** | 이미 발생한 상태 | `Already-` + 상태 | `AlreadyExists` |
| **R5** | 형식/구조 문제 | `Invalid-` + 대상 | `InvalidFormat` |
| **R6** | 두 값 불일치 | `Mismatch` / `Wrong-` | `Mismatch`, `WrongLength` |

### 전체 에러 타입 목록

```
값 존재:     Empty, Null
길이:        TooShort, TooLong, WrongLength
형식:        InvalidFormat
대소문자:    NotUpperCase, NotLowerCase
숫자 범위:   Negative, NotPositive, OutOfRange, BelowMinimum, AboveMaximum
존재 여부:   NotFound, AlreadyExists, Duplicate
비교:        Mismatch
커스텀:      Custom(Name)
```

---

## 2. 설계 철학

### 2.1 문제점

에러 이름을 정의할 때 여러 가지 접근법이 있습니다:

| 접근법 | 설명 | 예시 |
|--------|------|------|
| **현재 상태** | 값이 어떤 상태인지 | `Empty`, `Null`, `Negative` |
| **기대 위반** | 기대한 조건 불충족 | `MustBePositive`, `Required` |
| **부정** | 기대의 부정 | `NotPositive`, `NotFound` |
| **비교 결과** | 기준 대비 상태 | `TooShort`, `BelowMinimum` |

단일 원칙만 사용하면 일부 에러는 어색해집니다:
- **현재 상태만**: `Lowercase` → "대문자여야 하는데 소문자임"이 불명확
- **부정만**: `NotEmpty` → Empty가 에러인데 이상함
- **기대 위반만**: `MustBeLonger` → 너무 verbose

### 2.2 해결책: 실용적 혼합

상황에 맞는 가장 자연스러운 표현을 선택하되, **명확한 규칙**을 정의하여 일관성을 유지합니다.

## 3. 네이밍 규칙

### 규칙 R1: 자명한 상태 → 상태 그대로

**적용 조건**: 그 자체로 "문제"임이 명백한 경우

**패턴**: 상태를 나타내는 형용사/명사

```csharp
// ✅ Correct - 상태가 곧 문제
new DomainErrorType.Empty()      // 비어있음 → 문제
new DomainErrorType.Null()       // null임 → 문제
new DomainErrorType.Negative()   // 음수임 → 문제
new DomainErrorType.Duplicate()  // 중복됨 → 문제

// ❌ Incorrect - 불필요한 부정
new DomainErrorType.NotFilled()  // Empty로 충분
new DomainErrorType.IsNull()     // Null로 충분
```

**판단 기준**:
- "값이 X다"라고 했을 때, X 자체가 문제 상황인가?
- `Empty`, `Null`, `Negative`, `Duplicate`는 그 자체로 문제 상황을 명확히 표현

---

### 규칙 R2: 기준 대비 비교 → 비교 표현

**적용 조건**: 최소/최대/범위 등 기준값과 비교가 필요한 경우

**패턴**: `Too-`, `Below-`, `Above-`, `OutOf-`

```csharp
// ✅ Correct - 기준 대비 비교
new DomainErrorType.TooShort(MinLength: 8)     // 최소 길이 미만
new DomainErrorType.TooLong(MaxLength: 100)    // 최대 길이 초과
new DomainErrorType.BelowMinimum(Minimum: "0") // 최소값 미만
new DomainErrorType.AboveMaximum(Maximum: "100") // 최대값 초과
new DomainErrorType.OutOfRange(Min: "1", Max: "10") // 범위 밖

// ❌ Incorrect - 비교 표현 없음
new DomainErrorType.Short()      // 기준 불명확
new DomainErrorType.Long()       // 기준 불명확
new DomainErrorType.NotInRange() // R3 패턴과 혼용
```

**접두사 의미**:
| 접두사 | 의미 | 사용 상황 |
|--------|------|----------|
| `Too-` | 과도함/부족함 | 길이, 크기 등 상대적 비교 |
| `Below-` | 미만 | 최소 기준 미충족 |
| `Above-` | 초과 | 최대 기준 초과 |
| `OutOf-` | 범위 벗어남 | 허용 범위 외 |

**대칭 원칙**:
- `BelowMinimum` ↔ `AboveMaximum` (대칭 쌍)
- `TooShort` ↔ `TooLong` (대칭 쌍)

---

### 규칙 R3: 기대 조건 불충족 → Not + 기대

**적용 조건**: "~여야 하는데 아님"을 표현해야 하는 경우

**패턴**: `Not-` + 기대 상태

```csharp
// ✅ Correct - 기대 부정
new DomainErrorType.NotPositive()   // 양수여야 함 (0도 에러)
new DomainErrorType.NotUpperCase()  // 대문자여야 함
new DomainErrorType.NotLowerCase()  // 소문자여야 함
new DomainErrorType.NotFound()      // 존재해야 함

// ❌ Incorrect - R1과 혼용
new DomainErrorType.Lowercase()     // 의미 모호 (대문자? 소문자?)
new DomainErrorType.Missing()       // NotFound가 더 명확
new DomainErrorType.ZeroOrNegative() // NotPositive로 충분
```

**R1 vs R3 구분**:
| 상황 | 적용 규칙 | 이유 |
|------|----------|------|
| `Negative` | R1 | "음수임"이 명백한 문제 |
| `NotPositive` | R3 | "양수여야 함"인데 0도 포함해야 함 |
| `Empty` | R1 | "비어있음"이 명백한 문제 |
| `NotUpperCase` | R3 | "대문자여야 함"을 명시해야 명확 |

---

### 규칙 R4: 이미 발생한 상태 → Already + 상태

**적용 조건**: 이미 발생하여 되돌릴 수 없는 상태

**패턴**: `Already-` + 상태

```csharp
// ✅ Correct - 이미 발생
new DomainErrorType.AlreadyExists()  // 이미 존재함

// ❌ Incorrect
new DomainErrorType.Exists()         // "이미"가 빠지면 의미 약함
new DomainErrorType.Existing()       // 어색함
```

**R1 vs R4 구분**:
| 상황 | 적용 규칙 | 이유 |
|------|----------|------|
| `Duplicate` | R1 | 중복 "상태" 자체가 문제 |
| `AlreadyExists` | R4 | "이미 존재"하는 시점 강조 |

---

### 규칙 R5: 형식/구조 문제 → Invalid + 대상

**적용 조건**: 값의 형식이나 구조가 유효하지 않은 경우

**패턴**: `Invalid-` + 대상

```csharp
// ✅ Correct - 형식 문제
new DomainErrorType.InvalidFormat(Pattern: @"^\d{3}-\d{4}$")

// ❌ Incorrect
new DomainErrorType.BadFormat()      // Invalid가 표준
new DomainErrorType.Malformed()      // 덜 명확
new DomainErrorType.FormatError()    // Error 접미사 지양
```

**주의**: `Invalid-` 접두사는 형식/구조 문제에만 사용합니다. 남용하면 의미가 희석됩니다.

```csharp
// ❌ Invalid 남용
new DomainErrorType.InvalidLength()  // WrongLength 사용 (R6)
new DomainErrorType.InvalidValue()   // 너무 추상적
```

---

### 규칙 R6: 두 값 불일치 → Mismatch 또는 Wrong

**적용 조건**: 두 값이 일치해야 하는데 불일치하는 경우

**패턴**: `Mismatch` 또는 `Wrong-` + 대상

```csharp
// ✅ Correct - 불일치
new DomainErrorType.Mismatch()           // 일반적인 불일치
new DomainErrorType.WrongLength(Expected: 10)  // 정확한 길이 불일치

// ❌ Incorrect
new DomainErrorType.NotMatching()    // Mismatch가 더 간결
new DomainErrorType.LengthMismatch() // WrongLength가 더 명확
new DomainErrorType.InvalidLength()  // R5 패턴 남용
```

**Mismatch vs Wrong-**:
| 패턴 | 사용 상황 |
|------|----------|
| `Mismatch` | 두 값 비교 (비밀번호 확인 등) |
| `Wrong-` | 기대한 정확한 값과 불일치 |

---

## 4. 전체 에러 타입 목록

### 4.1 값 존재 (Presence) - R1

| 에러 | 의미 | 규칙 |
|------|------|------|
| `Empty` | 비어있음 (null, empty string, empty collection) | R1 |
| `Null` | null임 | R1 |

### 4.2 길이 (Length) - R2, R6

| 에러 | 의미 | 컨텍스트 | 규칙 |
|------|------|----------|------|
| `TooShort` | 최소 길이 미만 | `MinLength` | R2 |
| `TooLong` | 최대 길이 초과 | `MaxLength` | R2 |
| `WrongLength` | 정확한 길이 불일치 | `Expected` | R6 |

### 4.3 형식 (Format) - R5

| 에러 | 의미 | 컨텍스트 | 규칙 |
|------|------|----------|------|
| `InvalidFormat` | 형식 불일치 | `Pattern` | R5 |

### 4.4 대소문자 (Case) - R3

| 에러 | 의미 | 규칙 |
|------|------|------|
| `NotUpperCase` | 대문자가 아님 | R3 |
| `NotLowerCase` | 소문자가 아님 | R3 |

### 4.5 숫자 범위 (Numeric Range) - R1, R2, R3

| 에러 | 의미 | 컨텍스트 | 규칙 |
|------|------|----------|------|
| `Negative` | 음수임 | - | R1 |
| `NotPositive` | 양수가 아님 (0 포함) | - | R3 |
| `OutOfRange` | 범위 밖 | `Min`, `Max` | R2 |
| `BelowMinimum` | 최소값 미만 | `Minimum` | R2 |
| `AboveMaximum` | 최대값 초과 | `Maximum` | R2 |

### 4.6 존재 여부 (Existence) - R1, R3, R4

| 에러 | 의미 | 규칙 |
|------|------|------|
| `NotFound` | 찾을 수 없음 | R3 |
| `AlreadyExists` | 이미 존재함 | R4 |
| `Duplicate` | 중복됨 | R1 |

### 4.7 비교 (Comparison) - R6

| 에러 | 의미 | 규칙 |
|------|------|------|
| `Mismatch` | 일치하지 않음 | R6 |

### 4.8 커스텀 (Custom)

| 에러 | 의미 | 사용 |
|------|------|------|
| `Custom(Name)` | 도메인 특화 에러 | 표준 에러로 표현 불가 시 |

---

## 5. 사용 예시

### 5.1 값 객체 검증

```csharp
public static Validation<Error, string> NotEmpty<TValueObject>(string value) =>
    !string.IsNullOrWhiteSpace(value)
        ? value
        : DomainError.For<TValueObject>(
            new Empty(),  // R1: 자명한 상태
            value,
            $"{typeof(TValueObject).Name} cannot be empty");

public static Validation<Error, string> MinLength<TValueObject>(string value, int minLength) =>
    value.Length >= minLength
        ? value
        : DomainError.For<TValueObject>(
            new TooShort(minLength),  // R2: 기준 대비 비교
            value,
            $"Must be at least {minLength} characters");

public static Validation<Error, T> Positive<TValueObject, T>(T value)
    where T : INumber<T> =>
    value > T.Zero
        ? value
        : DomainError.For<TValueObject, T>(
            new NotPositive(),  // R3: 기대 불충족
            value,
            $"Must be positive");
```

### 5.2 체이닝 검증

```csharp
var result = ValidationRules.NotEmpty<Password>(value)
    .ThenMinLength<Password>(8)
    .ThenMatches<Password>(PasswordPattern)
    .ThenMust<Password, string>(
        v => v.Any(char.IsUpper),
        new NotUpperCase(),  // R3: 대문자 포함 필요
        "Must contain at least one uppercase letter");
```

### 5.3 커스텀 에러

```csharp
// 표준 에러로 표현 불가능한 도메인 특화 에러
DomainError.For<Order>(
    new Custom("AlreadyShipped"),  // 도메인 특화
    orderId,
    "Order has already been shipped");

DomainError.For<User>(
    new Custom("NotVerified"),  // 도메인 특화
    userId,
    "User email is not verified");
```

---

## 6. 규칙 적용 플로우차트

새로운 에러 타입을 정의할 때 다음 순서로 규칙을 적용합니다:

```
1. 상태 자체가 문제인가?
   ├─ Yes → R1 (Empty, Null, Negative, Duplicate)
   └─ No ↓

2. 기준값과 비교가 필요한가?
   ├─ Yes → R2 (TooShort, BelowMinimum, OutOfRange)
   └─ No ↓

3. "~여야 하는데 아님"인가?
   ├─ Yes → R3 (NotPositive, NotUpperCase, NotFound)
   └─ No ↓

4. 이미 발생한 상태인가?
   ├─ Yes → R4 (AlreadyExists)
   └─ No ↓

5. 형식/구조 문제인가?
   ├─ Yes → R5 (InvalidFormat)
   └─ No ↓

6. 두 값 불일치인가?
   ├─ Yes → R6 (Mismatch, WrongLength)
   └─ No → Custom 사용
```

---

## 7. 체크리스트

새로운 에러 타입을 정의할 때 다음을 확인하세요:

- [ ] 기존 표준 에러로 표현 가능한가? (Custom 남용 방지)
- [ ] 적절한 규칙(R1-R6)을 적용했는가?
- [ ] 대칭 쌍이 있다면 일관성을 유지했는가? (Below ↔ Above)
- [ ] 컨텍스트 정보가 필요한가? (MinLength, Pattern 등)
- [ ] 에러 메시지가 에러 이름과 일관성 있는가?

---

## 8. 규칙 요약표

| 규칙 | 적용 조건 | 패턴 | 예시 |
|------|----------|------|------|
| **R1** | 상태가 자명한 문제 | 상태 그대로 | `Empty`, `Null`, `Negative`, `Duplicate` |
| **R2** | 기준 대비 비교 | `Too-` / `Below-` / `Above-` / `OutOf-` | `TooShort`, `BelowMinimum`, `OutOfRange` |
| **R3** | 기대 조건 불충족 | `Not-` + 기대 | `NotPositive`, `NotUpperCase`, `NotFound` |
| **R4** | 이미 발생한 상태 | `Already-` + 상태 | `AlreadyExists` |
| **R5** | 형식/구조 문제 | `Invalid-` + 대상 | `InvalidFormat` |
| **R6** | 두 값 불일치 | `Mismatch` / `Wrong-` | `Mismatch`, `WrongLength` |

---

## 9. 변경 이력

| 날짜 | 변경 사항 | 작성자 |
|------|----------|--------|
| 2026-01-22 | 최초 작성 - 실용적 혼합 체계 수립 | - |
