# Adapter 구현 활동 가이드

이 문서는 Functorium 프레임워크에서 Adapter를 처음부터 끝까지 구현하는 **단계별 활동 가이드**입니다.
각 활동(Activity)은 "다음에 무엇을 해야 하나?"를 안내하며, 실제 참조 코드와 함께 구현 절차를 설명합니다.

> **개념/참조 문서**: 설계 원칙, 인터페이스 계층 구조, GeneratePipeline 소스 생성기 상세 등은 [adapter-guide.md](./adapter-guide.md)를 참조하세요.

### 전제 조건

- 도메인 모델링 기본 이해 ([domain-modeling-overview.md](./domain-modeling-overview.md))
- 값 객체(Value Object) 정의 방법 ([valueobject-guide.md](./valueobject-guide.md))
- Entity/Aggregate Root 기본 이해 ([entity-guide.md](./entity-guide.md))

---

## 목차

- [구현 라이프사이클 개요](#구현-라이프사이클-개요)
- [Activity 1: Port 인터페이스 정의](#activity-1-port-인터페이스-정의)
  - [1.1 위치 규칙](#11-위치-규칙)
  - [1.2 Port 정의 체크리스트](#12-port-정의-체크리스트)
  - [1.3 유형별 Port 정의 패턴](#13-유형별-port-정의-패턴)
- [Activity 2: Adapter 구현](#activity-2-adapter-구현)
  - [2.1 Repository Adapter](#21-repository-adapter)
  - [2.2 External API Adapter](#22-external-api-adapter)
  - [2.3 Messaging Adapter](#23-messaging-adapter)
  - [2.4 IO.lift vs IO.liftAsync 판단](#24-iolift-vs-ioliftasync-판단)
- [Activity 3: Pipeline 생성 확인](#activity-3-pipeline-생성-확인)
  - [3.1 생성 파일 확인](#31-생성-파일-확인)
  - [3.2 생성 코드 구조](#32-생성-코드-구조)
  - [3.3 빌드 에러 대응](#33-빌드-에러-대응)
- [Activity 4: DI 등록](#activity-4-di-등록)
  - [4.1 Registration 클래스 생성](#41-registration-클래스-생성)
  - [4.2 유형별 등록 패턴](#42-유형별-등록-패턴)
  - [4.3 DI Lifetime 선택 가이드](#43-di-lifetime-선택-가이드)
  - [4.4 Host Bootstrap 통합](#44-host-bootstrap-통합)
- [Activity 5: 단위 테스트](#activity-5-단위-테스트)
  - [5.1 테스트 원칙](#51-테스트-원칙)
  - [5.2 Repository 테스트](#52-repository-테스트)
  - [5.3 External API 테스트](#53-external-api-테스트)
  - [5.4 Messaging 테스트](#54-messaging-테스트)
- [End-to-End Walkthrough: Repository](#end-to-end-walkthrough-repository)
- [End-to-End Walkthrough: External API](#end-to-end-walkthrough-external-api)
- [End-to-End Walkthrough: Messaging](#end-to-end-walkthrough-messaging)
- [Troubleshooting](#troubleshooting)
- [Quick Reference 체크리스트](#quick-reference-체크리스트)
- [참고 문서](#참고-문서)

---

## 구현 라이프사이클 개요

Adapter 구현은 5단계 활동으로 구성됩니다.

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Adapter 구현 라이프사이클                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Activity 1          Activity 2          Activity 3                 │
│  ┌──────────┐        ┌──────────┐        ┌──────────┐              │
│  │ Port     │───────▶│ Adapter  │───────▶│ Pipeline │              │
│  │ 인터페이스│        │ 구현     │        │ 생성 확인│              │
│  │ 정의     │        │          │        │ (자동)   │              │
│  └──────────┘        └──────────┘        └──────────┘              │
│       │                   │                   │                     │
│       ▼                   ▼                   ▼                     │
│  Domain Layer /      Adapter Layer       obj/GeneratedFiles/       │
│  Application Layer                                                  │
│                                                                     │
│  Activity 4          Activity 5                                     │
│  ┌──────────┐        ┌──────────┐                                  │
│  │ DI 등록  │───────▶│ 단위     │                                  │
│  │          │        │ 테스트   │                                  │
│  └──────────┘        └──────────┘                                  │
│       │                   │                                         │
│       ▼                   ▼                                         │
│  Registration /      Tests.Unit/                                    │
│  Program.cs                                                         │
└─────────────────────────────────────────────────────────────────────┘
```

### 단계별 소속 레이어/프로젝트

| Activity | 작업 | 소속 레이어 | 프로젝트 예시 |
|----------|------|-------------|---------------|
| 1 | Port 인터페이스 정의 | Domain / Application | `LayeredArch.Domain`, `LayeredArch.Application` |
| 2 | Adapter 구현 | Adapter | `LayeredArch.Adapters.Persistence`, `LayeredArch.Adapters.Infrastructure` |
| 3 | Pipeline 생성 확인 | (자동 생성) | `obj/GeneratedFiles/` |
| 4 | DI 등록 | Adapter / Host | `{Project}.Adapters.{Layer}`, `LayeredArch` |
| 5 | 단위 테스트 | Test | `{Project}.Tests.Unit` |

---

## Activity 1: Port 인터페이스 정의

Port 인터페이스는 Application Layer가 외부 시스템과 통신하기 위한 **계약(Contract)**입니다.

### 1.1 위치 규칙

| 유형 | 위치 | 이유 |
|------|------|------|
| Repository | **Domain Layer** (`Domain/Repositories/`) | 도메인 모델(Entity, VO)에 직접 의존 |
| External API | **Application Layer** (`Application/Ports/`) | 외부 시스템 통신은 Application 관심사 |
| Messaging | **Application Layer** (`Application/Ports/`) 또는 Adapter 내부 | 메시징은 인프라 관심사, 프로젝트 구조에 따라 결정 |

> **참고**: Cqrs06Services 튜토리얼에서는 Messaging Port를 `Adapters/Messaging/` 내부에 배치합니다.
> 이는 Port와 Adapter가 동일 프로젝트에 있는 간소화된 구조입니다.

### 1.2 Port 정의 체크리스트

Port 인터페이스를 정의할 때 반드시 확인해야 할 항목입니다.

- [ ] `IAdapter` 인터페이스를 상속하는가?
- [ ] 모든 메서드의 반환 타입이 `FinT<IO, T>`인가?
- [ ] 매개변수와 반환 타입에 도메인 값 객체(VO)를 사용하는가?
- [ ] 비동기 작업이 필요한 메서드에 `CancellationToken` 매개변수가 있는가?
- [ ] 인터페이스 이름이 `I` 접두사 규칙을 따르는가?

### 1.3 유형별 Port 정의 패턴

#### Repository Port

도메인 Entity의 영속성을 담당합니다. **Domain Layer**에 위치합니다.

```csharp
// 파일: {Domain}/Repositories/IProductRepository.cs

using Functorium.Applications.Observabilities;  // IAdapter

public interface IProductRepository : IAdapter
{
    FinT<IO, Product> Create(Product product);
    FinT<IO, Product> GetById(ProductId id);
    FinT<IO, Option<Product>> GetByName(ProductName name);
    FinT<IO, Seq<Product>> GetAll();
    FinT<IO, Product> Update(Product product);
    FinT<IO, Unit> Delete(ProductId id);
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Domain/Repositories/IProductRepository.cs`

**핵심 포인트**:
- 매개변수는 도메인 값 객체 (`ProductId`, `ProductName`) 사용
- 조회 실패 가능성이 있으면 `Option<T>` 래핑
- 컬렉션 반환은 `Seq<T>` 사용
- 반환 값이 없으면 `Unit` 사용

#### External API Port

외부 시스템 API 호출을 추상화합니다. **Application Layer**에 위치합니다.

```csharp
// 파일: {Application}/Ports/IExternalPricingService.cs

using Functorium.Applications.Observabilities;  // IAdapter

public interface IExternalPricingService : IAdapter
{
    FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken);
    FinT<IO, Map<string, Money>> GetPricesAsync(Seq<string> productCodes, CancellationToken cancellationToken);
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Application/Ports/IExternalPricingService.cs`

**핵심 포인트**:
- 비동기 작업이므로 `CancellationToken` 매개변수 포함
- 메서드 이름에 `Async` 접미사 사용 (내부적으로 `IO.liftAsync` 사용 예정)
- 응답 DTO는 같은 파일 또는 별도 파일에 정의 가능

#### Messaging Port

메시지 브로커(RabbitMQ 등)를 통한 서비스 간 통신을 추상화합니다.

```csharp
// 파일: {Application}/Ports/IInventoryMessaging.cs
// 또는: {Adapters}/Messaging/IInventoryMessaging.cs

using Functorium.Applications.Observabilities;  // IAdapter

public interface IInventoryMessaging : IAdapter
{
    /// Request/Reply 패턴
    FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request);

    /// Fire-and-Forget 패턴
    FinT<IO, Unit> ReserveInventory(ReserveInventoryCommand command);
}
```

> **참조**: `Tutorials/Cqrs06Services/Src/OrderService/Adapters/Messaging/IInventoryMessaging.cs`

**핵심 포인트**:
- Request/Reply: 응답 타입을 반환 (`FinT<IO, TResponse>`)
- Fire-and-Forget: `FinT<IO, Unit>` 반환
- 메시지 타입(`CheckInventoryRequest` 등)은 공유 프로젝트에 정의

#### 유형별 비교 테이블

| 항목 | Repository | External API | Messaging |
|------|-----------|-------------|-----------|
| 위치 | Domain Layer | Application Layer | Application 또는 Adapter |
| `IAdapter` 상속 | 필수 | 필수 | 필수 |
| 반환 타입 | `FinT<IO, T>` | `FinT<IO, T>` | `FinT<IO, T>` |
| `CancellationToken` | 선택 | 권장 | 선택 |
| 값 객체 사용 | 필수 | 권장 | 메시지 DTO 사용 |
| 컬렉션 타입 | `Seq<T>` | `Seq<T>`, `Map<K,V>` | 단일 메시지 |

---

## Activity 2: Adapter 구현

Adapter는 Port 인터페이스의 **구현체**입니다. `[GeneratePipeline]` 어트리뷰트를 통해 Observability Pipeline이 자동 생성됩니다.

### 공통 구현 체크리스트

모든 Adapter 구현에 필수인 항목입니다.

- [ ] `[GeneratePipeline]` 어트리뷰트를 클래스에 적용했는가?
- [ ] Port 인터페이스를 구현하는가?
- [ ] `RequestCategory` 프로퍼티를 정의했는가?
- [ ] 모든 인터페이스 메서드에 `virtual` 키워드를 추가했는가?
- [ ] `IO.lift()` 또는 `IO.liftAsync()` 로 비즈니스 로직을 래핑했는가?

### 2.1 Repository Adapter

Repository Adapter는 데이터 저장소에 대한 CRUD 작업을 구현합니다.

```csharp
// 파일: {Adapters.Persistence}/Repositories/InMemoryProductRepository.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

[GeneratePipeline]                                    // 1. Pipeline 자동 생성
public class InMemoryProductRepository : IProductRepository  // 2. Port 인터페이스 구현
{
    private static readonly ConcurrentDictionary<ProductId, Product> _products = new();

    public string RequestCategory => "Repository";     // 3. 요청 카테고리

    public InMemoryProductRepository()                 // 4. 생성자
    {
    }

    public virtual FinT<IO, Product> Create(Product product)  // 5. virtual 필수
    {
        return IO.lift(() =>                           // 6. IO.lift (동기)
        {
            _products[product.Id] = product;
            return Fin.Succ(product);                  // 7. 성공 반환
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (_products.TryGetValue(id, out Product? product))
            {
                return Fin.Succ(product);
            }

            return AdapterError.For<InMemoryProductRepository>(  // 8. 실패 반환
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Unit> Delete(ProductId id)
    {
        return IO.lift(() =>
        {
            if (!_products.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);                     // 9. Unit 반환
        });
    }

    // ... 나머지 메서드도 동일 패턴
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Repositories/InMemoryProductRepository.cs`

**Repository Adapter 핵심 패턴**:

| 패턴 | 코드 | 설명 |
|------|------|------|
| IO 래핑 | `IO.lift(() => { ... })` | 동기 작업은 `IO.lift` 사용 |
| 성공 | `Fin.Succ(value)` | 성공 값 래핑 |
| 도메인 실패 | `AdapterError.For<T>(errorType, context, message)` | 비즈니스 실패 (not found 등) |
| Unit 반환 | `Fin.Succ(unit)` | 반환 값 없는 작업 (`using static LanguageExt.Prelude`) |
| Optional | `Fin.Succ(Optional(value))` | `Option<T>` 래핑 |
| 컬렉션 | `Fin.Succ(toSeq(values))` | `Seq<T>` 래핑 |

### 2.2 External API Adapter

External API Adapter는 HTTP 클라이언트를 통한 외부 시스템 호출을 구현합니다.

```csharp
// 파일: {Adapters.Infrastructure}/ExternalApis/ExternalPricingApiService.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorType;

[GeneratePipeline]
public class ExternalPricingApiService : IExternalPricingService
{
    private readonly HttpClient _httpClient;              // 1. HttpClient 주입

    public string RequestCategory => "ExternalApi";       // 2. 요청 카테고리

    public ExternalPricingApiService(HttpClient httpClient)  // 3. 생성자 주입
    {
        _httpClient = httpClient;
    }

    public virtual FinT<IO, Money> GetPriceAsync(
        string productCode, CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>                   // 4. IO.liftAsync (비동기)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/pricing/{productCode}",
                    cancellationToken);

                // 5. HTTP 오류 처리
                if (!response.IsSuccessStatusCode)
                {
                    return HandleHttpError<Money>(response, productCode);
                }

                // 6. 응답 역직렬화
                var priceResponse = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse>(
                        cancellationToken: cancellationToken);

                // 7. null 응답 처리
                if (priceResponse is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        productCode,
                        $"외부 API 응답이 null입니다. ProductCode: {productCode}");
                }

                return Money.Create(priceResponse.Price);
            }
            catch (HttpRequestException ex)               // 8. 연결 실패
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"),
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return AdapterError.For<ExternalPricingApiService>(  // 9. 사용자 취소
                    new Custom("OperationCancelled"),
                    productCode,
                    "요청이 취소되었습니다");
            }
            catch (TaskCanceledException ex)              // 10. 타임아웃
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new AdapterErrorType.Timeout(TimeSpan.FromSeconds(30)),
                    ex);
            }
            catch (Exception ex)                          // 11. 기타 예외
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new Custom("UnexpectedException"),
                    ex);
            }
        });
    }

    // HTTP 상태 코드별 에러 매핑
    private static Fin<T> HandleHttpError<T>(
        HttpResponseMessage response, string context) =>
        response.StatusCode switch
        {
            HttpStatusCode.NotFound => AdapterError.For<ExternalPricingApiService>(
                new NotFound(), context, "리소스를 찾을 수 없습니다"),

            HttpStatusCode.Unauthorized => AdapterError.For<ExternalPricingApiService>(
                new Unauthorized(), context, "인증에 실패했습니다"),

            HttpStatusCode.Forbidden => AdapterError.For<ExternalPricingApiService>(
                new Forbidden(), context, "접근이 금지되었습니다"),

            HttpStatusCode.TooManyRequests => AdapterError.For<ExternalPricingApiService>(
                new Custom("RateLimited"), context, "요청 제한에 도달했습니다"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context, "서비스를 사용할 수 없습니다"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new Custom("HttpError"), response.StatusCode,
                $"API 호출 실패. Status: {response.StatusCode}")
        };
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs`

**External API Adapter 핵심 패턴**:

| 패턴 | 코드 | 설명 |
|------|------|------|
| IO 래핑 | `IO.liftAsync(async () => { ... })` | 비동기 작업은 `IO.liftAsync` 사용 |
| 예외 래핑 (연결 실패) | `AdapterError.FromException<T>(errorType, ex)` | 예외를 `Fin.Fail`로 변환 |
| 도메인 실패 | `AdapterError.For<T>(errorType, context, message)` | 비즈니스 실패 변환 |
| HTTP 에러 매핑 | `HandleHttpError<T>(response, context)` | 상태 코드별 `AdapterErrorType` 매핑 |
| 취소 vs 타임아웃 | `when (ex.CancellationToken == cancellationToken)` | 사용자 취소와 타임아웃 구분 |

#### HTTP 상태 코드 → AdapterErrorType 매핑 참조

| HTTP 상태 코드 | AdapterErrorType | 설명 |
|---------------|------------------|------|
| 404 | `new NotFound()` | 리소스 없음 |
| 401 | `new Unauthorized()` | 인증 실패 |
| 403 | `new Forbidden()` | 접근 거부 |
| 429 | `new Custom("RateLimited")` | 요청 제한 초과 |
| 503 | `new ExternalServiceUnavailable(name)` | 서비스 불가 |
| 기타 | `new Custom("HttpError")` | 일반 HTTP 에러 |

#### 예외 → AdapterErrorType 매핑 참조

| 예외 타입 | AdapterErrorType | 설명 |
|----------|------------------|------|
| `HttpRequestException` | `new ConnectionFailed(name)` | 연결 실패 |
| `TaskCanceledException` (사용자) | `new Custom("OperationCancelled")` | 요청 취소 |
| `TaskCanceledException` (타임아웃) | `new Timeout(timespan)` | 응답 시간 초과 |
| `Exception` | `new Custom("UnexpectedException")` | 예상 외 예외 |

### 2.3 Messaging Adapter

Messaging Adapter는 메시지 브로커를 통한 서비스 간 통신을 구현합니다.

```csharp
// 파일: {Adapters}/Messaging/RabbitMqInventoryMessaging.cs

using Functorium.Adapters.SourceGenerators;
using static LanguageExt.Prelude;
using Wolverine;

[GeneratePipeline]
public class RabbitMqInventoryMessaging : IInventoryMessaging
{
    private readonly IMessageBus _messageBus;              // 1. MessageBus 주입

    public string RequestCategory => "Messaging";          // 2. 요청 카테고리

    public RabbitMqInventoryMessaging(IMessageBus messageBus)  // 3. 생성자 주입
    {
        _messageBus = messageBus;
    }

    // Request/Reply 패턴
    public virtual FinT<IO, CheckInventoryResponse> CheckInventory(
        CheckInventoryRequest request)
    {
        return IO.liftAsync(async () =>                    // 4. IO.liftAsync
        {
            try
            {
                var response = await _messageBus
                    .InvokeAsync<CheckInventoryResponse>(request);  // 5. InvokeAsync
                return Fin.Succ(response);
            }
            catch (Exception ex)
            {
                return Fin.Fail<CheckInventoryResponse>(
                    Error.New(ex.Message));                 // 6. 에러 래핑
            }
        });
    }

    // Fire-and-Forget 패턴
    public virtual FinT<IO, Unit> ReserveInventory(
        ReserveInventoryCommand command)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _messageBus.SendAsync(command);      // 7. SendAsync
                return Fin.Succ(unit);
            }
            catch (Exception ex)
            {
                return Fin.Fail<Unit>(Error.New(ex.Message));
            }
        });
    }
}
```

> **참조**: `Tutorials/Cqrs06Services/Src/OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs`

**Messaging Adapter 핵심 패턴**:

| 패턴 | API | 설명 |
|------|-----|------|
| Request/Reply | `_messageBus.InvokeAsync<TResponse>(request)` | 응답을 기다리는 동기적 메시징 |
| Fire-and-Forget | `_messageBus.SendAsync(command)` | 응답 없이 메시지 전송 |
| 에러 래핑 | `Fin.Fail<T>(Error.New(ex.Message))` | 메시징 예외를 `Fin.Fail`로 변환 |

### 2.4 IO.lift vs IO.liftAsync 판단

| 기준 | `IO.lift(() => { ... })` | `IO.liftAsync(async () => { ... })` |
|------|--------------------------|--------------------------------------|
| 작업 유형 | 동기 (sync) | 비동기 (async/await) |
| 대표 사례 | In-Memory 저장소, 캐시 조회 | HTTP 호출, 메시지 전송, DB 비동기 쿼리 |
| 반환 | `Fin<T>` | `Fin<T>` |
| 사용 유형 | Repository (동기) | External API, Messaging |

**판단 기준**: 내부에서 `await`를 사용해야 하는가?
- **예** → `IO.liftAsync`
- **아니오** → `IO.lift`

> **참고**: EF Core 등 비동기 DB 접근 시에는 Repository에서도 `IO.liftAsync`를 사용합니다.

---

## Activity 3: Pipeline 생성 확인

`[GeneratePipeline]` 어트리뷰트가 적용된 Adapter를 빌드하면, Source Generator가 자동으로 Pipeline 클래스를 생성합니다.

### 3.1 생성 파일 확인

빌드 후 다음 경로에서 생성된 파일을 확인합니다.

```
{Project}/obj/GeneratedFiles/
  └── Functorium.Adapters.SourceGenerator/
      └── Functorium.Adapters.SourceGenerators.Generators.AdapterPipelineGenerator.AdapterPipelineGenerator/
          └── {Namespace}.{ClassName}Pipeline.g.cs
```

**예시**:
```
LayeredArch.Adapters.Persistence/obj/GeneratedFiles/.../
  └── Repositories.InMemoryProductRepositoryPipeline.g.cs

LayeredArch.Adapters.Infrastructure/obj/GeneratedFiles/.../
  └── ExternalApis.ExternalPricingApiServicePipeline.g.cs

OrderService/obj/GeneratedFiles/.../
  └── Messaging.RabbitMqInventoryMessagingPipeline.g.cs
```

### 3.2 생성 코드 구조

생성된 Pipeline 클래스는 다음과 같은 구조를 가집니다.

```csharp
// 자동 생성 코드 (예시 구조)
public class InMemoryProductRepositoryPipeline : InMemoryProductRepository
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<InMemoryProductRepositoryPipeline> _logger;
    private readonly Histogram<double> _durationHistogram;
    // ... 기타 Observability 필드

    public InMemoryProductRepositoryPipeline(
        ActivitySource activitySource,
        ILogger<InMemoryProductRepositoryPipeline> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions
        /* + 원본 생성자의 매개변수들 */)
        : base(/* 원본 생성자 매개변수 */)
    {
        // Observability 초기화
    }

    public override FinT<IO, Product> Create(Product product)
    {
        // Activity 시작 → 원본 메서드 호출 → 로깅/메트릭 기록
        return /* 래핑된 호출 */;
    }
}
```

**핵심 구조**:
- 원본 Adapter 클래스를 **상속** (`InMemoryProductRepositoryPipeline : InMemoryProductRepository`)
- `virtual` 메서드를 **override**하여 Observability 로직 추가
- 생성자에 `ActivitySource`, `ILogger`, `IMeterFactory` 등 Observability 의존성 주입
- 원본 생성자 매개변수도 함께 전달

### 3.3 빌드 에러 대응

| 에러 | 증상 | 원인 | 해결 |
|------|------|------|------|
| CS0506 | `cannot override because it is not virtual` | 메서드에 `virtual` 키워드 누락 | 모든 인터페이스 메서드에 `virtual` 추가 |
| Pipeline 클래스 미생성 | `obj/GeneratedFiles/`에 파일 없음 | `[GeneratePipeline]` 어트리뷰트 누락 | 클래스에 어트리뷰트 추가 |
| 생성자 매개변수 충돌 | Source Generator 에러 | 생성자 매개변수 타입이 Observability 타입과 충돌 | 생성자 매개변수에 고유 타입 사용 |
| 네임스페이스 누락 | `using` 에러 | Functorium 패키지 참조 누락 | `Functorium.Adapters.SourceGenerators` NuGet 패키지 추가 |

---

## Activity 4: DI 등록

생성된 Pipeline 클래스를 DI 컨테이너에 등록합니다.

### 4.1 Registration 클래스 생성

**위치 규칙**: `{Project}.Adapters.{Layer}/Abstractions/Registrations/`

**네이밍 규칙**: `Adapter{Layer}Registration`

```csharp
// 파일: {Adapters.Persistence}/Abstractions/Registrations/AdapterPersistenceRegistration.cs

using Functorium.Abstractions.Registrations;

public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(
        this IServiceCollection services)
    {
        // Pipeline 등록
        services.RegisterScopedAdapterPipeline<
            IProductRepository,
            InMemoryProductRepositoryPipeline>();

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(
        this IApplicationBuilder app)
    {
        return app;
    }
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Abstractions/Registrations/AdapterPersistenceRegistration.cs`

### 4.2 유형별 등록 패턴

#### Repository 등록

```csharp
// 단일 인터페이스 등록
services.RegisterScopedAdapterPipeline<
    IProductRepository,                      // Port 인터페이스
    InMemoryProductRepositoryPipeline>();     // 생성된 Pipeline
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Abstractions/Registrations/AdapterPersistenceRegistration.cs`

#### External API 등록

External API Adapter는 HttpClient와 Pipeline 두 가지를 등록해야 합니다.

```csharp
// 1단계: HttpClient 등록
services.AddHttpClient<ExternalPricingApiServicePipeline>(client =>
{
    client.BaseAddress = new Uri(configuration["ExternalApi:BaseUrl"]
        ?? "https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 2단계: Pipeline 등록
services.RegisterScopedAdapterPipeline<
    IExternalPricingService,
    ExternalPricingApiServicePipeline>();
```

> **참고**: `HttpClient`는 Pipeline 클래스 타입으로 등록합니다. Pipeline이 원본 Adapter를 상속하므로 생성자의 `HttpClient` 매개변수를 그대로 받습니다.

#### Messaging 등록

```csharp
// Pipeline 등록 (MessageBus는 별도 등록 필요)
services.RegisterScopedAdapterPipeline<
    IInventoryMessaging,
    RabbitMqInventoryMessagingPipeline>();
```

> **참조**: `Tutorials/Cqrs06Services/Src/OrderService/Program.cs` (57행)

#### 등록 API 요약

| 등록 API | Lifetime | 용도 |
|----------|----------|------|
| `RegisterScopedAdapterPipeline<TService, TImpl>()` | Scoped | HTTP 요청당 1개 (기본 권장) |
| `RegisterTransientAdapterPipeline<TService, TImpl>()` | Transient | 매 요청마다 새 인스턴스 |
| `RegisterSingletonAdapterPipeline<TService, TImpl>()` | Singleton | 애플리케이션 전체 1개 |
| `RegisterScopedAdapterPipelineFor<T1, T2, TImpl>()` | Scoped | 2개 인터페이스 → 1개 구현체 |
| `RegisterScopedAdapterPipelineFor<T1, T2, T3, TImpl>()` | Scoped | 3개 인터페이스 → 1개 구현체 |

> **참조**: `Src/Functorium/Abstractions/Registrations/AdapterPipelineRegistration.cs`

### 4.3 DI Lifetime 선택 가이드

| Lifetime | 사용 시점 | 주의사항 |
|----------|----------|---------|
| **Scoped** (기본) | Repository, External API, Messaging | HTTP 요청 내 동일 인스턴스 공유 |
| **Transient** | 상태 없는 가벼운 Adapter | 매번 새 인스턴스 생성 (메모리 주의) |
| **Singleton** | 스레드 안전한 읽기 전용 Adapter | 상태 변경 불가, 스레드 안전성 보장 필요 |

> **권장**: 특별한 이유가 없으면 **Scoped**를 사용하세요.

### 4.4 Host Bootstrap 통합

`Program.cs`에서 레이어별 Registration을 호출합니다.

```csharp
// 파일: {Host}/Program.cs

var builder = WebApplication.CreateBuilder(args);

// 레이어별 서비스 등록
builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence()
    .RegisterAdapterInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseAdapterInfrastructure()
   .UseAdapterPersistence()
   .UseAdapterPresentation();

app.Run();
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch/Program.cs`

**핵심 포인트**:
- `RegisterAdapter{Layer}()`: `IServiceCollection` 확장 메서드로 서비스 등록
- `UseAdapter{Layer}()`: `IApplicationBuilder` 확장 메서드로 미들웨어 설정
- 등록 순서는 의존성 방향에 따라 결정 (Presentation → Persistence → Infrastructure)

---

## Activity 5: 단위 테스트

Adapter의 단위 테스트는 **원본 클래스를 직접 테스트**합니다 (Pipeline이 아님).

### 5.1 테스트 원칙

| 원칙 | 설명 |
|------|------|
| 테스트 대상 | 원본 Adapter 클래스 (Pipeline 아님) |
| 패턴 | AAA (Arrange-Act-Assert) |
| 네이밍 | `T1_T2_T3` (메서드명_시나리오_기대결과) |
| 실행 | `.Run().RunAsync()` 또는 `Task.Run(() => ioResult.Run())` |
| 단언 라이브러리 | Shouldly |
| Mock 라이브러리 | NSubstitute |

> **참고**: 테스트 규칙 상세는 [unit-testing-guide.md](./unit-testing-guide.md)를 참조하세요.

### IO 실행 패턴

`FinT<IO, T>` 반환값을 테스트에서 실행하는 패턴입니다.

```csharp
// Act
var ioFin = adapter.MethodUnderTest(args);   // FinT<IO, T> 반환
var ioResult = ioFin.Run();                  // IO<Fin<T>> 변환
var result = await Task.Run(() => ioResult.Run());  // Fin<T> 실행

// Assert
result.IsSucc.ShouldBeTrue();
```

### 5.2 Repository 테스트

Repository Adapter는 외부 의존성이 없으므로 (In-Memory 구현의 경우) 직접 인스턴스를 생성하여 테스트합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/LayerTests/Adapters/InMemoryProductRepositoryTests.cs

public sealed class InMemoryProductRepositoryTests
{
    [Fact]
    public async Task Create_ReturnsProduct_WhenProductIsValid()
    {
        // Arrange
        var repository = new InMemoryProductRepository();
        var product = Product.Create(
            ProductId.Create(Guid.NewGuid()),
            ProductName.Create("테스트 상품"),
            Money.Create(10000m));

        // Act
        var ioFin = repository.Create(product);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: created =>
            {
                created.Id.ShouldBe(product.Id);
                created.Name.ShouldBe(product.Name);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetById_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var repository = new InMemoryProductRepository();
        var nonExistentId = ProductId.Create(Guid.NewGuid());

        // Act
        var ioFin = repository.GetById(nonExistentId);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}
```

### 5.3 External API 테스트

External API Adapter는 `HttpClient`를 Mock하여 테스트합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/LayerTests/Adapters/ExternalPricingApiServiceTests.cs

public sealed class ExternalPricingApiServiceTests
{
    [Fact]
    public async Task GetPriceAsync_ReturnsMoney_WhenApiReturnsSuccess()
    {
        // Arrange
        var priceResponse = new ExternalPriceResponse(
            "PROD-001", 29900m, "KRW", DateTime.UtcNow.AddHours(1));

        var handler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(priceResponse));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        var service = new ExternalPricingApiService(httpClient);

        // Act
        var ioFin = service.GetPriceAsync("PROD-001", CancellationToken.None);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPriceAsync_ReturnsFail_WhenApiReturns404()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        var service = new ExternalPricingApiService(httpClient);

        // Act
        var ioFin = service.GetPriceAsync("INVALID", CancellationToken.None);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    // HttpClient Mock을 위한 도우미 클래스
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public MockHttpMessageHandler(
            HttpStatusCode statusCode, string? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content is not null)
            {
                response.Content = new StringContent(
                    _content, System.Text.Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }
    }
}
```

### 5.4 Messaging 테스트

Messaging Adapter는 `IMessageBus`를 NSubstitute로 Mock하여 테스트합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs

public sealed class RabbitMqInventoryMessagingTests
{
    [Fact]
    public async Task CheckInventory_SendsRequest_WhenRequestIsValid()
    {
        // Arrange
        var request = new CheckInventoryRequest(Guid.NewGuid(), Quantity: 5);
        var expectedResponse = new CheckInventoryResponse(
            ProductId: request.ProductId,
            IsAvailable: true,
            AvailableQuantity: 10);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.InvokeAsync<CheckInventoryResponse>(
                request, Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(expectedResponse);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.CheckInventory(request);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ProductId.ShouldBe(request.ProductId);
                response.IsAvailable.ShouldBeTrue();
                response.AvailableQuantity.ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task CheckInventory_ReturnsFailure_WhenMessageBusThrowsException()
    {
        // Arrange
        var request = new CheckInventoryRequest(Guid.NewGuid(), Quantity: 5);
        var messageBus = Substitute.For<IMessageBus>();
        messageBus.InvokeAsync<CheckInventoryResponse>(
                request, Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(Task.FromException<CheckInventoryResponse>(
                new Exception("Connection failed")));

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.CheckInventory(request);
        var ioResult = ioFin.Run();
        Fin<CheckInventoryResponse> result;
        try
        {
            result = await Task.Run(() => ioResult.Run());
        }
        catch (Exception ex)
        {
            result = Fin.Fail<CheckInventoryResponse>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public async Task ReserveInventory_SendsCommand_WhenCommandIsValid()
    {
        // Arrange
        var command = new ReserveInventoryCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.SendAsync(command)
            .Returns(ValueTask.CompletedTask);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.ReserveInventory(command);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        await messageBus.Received(1).SendAsync(command);
    }
}
```

> **참조**: `Tutorials/Cqrs06Services/Tests/OrderService.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs`

---

## End-to-End Walkthrough: Repository

`01-SingleHost`의 `IProductRepository` 구현 전 과정을 추적합니다.

### Step 1: Port 인터페이스 정의

```
📁 LayeredArch.Domain/Repositories/IProductRepository.cs
```

```csharp
public interface IProductRepository : IAdapter
{
    FinT<IO, Product> Create(Product product);
    FinT<IO, Product> GetById(ProductId id);
    FinT<IO, Option<Product>> GetByName(ProductName name);
    FinT<IO, Seq<Product>> GetAll();
    FinT<IO, Product> Update(Product product);
    FinT<IO, Unit> Delete(ProductId id);
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
}
```

### Step 2: Adapter 구현

```
📁 LayeredArch.Adapters.Persistence/Repositories/InMemoryProductRepository.cs
```

- `[GeneratePipeline]` 어트리뷰트 적용
- `RequestCategory => "Repository"` 설정
- 모든 메서드에 `virtual` 키워드 추가
- `IO.lift(() => { ... })` 로 동기 작업 래핑
- `AdapterError.For<T>()` 로 실패 처리

### Step 3: Pipeline 생성 확인

```
📁 LayeredArch.Adapters.Persistence/obj/GeneratedFiles/.../
   Repositories.InMemoryProductRepositoryPipeline.g.cs
```

빌드 후 `InMemoryProductRepositoryPipeline` 클래스가 자동 생성됩니다.

### Step 4: DI 등록

```
📁 LayeredArch.Adapters.Persistence/Abstractions/Registrations/AdapterPersistenceRegistration.cs
```

```csharp
services.RegisterScopedAdapterPipeline<
    IProductRepository,
    InMemoryProductRepositoryPipeline>();
```

```
📁 LayeredArch/Program.cs
```

```csharp
builder.Services
    .RegisterAdapterPersistence()    // ← 여기서 호출
    // ...
```

### Step 5: 단위 테스트

원본 `InMemoryProductRepository`를 직접 인스턴스화하여 테스트합니다.
[Activity 5: Repository 테스트](#52-repository-테스트) 섹션의 템플릿을 참조하세요.

---

## End-to-End Walkthrough: External API

`01-SingleHost`의 `IExternalPricingService` 구현 전 과정을 추적합니다.

### Step 1: Port 인터페이스 정의

```
📁 LayeredArch.Application/Ports/IExternalPricingService.cs
```

```csharp
public interface IExternalPricingService : IAdapter
{
    FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken);
    FinT<IO, Map<string, Money>> GetPricesAsync(Seq<string> productCodes, CancellationToken cancellationToken);
}
```

- Application Layer에 위치 (외부 시스템 통신)
- `CancellationToken` 매개변수 포함
- 응답 DTO (`ExternalPriceResponse`)를 같은 파일에 정의

### Step 2: Adapter 구현

```
📁 LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs
```

- `[GeneratePipeline]` 어트리뷰트 적용
- `RequestCategory => "ExternalApi"` 설정
- `HttpClient` 생성자 주입
- `IO.liftAsync(async () => { ... })` 로 비동기 작업 래핑
- `HandleHttpError<T>()` 패턴으로 HTTP 상태 코드별 에러 매핑
- 종합적 try/catch 예외 처리 (`HttpRequestException`, `TaskCanceledException`, `Exception`)

### Step 3: Pipeline 생성 확인

```
📁 LayeredArch.Adapters.Infrastructure/obj/GeneratedFiles/.../
   ExternalApis.ExternalPricingApiServicePipeline.g.cs
```

### Step 4: DI 등록

```
📁 LayeredArch.Adapters.Infrastructure/Abstractions/Registrations/AdapterInfrastructureRegistration.cs
```

```csharp
// HttpClient 등록
services.AddHttpClient<ExternalPricingApiServicePipeline>(client =>
{
    client.BaseAddress = new Uri(configuration["ExternalApi:BaseUrl"]!);
});

// Pipeline 등록
services.RegisterScopedAdapterPipeline<
    IExternalPricingService,
    ExternalPricingApiServicePipeline>();
```

### Step 5: 단위 테스트

`MockHttpMessageHandler`를 사용하여 `HttpClient`를 Mock하고, 원본 `ExternalPricingApiService`를 직접 테스트합니다.
[Activity 5: External API 테스트](#53-external-api-테스트) 섹션의 템플릿을 참조하세요.

---

## End-to-End Walkthrough: Messaging

`Cqrs06Services`의 `IInventoryMessaging` 구현 전 과정을 추적합니다.

### Step 1: Port 인터페이스 정의

```
📁 OrderService/Adapters/Messaging/IInventoryMessaging.cs
```

```csharp
public interface IInventoryMessaging : IAdapter
{
    FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request);
    FinT<IO, Unit> ReserveInventory(ReserveInventoryCommand command);
}
```

- Request/Reply (`CheckInventory`): 응답을 기다림
- Fire-and-Forget (`ReserveInventory`): 메시지만 전송

### Step 2: Adapter 구현

```
📁 OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs
```

- `[GeneratePipeline]` 어트리뷰트 적용
- `RequestCategory => "Messaging"` 설정
- `IMessageBus` 생성자 주입 (Wolverine)
- `IO.liftAsync(async () => { ... })` 사용
- `InvokeAsync<T>()`: Request/Reply
- `SendAsync()`: Fire-and-Forget

### Step 3: Pipeline 생성 확인

```
📁 OrderService/obj/GeneratedFiles/.../
   Messaging.RabbitMqInventoryMessagingPipeline.g.cs
```

### Step 4: DI 등록

```
📁 OrderService/Program.cs (57행)
```

```csharp
services.RegisterScopedAdapterPipeline<
    IInventoryMessaging,
    OrderService.Adapters.Messaging.RabbitMqInventoryMessagingPipeline>();
```

MessageBus는 Wolverine 호스트 설정에서 별도 등록됩니다.

### Step 5: 단위 테스트

```
📁 OrderService.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs
```

NSubstitute로 `IMessageBus`를 Mock하여 원본 `RabbitMqInventoryMessaging`을 테스트합니다.
[Activity 5: Messaging 테스트](#54-messaging-테스트) 섹션의 코드를 참조하세요.

---

## Troubleshooting

| 문제 | 증상 | 해결 |
|------|------|------|
| `virtual` 누락 | `CS0506: cannot override because it is not virtual` | 모든 인터페이스 메서드에 `virtual` 키워드 추가 |
| `[GeneratePipeline]` 누락 | Pipeline 클래스가 `obj/GeneratedFiles/`에 생성되지 않음 | 클래스에 `[GeneratePipeline]` 어트리뷰트 추가 |
| `IAdapter` 미상속 | `RegisterScopedAdapterPipeline` 컴파일 에러 | Port 인터페이스에 `: IAdapter` 상속 추가 |
| 생성자 타입 충돌 | Source Generator 에러 또는 잘못된 Pipeline 생성 | 생성자 매개변수에 고유 타입 사용 (동일 타입 중복 금지) |
| Pipeline DI 미등록 | `InvalidOperationException: No service for type 'IXxx'` | `RegisterScopedAdapterPipeline<IXxx, XxxPipeline>()` 호출 확인 |
| Pipeline 타입 미발견 | `The type or namespace name 'XxxPipeline' could not be found` | `dotnet build` 실행 후 재시도 (Source Generator 트리거) |
| `RequestCategory` 누락 | 컴파일 에러 | `public string RequestCategory => "카테고리명";` 추가 |
| `IO.lift` 내 `await` 사용 | 컴파일 에러 (async 불가) | `IO.liftAsync(async () => ...)` 로 변경 |
| HttpClient 미등록 | `No service for type 'HttpClient'` | `services.AddHttpClient<XxxPipeline>(...)` 등록 |

---

## Quick Reference 체크리스트

### Port 인터페이스

- [ ] `IAdapter` 상속
- [ ] 반환 타입: `FinT<IO, T>`
- [ ] 도메인 VO 사용 (Repository)
- [ ] `CancellationToken` (External API)
- [ ] 위치: Repository → Domain, External API → Application

### Adapter 구현

- [ ] `[GeneratePipeline]` 어트리뷰트
- [ ] Port 인터페이스 구현
- [ ] `RequestCategory` 프로퍼티
- [ ] 모든 메서드에 `virtual`
- [ ] `IO.lift` (동기) 또는 `IO.liftAsync` (비동기)
- [ ] 성공: `Fin.Succ(value)`
- [ ] 실패: `AdapterError.For<T>(errorType, context, message)`
- [ ] 예외: `AdapterError.FromException<T>(errorType, ex)`

### DI 등록

- [ ] Registration 클래스 생성 (`Adapter{Layer}Registration`)
- [ ] `RegisterScopedAdapterPipeline<IPort, AdapterPipeline>()`
- [ ] HttpClient 등록 (External API)
- [ ] `Program.cs`에서 Registration 호출

### 단위 테스트

- [ ] 원본 Adapter 클래스 테스트 (Pipeline 아님)
- [ ] AAA 패턴
- [ ] `T1_T2_T3` 네이밍
- [ ] `.Run()` → `Task.Run(() => ioResult.Run())` 실행
- [ ] 성공/실패 케이스 모두 테스트

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [adapter-guide.md](./adapter-guide.md) | Adapter 설계 개념, IAdapter, GeneratePipeline 상세 |
| [domain-modeling-overview.md](./domain-modeling-overview.md) | 도메인 모델링 개요 |
| [valueobject-guide.md](./valueobject-guide.md) | 값 객체 구현 |
| [entity-guide.md](./entity-guide.md) | Entity/Aggregate Root 구현 |
| [error-guide.md](./error-guide.md) | 레이어별 에러 시스템 |
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 규칙 |
| [error-testing-guide.md](./error-testing-guide.md) | 에러 테스트 패턴 |
| [observability-spec.md](./observability-spec.md) | Observability 사양 |
