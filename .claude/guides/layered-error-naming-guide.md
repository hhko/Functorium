# 레이어별 에러 이름 규칙 가이드

## 1. 개요

이 문서는 Functorium 프로젝트의 각 레이어(Domain, Application, Adapter)별 에러 이름을 정의할 때 따라야 할 명명 규칙을 정의합니다. 실용성과 일관성을 고려하여 **실용적 혼합 체계**를 채택하였습니다.

### 1.1 에러 코드 형식

```
{LayerPrefix}.{TypeName}.{ErrorName}
```

| 레이어 | 접두사 | 예시 |
|--------|--------|------|
| Domain | `DomainErrors` | `DomainErrors.Email.Empty` |
| Application | `ApplicationErrors` | `ApplicationErrors.CreateProductCommand.NotFound` |
| Adapter | `AdapterErrors` | `AdapterErrors.UsecaseValidationPipeline.PipelineValidation` |

---

## 2. 공통 네이밍 규칙

모든 레이어에 공통으로 적용되는 네이밍 규칙입니다.

### 빠른 참조: 네이밍 규칙 요약

| 규칙 | 적용 조건 | 패턴 | 예시 |
|------|----------|------|------|
| **R1** | 상태가 자명한 문제 | 상태 그대로 | `Empty`, `Null`, `Negative`, `Duplicate` |
| **R2** | 기준 대비 비교 | `Too-` / `Below-` / `Above-` / `OutOf-` | `TooShort`, `BelowMinimum`, `OutOfRange` |
| **R3** | 기대 조건 불충족 | `Not-` + 기대 | `NotPositive`, `NotUpperCase`, `NotFound` |
| **R4** | 이미 발생한 상태 | `Already-` + 상태 | `AlreadyExists` |
| **R5** | 형식/구조 문제 | `Invalid-` + 대상 | `InvalidFormat`, `InvalidState` |
| **R6** | 두 값 불일치 | `Mismatch` / `Wrong-` | `Mismatch`, `WrongLength` |
| **R7** | 권한/인증 문제 | 상태 그대로 | `Unauthorized`, `Forbidden` |
| **R8** | 작업/프로세스 문제 | 동사 과거분사 + 명사 | `ValidationFailed`, `OperationCancelled` |

---

## 3. 규칙 상세 설명

### 규칙 R1: 자명한 상태 → 상태 그대로

**적용 조건**: 그 자체로 "문제"임이 명백한 경우

**패턴**: 상태를 나타내는 형용사/명사

```csharp
// ✅ Correct - 상태가 곧 문제
new Empty()      // 비어있음 → 문제
new Null()       // null임 → 문제
new Negative()   // 음수임 → 문제
new Duplicate()  // 중복됨 → 문제

// ❌ Incorrect - 불필요한 부정
new NotFilled()  // Empty로 충분
new IsNull()     // Null로 충분
```

**판단 기준**: "값이 X다"라고 했을 때, X 자체가 문제 상황인가?

---

### 규칙 R2: 기준 대비 비교 → 비교 표현

**적용 조건**: 최소/최대/범위 등 기준값과 비교가 필요한 경우

**패턴**: `Too-`, `Below-`, `Above-`, `OutOf-`

```csharp
// ✅ Correct - 기준 대비 비교
new TooShort(MinLength: 8)        // 최소 길이 미만
new TooLong(MaxLength: 100)       // 최대 길이 초과
new BelowMinimum(Minimum: "0")    // 최소값 미만
new AboveMaximum(Maximum: "100")  // 최대값 초과
new OutOfRange(Min: "1", Max: "10") // 범위 밖

// ❌ Incorrect - 비교 표현 없음
new Short()      // 기준 불명확
new Long()       // 기준 불명확
```

**접두사 의미**:

| 접두사 | 의미 | 사용 상황 |
|--------|------|----------|
| `Too-` | 과도함/부족함 | 길이, 크기 등 상대적 비교 |
| `Below-` | 미만 | 최소 기준 미충족 |
| `Above-` | 초과 | 최대 기준 초과 |
| `OutOf-` | 범위 벗어남 | 허용 범위 외 |

**대칭 원칙**: `BelowMinimum` ↔ `AboveMaximum`, `TooShort` ↔ `TooLong`

---

### 규칙 R3: 기대 조건 불충족 → Not + 기대

