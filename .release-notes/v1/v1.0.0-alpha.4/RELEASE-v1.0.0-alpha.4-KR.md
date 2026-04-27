# Functorium Release v1.0.0-alpha.4

**[English](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.4/.release-notes/v1/v1.0.0-alpha.4/RELEASE-v1.0.0-alpha.4.md)** | **한국어**

**발표 자료**: [PDF](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.4/.release-notes/v1/v1.0.0-alpha.4/RELEASE-v1.0.0-alpha.4-KR.pdf) | [PPTX](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.4/.release-notes/v1/v1.0.0-alpha.4/RELEASE-v1.0.0-alpha.4-KR.pptx) | [MP4](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.4/.release-notes/v1/v1.0.0-alpha.4/RELEASE-v1.0.0-alpha.4-KR.mp4) | [M4A](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.4/.release-notes/v1/v1.0.0-alpha.4/RELEASE-v1.0.0-alpha.4-KR.m4a)

## 개요

Functorium v1.0.0-alpha.4는 **에러 시스템 재설계 릴리스입니다.** alpha.1~alpha.3을 거치며 누적된 네이밍 중복 — `ErrorCode*`로 시작하는 6개 심볼과 `*ErrorType`으로 끝나는 5개 심볼 — 을 해소하기 위해 에러 타입 계층 전체를 일관성 있게 리네이밍했습니다. 이번 릴리스 이후 에러 시스템의 모든 역할이 고유한 이름을 갖습니다: 팩토리는 `Error`, 분류 record는 `Kind`, 코드 prefix는 짧은 레이어명만 사용합니다.

> **1.0 이전 안정성 공지**: 1.0.0-alpha 라인은 여전히 활발한 설계 단계입니다. 이후 alpha 릴리스에서도 구조적 이슈가 발견될 때마다 추가 Breaking Change가 발생할 수 있습니다. 프로덕션 도입은 1.0.0 정식 릴리스 이후로 권장합니다.

리네이밍과 함께 공개 API 표면도 정돈되었습니다 (외부 사용자가 0건이었던 `ErrorCodeFactory`와 prefix 상수 3개가 internal로 이동). Repository/Query 신규 기능 3가지(`FindAllSatisfying`, `FindFirstSatisfying`, `IQueryPort.Exists`/`Count`, `ConcurrencyConflict` 타입드 에러)가 함께 추가됩니다.

**주요 기능**:

- **에러 시스템 리네이밍 (Breaking)**: `ErrorType` -> `ErrorKind`, `ErrorCodeFactory` -> internal `ErrorFactory`, `ErrorCodeExpected/Exceptional` -> `ExpectedError/ExceptionalError`, 로그 필드명 변경(`ErrorType` -> `Kind`, `ErrorCodeId` -> `NumericCode`)
- **에러 코드 prefix 단축 (Breaking)**: `"DomainErrors.X.Y"` -> `"Domain.X.Y"`, `"ApplicationErrors.X.Y"` -> `"Application.X.Y"`, `"AdapterErrors.X.Y"` -> `"Adapter.X.Y"` — 운영 대시보드에서 기존 prefix로 필터링하던 쿼리는 마이그레이션 필요
- **아키텍처 테스트 계약 재구성 (Breaking)**: `IValueObject`/`IEntity`의 아키-테스트 상수가 중첩 `ArchTestContract` static class로 이동
- **Repository 신규 API**: `IRepository<TAggregate, TId>`에 Specification 기반 읽기 메서드 `FindAllSatisfying` / `FindFirstSatisfying` 추가
- **QueryPort 신규 API**: `IQueryPort<TEntity, TDto>`에 `Exists` / `Count` 추가, `IRepository`와 동일한 표면 제공
- **Concurrency Conflict 타입드 에러**: `EfCoreRepositoryBase.Update`가 `DbUpdateConcurrencyException`을 감지하여 타입드 `AdapterErrorKind.ConcurrencyConflict` 에러로 변환
- **Pipeline 취소 처리 수정**: `UsecaseExceptionPipeline`이 `OperationCanceledException`을 더 이상 삼키지 않아, 호스트 프레임워크(HTTP, gRPC)가 클라이언트 연결 해제를 인식 가능
- **Validation 다중 에러 보존**: `Validation<Error,T>` -> `Fin<T>` 변환 시 누적된 모든 에러를 보존 (이전에는 첫 번째 에러만 유지)

## Breaking Changes

### 1. `ErrorType` -> `ErrorKind` 3 레이어 전면 리네이밍