**적용 조건**: "~여야 하는데 아님"을 표현해야 하는 경우

**패턴**: `Not-` + 기대 상태

```csharp
// ✅ Correct - 기대 부정
new NotPositive()   // 양수여야 함 (0도 에러)
new NotUpperCase()  // 대문자여야 함
new NotLowerCase()  // 소문자여야 함
new NotFound()      // 존재해야 함

// ❌ Incorrect - R1과 혼용
new Lowercase()     // 의미 모호
new Missing()       // NotFound가 더 명확
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
new AlreadyExists()  // 이미 존재함

// ❌ Incorrect
new Exists()         // "이미"가 빠지면 의미 약함
```

---

### 규칙 R5: 형식/구조/상태 문제 → Invalid + 대상

**적용 조건**: 값의 형식, 구조, 또는 상태가 유효하지 않은 경우

**패턴**: `Invalid-` + 대상

```csharp
// ✅ Correct - 형식/상태 문제
new InvalidFormat(Pattern: @"^\d{3}-\d{4}$")
new InvalidState()

// ❌ Incorrect - Invalid 남용
new InvalidLength()  // WrongLength 사용 (R6)
new InvalidValue()   // 너무 추상적
```

**주의**: `Invalid-` 접두사는 형식/구조/상태 문제에만 사용합니다. 남용하면 의미가 희석됩니다.

---

### 규칙 R6: 두 값 불일치 → Mismatch 또는 Wrong

**적용 조건**: 두 값이 일치해야 하는데 불일치하는 경우

**패턴**: `Mismatch` 또는 `Wrong-` + 대상

```csharp
// ✅ Correct - 불일치
new Mismatch()                    // 일반적인 불일치
new WrongLength(Expected: 10)     // 정확한 길이 불일치

// ❌ Incorrect
new NotMatching()    // Mismatch가 더 간결
new LengthMismatch() // WrongLength가 더 명확
```

| 패턴 | 사용 상황 |
|------|----------|
| `Mismatch` | 두 값 비교 (비밀번호 확인 등) |
| `Wrong-` | 기대한 정확한 값과 불일치 |

---

### 규칙 R7: 권한/인증 문제 → 상태 그대로

**적용 조건**: 인증/권한 관련 문제

**패턴**: HTTP 상태 코드와 일치하는 표준 용어

```csharp
// ✅ Correct - 권한/인증 문제
new Unauthorized()   // 401: 인증 필요
new Forbidden()      // 403: 접근 금지

// ❌ Incorrect
new NotAuthenticated()  // Unauthorized가 표준
new AccessDenied()      // Forbidden이 표준
```

---

### 규칙 R8: 작업/프로세스 문제 → 동사 과거분사 + 명사

**적용 조건**: 작업이나 프로세스 실행 중 발생한 문제

**패턴**: `{동사 과거분사}` 또는 `{명사}{동사 과거분사}`

```csharp
// ✅ Correct - 작업 실패
new ValidationFailed(PropertyName: "Email")
new OperationCancelled()
new BusinessRuleViolated(RuleName: "MaxOrderLimit")
new ConcurrencyConflict()

// ❌ Incorrect
new FailedValidation()  // 어순 불일치
new CancelledOperation() // OperationCancelled가 표준
```

---

## 4. Domain 레이어 에러

### 4.1 사용 시점

- Value Object 검증 실패
- Entity 불변성 위반
- Aggregate 비즈니스 규칙 위반

### 4.2 DomainErrorType 전체 목록

#### 값 존재 검증 (R1)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |

#### 문자열/컬렉션 길이 (R2, R6)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `TooShort` | 최소 길이 미만 | `new TooShort(MinLength: 8)` |
| `TooLong` | 최대 길이 초과 | `new TooLong(MaxLength: 100)` |
| `WrongLength` | 정확한 길이 불일치 | `new WrongLength(Expected: 10)` |

#### 형식 검증 (R5)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `InvalidFormat` | 형식 불일치 | `new InvalidFormat(Pattern: @"^\d+$")` |

#### 대소문자 검증 (R3)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `NotUpperCase` | 대문자가 아님 | `new NotUpperCase()` |
| `NotLowerCase` | 소문자가 아님 | `new NotLowerCase()` |

#### 숫자 범위 검증 (R1, R2, R3)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Negative` | 음수임 | `new Negative()` |
| `NotPositive` | 양수가 아님 | `new NotPositive()` |
| `OutOfRange` | 범위 밖 | `new OutOfRange(Min: "1", Max: "100")` |
| `BelowMinimum` | 최소값 미만 | `new BelowMinimum(Minimum: "0")` |
| `AboveMaximum` | 최대값 초과 | `new AboveMaximum(Maximum: "1000")` |

#### 존재 여부 검증 (R1, R3, R4)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |

#### 비교 (R6)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Mismatch` | 일치하지 않음 | `new Mismatch()` |

#### 커스텀

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Custom` | 도메인 특화 에러 | `new Custom("AlreadyShipped")` |

### 4.3 사용 예시

```csharp
using static Functorium.Domains.Errors.DomainErrorType;

// Value Object 검증
DomainError.For<Email>(new Empty(), "", "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), "abc", "Password too short");
DomainError.For<Currency>(new Custom("Unsupported"), "XYZ", "Currency not supported");
```

---

## 5. Application 레이어 에러

### 5.1 사용 시점

- 유스케이스(Command/Query) 실행 중 비즈니스 로직 오류
- 권한/인증 오류
- 데이터 조회 실패
- 동시성 충돌

### 5.2 ApplicationErrorType 전체 목록

#### 공통 에러 (R1, R3, R4)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |

#### 권한/인증 (R7)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### 검증/비즈니스 규칙 (R8)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `ValidationFailed` | 검증 실패 | `new ValidationFailed(PropertyName: "Email")` |
| `BusinessRuleViolated` | 비즈니스 규칙 위반 | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | 동시성 충돌 | `new ConcurrencyConflict()` |
| `ResourceLocked` | 리소스 잠금 | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | 작업 취소됨 | `new OperationCancelled()` |
| `InsufficientPermission` | 권한 부족 | `new InsufficientPermission(Permission: "Admin")` |

#### 커스텀

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Custom` | 애플리케이션 특화 에러 | `new Custom("PaymentDeclined")` |

### 5.3 사용 예시

```csharp
using static Functorium.Applications.Errors.ApplicationErrorType;

// 유스케이스 에러
ApplicationError.For<CreateProductCommand>(new AlreadyExists(), productId, "Product already exists");
ApplicationError.For<CancelOrderCommand>(new InvalidState(), orderId, "Cannot cancel shipped order");
ApplicationError.For<TransferCommand>(new BusinessRuleViolated("InsufficientBalance"), amount, "Insufficient balance");
```

---

## 6. Adapter 레이어 에러

### 6.1 사용 시점

- 파이프라인 검증/예외 처리
- 외부 서비스 호출 실패
- 데이터 직렬화/역직렬화 오류
- 연결/타임아웃 오류

### 6.2 AdapterErrorType 전체 목록

#### 공통 에러 (R1, R3, R4, R5, R7)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### Pipeline 관련 (R8)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `PipelineValidation` | 파이프라인 검증 실패 | `new PipelineValidation(PropertyName: "Id")` |
| `PipelineException` | 파이프라인 예외 발생 | `new PipelineException()` |

#### 외부 서비스 관련 (R1, R8)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `ExternalServiceUnavailable` | 외부 서비스 사용 불가 | `new ExternalServiceUnavailable(ServiceName: "PaymentGateway")` |
| `ConnectionFailed` | 연결 실패 | `new ConnectionFailed(Target: "database")` |
| `Timeout` | 타임아웃 | `new Timeout(Duration: TimeSpan.FromSeconds(30))` |

#### 데이터 관련 (R1, R8)

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Serialization` | 직렬화 실패 | `new Serialization(Format: "JSON")` |
| `Deserialization` | 역직렬화 실패 | `new Deserialization(Format: "XML")` |
| `DataCorruption` | 데이터 손상 | `new DataCorruption()` |

#### 커스텀

| 에러 | 의미 | 사용 예시 |
|------|------|----------|
| `Custom` | 어댑터 특화 에러 | `new Custom("RateLimited")` |

### 6.3 사용 예시

```csharp
using static Functorium.Adapters.Errors.AdapterErrorType;

// 파이프라인 에러
AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation("Name"), "", "Name is required");
AdapterError.FromException<UsecaseExceptionPipeline>(new PipelineException(), exception);