추상 기반 record와 3개 레이어 record가 모두 리네이밍되었습니다. 사용자 코드의 모든 커스텀 에러 정의, `For<T>(...)` 팩토리 호출, 에러명 파생용 virtual 프로퍼티에 영향을 줍니다.

**이전 (v1.0.0-alpha.3)**:
```csharp
// 추상 기반
public abstract partial record ErrorType
{
    public virtual string ErrorName { get; }
}

// 레이어 record
public abstract partial record DomainErrorType : ErrorType { ... }
public abstract partial record ApplicationErrorType : ErrorType { ... }
public abstract partial record AdapterErrorType : ErrorType { ... }

// 커스텀 에러
public sealed record InsufficientStock : DomainErrorType.Custom;

// 팩토리 호출
DomainError.For<Email>(new DomainErrorType.Empty(), value, "...");
```

**이후 (v1.0.0-alpha.4)**:
```csharp
// 추상 기반
public abstract partial record ErrorKind
{
    public virtual string Name { get; }
}

// 레이어 record
public abstract partial record DomainErrorKind : ErrorKind { ... }
public abstract partial record ApplicationErrorKind : ErrorKind { ... }
public abstract partial record AdapterErrorKind : ErrorKind { ... }

// 커스텀 에러
public sealed record InsufficientStock : DomainErrorKind.Custom;

// 팩토리 호출
DomainError.For<Email>(new DomainErrorKind.Empty(), value, "...");
```

**마이그레이션 가이드**:
1. `ErrorType` -> `ErrorKind`로 일괄 치환 (전역 find-and-replace로 대부분 처리)
2. `DomainErrorType` -> `DomainErrorKind`, `ApplicationErrorType` -> `ApplicationErrorKind`, `AdapterErrorType` -> `AdapterErrorKind`로 변경
3. `ErrorName`을 명시적으로 override한 코드는 `Name`으로 변경
4. `*ErrorType.Custom`을 상속한 커스텀 에러 record를 `*ErrorKind.Custom`으로 변경

<!-- 관련 커밋: b9396475 refactor(errors)!: ErrorType -> ErrorKind 전면 rename -->

---

### 2. 에러 코드 prefix 값 단축

방출되는 에러 코드의 prefix 부분이 짧은 레이어명만 사용하도록 단축되었습니다. `ErrorType`에 있던 3개의 `public const` prefix 필드는 공개 API에서 완전히 제거되었습니다 (이제 internal `ErrorCodePrefixes`에만 존재).

**이전 (v1.0.0-alpha.3)**:
```csharp
// ErrorType의 public 상수
public abstract partial record ErrorType
{
    public const string DomainErrorsPrefix = "DomainErrors";
    public const string ApplicationErrorsPrefix = "ApplicationErrors";
    public const string AdapterErrorsPrefix = "AdapterErrors";
}

// 방출되는 에러 코드
"DomainErrors.Email.Empty"
"ApplicationErrors.CreateProductCommand.AlreadyExists"
"AdapterErrors.ProductRepository.NotFound"
```

**이후 (v1.0.0-alpha.4)**:
```csharp
// 3개 public 상수 모두 공개 표면에서 제거
// 이제 internal 전용:
//   internal static class ErrorCodePrefixes
//   {
//       public const string Domain = "Domain";
//       public const string Application = "Application";
//       public const string Adapter = "Adapter";
//   }

// 방출되는 에러 코드
"Domain.Email.Empty"
"Application.CreateProductCommand.AlreadyExists"
"Adapter.ProductRepository.NotFound"
```

**마이그레이션 가이드**:
1. `DomainErrors.*`, `ApplicationErrors.*`, `AdapterErrors.*`로 필터링하는 운영 대시보드(Seq, Grafana, Elastic, Kibana) 쿼리를 `Domain.*`, `Application.*`, `Adapter.*`로 업데이트
2. `ErrorType.DomainErrorsPrefix` / `.ApplicationErrorsPrefix` / `.AdapterErrorsPrefix` 상수를 참조하는 코드 제거 — 이제 접근 불가능
3. 에러 코드 문자열을 하드코딩한 테스트 코드 업데이트 필요. 단, 레이어별 어설션 헬퍼(`ShouldBeDomainError`, `ShouldBeApplicationError`, `ShouldBeAdapterError`)는 prefix를 자동 계산하므로 변경 불필요

<!-- 관련 커밋: 21bc1f8b refactor(errors)!: 레이어 prefix를 internal ErrorCodePrefixes로 분리 + 값 단축 -->

---

### 3. `ErrorCodeFactory`를 internal `ErrorFactory`로 이동

기존 public이었던 `ErrorCodeFactory`가 `internal ErrorFactory`로 이동되었습니다. 메서드명도 `Create`/`CreateFromException`에서 `CreateExpected`/`CreateExceptional`로 변경되었고, `Format(...)` 메서드는 완전히 제거되었습니다. 외부 사용자는 alpha.1부터 권장된 호출 지점인 레이어 팩토리(`DomainError`/`ApplicationError`/`AdapterError`)를 그대로 사용하면 됩니다.

**이전 (v1.0.0-alpha.3)**:
```csharp
// public 클래스
public static class ErrorCodeFactory
{
    public static Error Create(string errorCode, string currentValue, string message);
    public static Error Create<T>(string errorCode, T currentValue, string message)
        where T : notnull;
    public static Error Create<T1, T2>(...);
    public static Error Create<T1, T2, T3>(...);
    public static Error CreateFromException(string errorCode, Exception exception);
    public static string Format(params string[] parts);
}

// 직접 호출 가능 (이제 불가)
var error = ErrorCodeFactory.Create("MyCustom.Code", value, "message");
```

**이후 (v1.0.0-alpha.4)**:
```csharp
// internal — 외부 어셈블리에서 보이지 않음
internal static class ErrorFactory
{
    public static Error CreateExpected(string errorCode, string currentValue, string message);
    public static Error CreateExpected<T>(string errorCode, T currentValue, string message)
        where T : notnull;
    // ...
    public static Error CreateExceptional(string errorCode, Exception exception);
    // Format(...) 제거됨
}

// 레이어 팩토리 사용 (이전 릴리스와 동일)
DomainError.For<Email>(new DomainErrorKind.Empty(), value, "message");
```

**마이그레이션 가이드**:
1. `ErrorCodeFactory.Create(...)`를 직접 호출하던 코드는 `DomainError.For<T>(...)`, `ApplicationError.For<T>(...)`, `AdapterError.For<T>(...)`로 전환
2. `ErrorCodeFactory.CreateFromException(...)`로 예외를 래핑하던 코드는 레이어 팩토리의 예외 오버로드로 처리
3. `ErrorCodeFactory.Format(...)` 참조 제거 — 헬퍼가 사라졌으므로 호출 지점에서 직접 문자열 결합

<!-- 관련 커밋: 8c057417 refactor(errors)!: ErrorCodeFactory를 internal ErrorFactory로; 049c0e26 refactor(errors): ErrorCodeFactory.Format 제거 -->

---

### 4. `ErrorCodeExpected`/`ErrorCodeExceptional` -> `ExpectedError`/`ExceptionalError`

`Error`의 서브클래스 record(및 짝을 이루는 Adapter destructurer와 Testing assertion)들이 `Expected` vs `Exceptional` 구분을 중심에 두는 이름으로 변경되었습니다.

**이전 (v1.0.0-alpha.3)**:
```csharp
// Adapter Serilog destructurer
public class ErrorCodeExpectedDestructurer : IErrorDestructurer { ... }
public class ErrorCodeExpectedTDestructurer : IErrorDestructurer { ... }
public class ErrorCodeExceptionalDestructurer : IErrorDestructurer { ... }

// Testing assertion
public static class ErrorCodeAssertions
{
    public static void ShouldBeErrorCodeExpected(this Error error, string expectedErrorCode, string expectedCurrentValue);
    public static void ShouldBeErrorCodeExpected<T>(this Error error, string expectedErrorCode, T expectedCurrentValue);
    public static void ShouldBeErrorCodeExpected<T1, T2>(...);
    public static void ShouldBeErrorCodeExpected<T1, T2, T3>(...);
}

public static class ErrorCodeExceptionalAssertions
{
    public static void ShouldBeErrorCodeExceptional(this Error error, string expectedErrorCode);
    public static void ShouldBeErrorCodeExceptional<TException>(this Error error, string expectedErrorCode)
        where TException : Exception;
    // ...
}
```

**이후 (v1.0.0-alpha.4)**:
```csharp
// Adapter Serilog destructurer
public class ExpectedErrorDestructurer : IErrorDestructurer { ... }
public class ExpectedErrorTDestructurer : IErrorDestructurer { ... }
public class ExceptionalErrorDestructurer : IErrorDestructurer { ... }

// Testing assertion
public static class ExpectedErrorAssertions
{
    public static void ShouldBeExpectedError(this Error error, string expectedErrorCode, string expectedCurrentValue);
    public static void ShouldBeExpectedError<T>(this Error error, string expectedErrorCode, T expectedCurrentValue);
    public static void ShouldBeExpectedError<T1, T2>(...);
    public static void ShouldBeExpectedError<T1, T2, T3>(...);
}

public static class ExceptionalErrorAssertions
{
    public static void ShouldBeExceptionalError(this Error error, string expectedErrorCode);
    public static void ShouldBeExceptionalError<TException>(this Error error, string expectedErrorCode)
        where TException : Exception;
    // ...
}
```