// 외부 서비스 에러
AdapterError.For<HttpClientAdapter>(new Timeout(Duration: TimeSpan.FromSeconds(30)), url, "Request timed out");
AdapterError.For<DatabaseAdapter>(new ConnectionFailed("PostgreSQL"), connectionString, "Connection failed");
```

---

## 7. Custom 에러 네이밍 가이드

### 7.1 언제 Custom을 사용하는가?

1. **표준 에러로 표현 불가능한 경우**: 도메인/애플리케이션/어댑터 특화 상황
2. **의미가 명확한 경우**: 에러 이름만으로 상황을 이해할 수 있을 때
3. **재사용 가능성이 낮은 경우**: 특정 상황에서만 발생하는 에러

### 7.2 Custom 에러 명명 규칙

```csharp
// ✅ Good - 명확하고 구체적
new Custom("AlreadyShipped")      // Domain: 이미 배송됨
new Custom("PaymentDeclined")     // Application: 결제 거부됨
new Custom("RateLimited")         // Adapter: 요청 제한 초과

// ❌ Bad - 모호하거나 너무 일반적
new Custom("Error")               // 의미 없음
new Custom("Failed")              // 너무 일반적
new Custom("Invalid")             // 구체적이지 않음
```

### 7.3 레이어별 Custom 에러 예시

| 레이어 | Custom 에러 예시 | 설명 |
|--------|-----------------|------|
| Domain | `AlreadyShipped`, `NotVerified`, `Expired` | 도메인 규칙 위반 |
| Application | `PaymentDeclined`, `QuotaExceeded`, `MaintenanceMode` | 비즈니스 프로세스 실패 |
| Adapter | `RateLimited`, `CircuitOpen`, `ServiceDegraded` | 인프라/외부 서비스 문제 |

---

## 8. 규칙 적용 플로우차트

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

5. 형식/구조/상태 문제인가?
   ├─ Yes → R5 (InvalidFormat, InvalidState)
   └─ No ↓

6. 두 값 불일치인가?
   ├─ Yes → R6 (Mismatch, WrongLength)
   └─ No ↓

7. 권한/인증 문제인가?
   ├─ Yes → R7 (Unauthorized, Forbidden)
   └─ No ↓

8. 작업/프로세스 실패인가?
   ├─ Yes → R8 (ValidationFailed, OperationCancelled)
   └─ No → Custom 사용
```

---

## 9. 체크리스트

새로운 에러 타입을 정의할 때 다음을 확인하세요:

- [ ] 적절한 레이어(Domain/Application/Adapter)를 선택했는가?
- [ ] 기존 표준 에러로 표현 가능한가? (Custom 남용 방지)
- [ ] 적절한 규칙(R1-R8)을 적용했는가?
- [ ] 대칭 쌍이 있다면 일관성을 유지했는가? (Below ↔ Above)
- [ ] 컨텍스트 정보가 필요한가? (MinLength, Pattern, PropertyName 등)
- [ ] 에러 메시지가 에러 이름과 일관성 있는가?

---

## 10. 레이어별 에러 타입 요약

### Domain (DomainErrorType)

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

### Application (ApplicationErrorType)

```
공통:        Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState
권한:        Unauthorized, Forbidden
검증:        ValidationFailed
비즈니스:    BusinessRuleViolated, ConcurrencyConflict, ResourceLocked,
             OperationCancelled, InsufficientPermission
커스텀:      Custom(Name)
```

### Adapter (AdapterErrorType)

```
공통:        Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState,
             Unauthorized, Forbidden
파이프라인:  PipelineValidation, PipelineException
외부서비스:  ExternalServiceUnavailable, ConnectionFailed, Timeout
데이터:      Serialization, Deserialization, DataCorruption
커스텀:      Custom(Name)
```

---

## 11. 참고 문서

- [layered-error-definition-guide.md](./layered-error-definition-guide.md) - 레이어별 에러 정의 방법
- [layered-error-testing-guide.md](./layered-error-testing-guide.md) - 레이어별 에러 테스트 방법

---

## 12. 변경 이력

| 날짜 | 변경 사항 | 작성자 |
|------|----------|--------|
| 2026-01-22 | 최초 작성 - Domain 에러 네이밍 규칙 | - |
| 2026-01-23 | 레이어별 에러 이름 규칙으로 확장 (Application, Adapter 추가) | - |