**마이그레이션 가이드**:
1. 모든 테스트 코드의 `ShouldBeErrorCodeExpected` -> `ShouldBeExpectedError`로 변경
2. `ShouldBeErrorCodeExceptional` -> `ShouldBeExceptionalError`로 변경
3. Serilog에 destructurer를 수동 등록한 경우 `ErrorCodeExpectedDestructurer` -> `ExpectedErrorDestructurer`, `ErrorCodeExceptionalDestructurer` -> `ExceptionalErrorDestructurer`로 변경
4. `ShouldBeExpected()` / `ShouldBeExceptional()` 판별 확장 메서드는 `ExpectedErrorAssertions`에 그대로 유지 (변경 불필요)

<!-- 관련 커밋: 6627c7fc refactor(errors)!: ErrorCodeExpected -> ExpectedError·ErrorCodeExceptional -> ExceptionalError rename -->

---

### 5. `ErrorCodeFieldNames` -> `ErrorLogFieldNames` + 로그 필드 키 리네이밍

Serilog 로그 필드명 레지스트리가 리네이밍되었고, 그중 2개 값이 변경되었습니다. 로그 스트림에서 이 속성명을 검색하는 운영 쿼리(Seq 필터, Elastic 쿼리, Loki LogQL)는 업데이트가 필요합니다.

**이전 (v1.0.0-alpha.3)**:
```csharp
// internal 클래스
internal static class ErrorCodeFieldNames
{
    public const string ErrorCode = "ErrorCode";
    public const string ErrorType = "ErrorType";        // record 분류 표시
    public const string ErrorCodeId = "ErrorCodeId";    // 숫자 에러 ID
    // ...
}

// 방출되는 로그 필드
{
  "ErrorCode": "Domain.Email.Empty",
  "ErrorType": "Empty",
  "ErrorCodeId": 1042
}
```

**이후 (v1.0.0-alpha.4)**:
```csharp
// internal 클래스
internal static class ErrorLogFieldNames
{
    public const string ErrorCode = "ErrorCode";
    public const string Kind = "Kind";                  // ErrorType에서 변경
    public const string NumericCode = "NumericCode";    // ErrorCodeId에서 변경
    // ...
}

// 방출되는 로그 필드
{
  "ErrorCode": "Domain.Email.Empty",
  "Kind": "Empty",
  "NumericCode": 1042
}
```

**마이그레이션 가이드**:
1. `@p.ErrorType`로 필터링하는 Seq / Grafana / Elastic / Loki 쿼리를 `@p.Kind`로 업데이트
2. `@p.ErrorCodeId`로 필터링하는 쿼리를 `@p.NumericCode`로 업데이트
3. `ErrorCode` 필드명은 변경되지 않으므로, `ErrorCode`만으로 필터링하는 대시보드는 업데이트 불필요
4. 클래스 자체는 `internal`이며 외부 직접 사용처가 없음

<!-- 관련 커밋: 2bd7c215 refactor(errors)!: ErrorCodeFieldNames -> ErrorLogFieldNames + NumericCode/Kind 리네이밍 -->

---

### 6. 아키텍처 테스트 계약 상수의 위치 변경

`IValueObject`와 `IEntity`는 이전에 팩토리 메서드명 상수(`CreateMethodName`, `ValidateMethodName` 등)를 인터페이스에 직접 노출했습니다. 이번 릴리스에서 중첩 `ArchTestContract` static class로 그룹핑되었습니다. `NestedErrorsClassName` 상수의 값도 변경 #2의 prefix 단축에 맞춰 `"DomainErrors"` -> `"Domain"`으로 변경되었습니다.

**이전 (v1.0.0-alpha.3)**:
```csharp
public interface IValueObject
{
    public const string CreateMethodName = "Create";
    public const string CreateFromValidatedMethodName = "CreateFromValidated";
    public const string ValidateMethodName = "Validate";
    public const string DomainErrorsNestedClassName = "DomainErrors";
}

public interface IEntity
{
    public const string CreateMethodName = "Create";
    public const string CreateFromValidatedMethodName = "CreateFromValidated";
}

// ArchUnit 테스트 사용 예
typeof(Email).GetMethod(IValueObject.CreateMethodName);
```

**이후 (v1.0.0-alpha.4)**:
```csharp
public interface IValueObject
{
    public static class ArchTestContract
    {
        public const string CreateMethodName = "Create";
        public const string CreateFromValidatedMethodName = "CreateFromValidated";
        public const string ValidateMethodName = "Validate";
        public const string NestedErrorsClassName = "Domain";    // 값도 변경: "DomainErrors" -> "Domain"
    }
}

public interface IEntity
{
    public static class ArchTestContract
    {
        public const string CreateMethodName = "Create";
        public const string CreateFromValidatedMethodName = "CreateFromValidated";
    }
}

// ArchUnit 테스트 사용 예
typeof(Email).GetMethod(IValueObject.ArchTestContract.CreateMethodName);
```

**마이그레이션 가이드**:
1. 인터페이스와 상수 이름 사이에 `.ArchTestContract` 추가: `IValueObject.CreateMethodName` -> `IValueObject.ArchTestContract.CreateMethodName`
2. `IEntity.CreateMethodName`, `CreateFromValidatedMethodName`, `ValidateMethodName`도 동일 적용
3. `IValueObject.DomainErrorsNestedClassName` -> `IValueObject.ArchTestContract.NestedErrorsClassName` (클래스 위치 변경 + 상수명 변경, 둘 다 주의)
4. 중첩 에러 클래스명을 `"DomainErrors"`로 단언하던 아키 테스트는 `"Domain"`을 기대하도록 업데이트

<!-- 관련 커밋: 216bc689 refactor(arch-contract)!: IValueObject·IEntity 아키테스트 상수를 ArchTestContract 중첩 클래스로 이동 -->

## 새로운 기능

### Functorium 라이브러리

#### 1. `IRepository.FindAllSatisfying` 및 `FindFirstSatisfying`

`IRepository<TAggregate, TId>`에 두 가지 새로운 Specification 기반 읽기 메서드가 추가되었습니다. `FindAllSatisfying`은 Specification에 매칭되는 모든 Aggregate를 반환하고(메모리 로드), `FindFirstSatisfying`은 첫 번째 매칭 Aggregate를 반환합니다(매칭이 없으면 `Option.None`). alpha.3에서 추가된 기존 Specification 메서드(`Exists`, `Count`, `DeleteBy`)와 짝을 이룹니다.

```csharp
// IRepository<TAggregate, TId> 인터페이스 (검증된 API 표면)
LanguageExt.FinT<LanguageExt.IO, LanguageExt.Seq<TAggregate>>
    FindAllSatisfying(Specification<TAggregate> spec);

LanguageExt.FinT<LanguageExt.IO, LanguageExt.Option<TAggregate>>
    FindFirstSatisfying(Specification<TAggregate> spec);

// 애플리케이션 서비스 사용 예
var spec = new ActiveOrdersByCustomerSpec(customerId);

// 매칭되는 모든 Aggregate
FinT<IO, Seq<Order>> activeOrders = orderRepository.FindAllSatisfying(spec);

// 첫 번째만 (EF Core에서 SELECT TOP 1 / LIMIT 1로 변환)
FinT<IO, Option<Order>> firstActive = orderRepository.FindFirstSatisfying(spec);
```

**Why this matters (왜 중요한가):**
- 이전에는 Specification에 매칭되는 Aggregate를 찾으려면 Query Port를 호출하거나(DTO만 반환, Aggregate 아님) `Exists`/`Count` 후 별도의 `GetByIds`를 합성해야 했음 — 두 번의 라운드트립
- `FindFirstSatisfying`은 EF Core에서 `FirstOrDefaultAsync`를 통해 `SELECT TOP 1` / `LIMIT 1`로 변환되므로, 한 건만 필요한 시나리오에서 전체 컬렉션을 materialize하는 비용을 회피
- 동일한 `Specification<TAggregate>` 인스턴스가 이제 `Exists`, `Count`, `DeleteBy`, `FindAllSatisfying`, `FindFirstSatisfying` 5가지 읽기/쓰기 모드를 모두 구동 — 하나의 Specification, 5가지 사용처
- `EfCoreRepositoryBase`와 `InMemoryRepositoryBase`가 모두 구현을 제공하므로, 테스트용 Repository와 운영용 Repository가 동일한 계약을 공유

<!-- 관련 커밋: 94f636b6 feat(repository): IRepository에 FindAllSatisfying·FindFirstSatisfying 추가 -->

---

#### 2. `IQueryPort`에 `Exists` 및 `Count` 추가

`IQueryPort<TEntity, TDto>` — DTO를 반환하는 읽기 전용 쿼리를 위한 Application 레이어 Port — 에 `Exists`와 `Count`가 추가되어, alpha.3에서 `IRepository`가 얻은 표면을 동일하게 갖추게 됩니다.

```csharp
// IQueryPort<TEntity, TDto> 인터페이스 (검증된 API 표면)
public interface IQueryPort<TEntity, TDto> : IObservablePort, IQueryPort
{
    LanguageExt.FinT<LanguageExt.IO, int> Count(Specification<TEntity> spec);
    LanguageExt.FinT<LanguageExt.IO, bool> Exists(Specification<TEntity> spec);
    // ... 기존 Search, SearchByCursor, Stream 메서드는 변경 없음
}

// 쿼리 핸들러 사용 예
public sealed class CountActiveOrdersQueryHandler
{
    public FinT<IO, int> Handle(CountActiveOrdersQuery query)
    {
        var spec = new ActiveOrdersSpec(query.CustomerId);
        return _orderQueryPort.Count(spec);
    }
}
```

**Why this matters (왜 중요한가):**
- 이전에는 Query Port에서 결과 개수를 얻으려면 `Search`를 호출하고 `PagedResult.TotalCount`를 읽는 방법밖에 없었음 — 페이지를 materialize하면서 동시에 카운트도 계산
- `Exists`는 `SELECT EXISTS (SELECT 1 FROM ...)`(EF Core에서는 `AnyAsync`)로 변환되므로, "매칭이 하나라도 있는가" 확인에 데이터 로드가 더 이상 필요 없음
- `Count`는 단일 `SELECT COUNT(*)`로 변환되므로, 총합만 필요한 대시보드나 페이지네이션 헤더는 정확히 한 번의 라운드트립으로 처리
- `InMemoryQueryBase`가 동일한 구현을 제공하므로, in-memory query port를 사용하는 단위 테스트가 운영 동작과 일관성을 유지

<!-- 관련 커밋: 56a62ac4 feat(query): IQueryPort에 Exists·Count 추가 -->

---

### Functorium.Adapters 라이브러리

#### 3. `EfCoreRepositoryBase.Update`의 `ConcurrencyConflict` 타입드 에러

`EfCoreRepositoryBase.Update(TAggregate)`가 이제 `DbUpdateConcurrencyException`(낙관적 동시성 토큰 불일치 시 EF Core가 던지는 예외)을 가로채어 타입드 `AdapterErrorKind.ConcurrencyConflict` 에러로 변환합니다. Application 레이어는 raw 예외 대신 엔티티 ID가 포함된 구조화된 에러를 받습니다. 서브클래스가 커스텀 경로(예: 명시적 `UpdateBy` 실패 복구)에서 동일한 타입드 에러를 발생시킬 수 있도록 `ConcurrencyConflictError(TId id)` protected 헬퍼도 함께 제공됩니다.

```csharp
// 신규 타입드 에러 record (검증된 API 표면)

// Application 레이어
public abstract record ApplicationErrorKind
{
    public sealed record ConcurrencyConflict : ApplicationErrorKind;
    // ...
}

// Adapter 레이어
public abstract record AdapterErrorKind
{
    public sealed record ConcurrencyConflict : AdapterErrorKind;
    // ...
}

// EfCoreRepositoryBase 헬퍼
protected LanguageExt.Common.Error ConcurrencyConflictError(TId id);

// 사용 예 — 호출자 코드 변경 불필요, 자동으로 에러가 등장
var result = await orderRepository.Update(modifiedOrder).RunAsync();

result.Match(
    Succ: order => /* 정상 갱신 */,
    Fail: error => error.Code switch
    {
        "Adapter.OrderRepository.ConcurrencyConflict"
            => /* fresh aggregate로 재시도, 또는 사용자에게 surface */,
        _ => /* 기타 실패 */
    });
```

**Why this matters (왜 중요한가):**
- 이전에는 `DbUpdateConcurrencyException`이 raw 예외로 전파되거나 일반적인 Adapter 에러로 변환되어, 호출자가 예외 타입을 검사해야 했음 — 애플리케이션 코드가 EF Core에 결합됨
- 동시성 충돌은 일상적인 비즈니스 상황(두 사용자가 같은 레코드를 동시 편집)이지 예외적인 결함이 아님. 타입드 에러 코드로 다루면 "재시도 vs 사용자에게 노출" 결정이 호출 지점에서 명시적으로 표현됨
- 에러 코드 `"Adapter.{RepositoryName}.ConcurrencyConflict"`는 구조화되어 있고 대시보드에서 쿼리 가능 — "Aggregate 타입별 동시성 충돌 비율" 같은 운영 메트릭이 자동으로 확보됨
- Application 레이어 코드는 이를 `ApplicationErrorKind.ConcurrencyConflict`로 매핑하여 HTTP 응답(예: 409 Conflict)으로 변환 가능, 상위 경계에 Adapter 레이어를 노출시키지 않음

<!-- 관련 커밋: a371f83d feat(adapter): ConcurrencyConflict typed error + EfCoreRepositoryBase.Update 감지 -->

## 버그 수정

### `UsecaseExceptionPipeline`이 취소 예외를 더 이상 삼키지 않음

`UsecaseExceptionPipeline`은 처리되지 않은 예외를 타입드 에러로 변환하는 프레임워크의 전역 catch-all입니다. 이전에는 `OperationCanceledException`도 함께 catch하여 호스트 프레임워크(ASP.NET Core, gRPC)로부터 클라이언트 연결 해제와 요청 타임아웃을 가렸습니다. 이제 파이프라인은 `OperationCanceledException`(및 그 파생인 `TaskCanceledException`)이 전파되도록 두어, 호스트가 취소된 작업을 인식하고 적절히 응답할 수 있게 됩니다(예: HTTP 499, gRPC `CANCELLED` 상태).

```csharp
// 파이프라인 동작 (개념적)
try
{
    return await next(request, ct);
}
catch (OperationCanceledException)
{
    throw;  // 신규: 변환하지 않고 전파
}
catch (Exception ex)
{
    return ToApplicationError(ex);
}
```

<!-- 관련 커밋: 15a84d47 fix(pipeline): UsecaseExceptionPipeline이 취소 예외를 삼키지 않도록 수정 -->

---

### `Validation -> Fin` 변환 시 모든 에러 보존

`Validation<Error,T>`(여러 에러 누적) -> `Fin<T>`(하나의 에러)로 변환할 때, 프레임워크는 이전에 시퀀스의 첫 번째 에러만 유지했습니다. 다중 에러 검증 결과 — `Validation`이 존재하는 이유 자체 — 가 변환 경계에서 조용히 truncate되었습니다. 이제 변환은 누적된 모든 에러를 `Error.Many`로 보존하여, 파이프라인 전체(`UsecaseLoggingPipeline`, `UsecaseExceptionPipeline`, 테스트 어설션)가 전체 에러 집합을 볼 수 있게 됩니다.

```csharp
// 이전: 첫 번째 에러만 살아남음
Validation<Error, Order> validation = ...; // 3개 에러 포함
Fin<Order> fin = validation.ToFin();        // fin.Error는 1개만 포함

// 이후: 변환 시 모든 에러 보존
Fin<Order> fin = validation.ToFin();        // fin.Error는 Error.Many(3개 에러)
```

<!-- 관련 커밋: a16107c3 fix(linq): Validation -> Fin 변환 시 모든 에러 보존 -->

## 기타 변경

### `FinTLinqExtensions` IO 전용 Validation 오버로드 제거

`FinTLinqExtensions`의 두 개 특수 `SelectMany` 오버로드 — `FinT<IO, A> -> Validation<Error, B>`와 `Validation<Error, A> -> FinT<IO, B>`의 IO 전용 변형 — 이 제거되었습니다. 임의의 모나드 `M`을 매개변수로 받는 일반 오버로드가 동일 시나리오를 모두 커버합니다. 공개 표면이 약간 축소되는 변경으로, 이 특정 오버로드를 명시적으로 사용한 코드(드뭄)가 있다면 동일 표현식 내에서 일반 버전이 컴파일됩니다.

<!-- 관련 커밋: d23503b3 refactor(linq): FinTLinqExtensions Validation의 IO 전용 오버로드 제거 -->

## API 변경 요약

### 리네이밍된 타입 (Functorium)

```
Functorium.Abstractions.Errors
├── ErrorType                            -> ErrorKind
├── ErrorCodeFactory (public)            -> ErrorFactory (internal)
├── ErrorCodeExpected                    -> ExpectedError
└── ErrorCodeExceptional                 -> ExceptionalError

Functorium.Domains.Errors
└── DomainErrorType                      -> DomainErrorKind

Functorium.Applications.Errors
└── ApplicationErrorType                 -> ApplicationErrorKind

Functorium.Adapters.Errors
└── AdapterErrorType                     -> AdapterErrorKind
```

### 리네이밍된 멤버 및 상수

```
Functorium.Abstractions.Errors.ErrorKind
├── virtual ErrorName -> Name
└── DomainErrorsPrefix / ApplicationErrorsPrefix / AdapterErrorsPrefix    [제거: 이제 internal]

Functorium.Abstractions.Errors.ErrorFactory
├── Create(...) -> CreateExpected(...)
├── CreateFromException(...) -> CreateExceptional(...)
└── Format(...)                                                            [제거]
```

### 리네이밍된 Adapter / Testing 동반자

```
Functorium.Adapters.Abstractions.Errors.DestructuringPolicies
├── ErrorCodeExpectedDestructurer        -> ExpectedErrorDestructurer
├── ErrorCodeExpectedTDestructurer       -> ExpectedErrorTDestructurer
└── ErrorCodeExceptionalDestructurer     -> ExceptionalErrorDestructurer

Functorium.Testing.Errors
├── ErrorCodeAssertions                  -> ExpectedErrorAssertions
│   ├── ShouldBeErrorCodeExpected        -> ShouldBeExpectedError
│   └── ShouldBeErrorCodeExpected<T>     -> ShouldBeExpectedError<T>
└── ErrorCodeExceptionalAssertions       -> ExceptionalErrorAssertions
    ├── ShouldBeErrorCodeExceptional     -> ShouldBeExceptionalError
    └── ShouldBeErrorCodeExceptional<T>  -> ShouldBeExceptionalError<T>
```

### 아키 테스트 계약 위치 변경 (Functorium)

```
Functorium.Domains.ValueObjects.IValueObject
├── CreateMethodName                     -> ArchTestContract.CreateMethodName
├── CreateFromValidatedMethodName        -> ArchTestContract.CreateFromValidatedMethodName
├── ValidateMethodName                   -> ArchTestContract.ValidateMethodName
└── DomainErrorsNestedClassName          -> ArchTestContract.NestedErrorsClassName  [값: "DomainErrors" -> "Domain"]

Functorium.Domains.Entities.IEntity
├── CreateMethodName                     -> ArchTestContract.CreateMethodName
└── CreateFromValidatedMethodName        -> ArchTestContract.CreateFromValidatedMethodName
```

### 신규 멤버 (Functorium)

```
Functorium.Domains.Repositories.IRepository<TAggregate, TId>
├── FindAllSatisfying(Specification<TAggregate>) -> FinT<IO, Seq<TAggregate>>      [신규]
└── FindFirstSatisfying(Specification<TAggregate>) -> FinT<IO, Option<TAggregate>> [신규]

Functorium.Applications.Queries.IQueryPort<TEntity, TDto>
├── Count(Specification<TEntity>) -> FinT<IO, int>                                 [신규]
└── Exists(Specification<TEntity>) -> FinT<IO, bool>                               [신규]
```

### 신규 멤버 (Functorium.Adapters)

```
Functorium.Adapters.Repositories.EfCoreRepositoryBase<TAggregate, TId, TModel>
├── ConcurrencyConflictError(TId) -> Error                                [신규: protected 헬퍼]
├── FindAllSatisfying(Specification<TAggregate>) -> FinT<IO, Seq<...>>    [신규: virtual]
└── FindFirstSatisfying(Specification<TAggregate>) -> FinT<IO, Option<...>> [신규: virtual]

Functorium.Adapters.Repositories.InMemoryRepositoryBase<TAggregate, TId>
├── FindAllSatisfying(Specification<TAggregate>) -> FinT<IO, Seq<...>>    [신규: virtual]
└── FindFirstSatisfying(Specification<TAggregate>) -> FinT<IO, Option<...>> [신규: virtual]

Functorium.Adapters.Repositories.InMemoryQueryBase<TEntity, TDto>
├── Count(Specification<TEntity>) -> FinT<IO, int>                        [신규: virtual]
└── Exists(Specification<TEntity>) -> FinT<IO, bool>                      [신규: virtual]
```

### 신규 ErrorKind

```
Functorium.Applications.Errors.ApplicationErrorKind
└── ConcurrencyConflict                  [신규]

Functorium.Adapters.Errors.AdapterErrorKind
└── ConcurrencyConflict                  [신규]
```

## 설치

### NuGet 패키지 설치

```bash
# Functorium 핵심 라이브러리
dotnet add package Functorium --version 1.0.0-alpha.4

# Functorium.Adapters (Repository, Pipeline, Observability)
dotnet add package Functorium.Adapters --version 1.0.0-alpha.4

# Functorium.SourceGenerators (빌드 타임 코드 생성)
dotnet add package Functorium.SourceGenerators --version 1.0.0-alpha.4

# Functorium.Testing (테스트 유틸리티, 선택)
dotnet add package Functorium.Testing --version 1.0.0-alpha.4
```

### 필수 의존성

- .NET 10 이상
- LanguageExt.Core 5.x
- Microsoft.EntityFrameworkCore.Relational (`EfCoreRepositoryBase` 사용 시 필요)
