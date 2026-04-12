---
title: "Domain Services"
---

여러 Aggregate에 걸친 비즈니스 규칙은 어디에 두어야 할까요? Entity에 넣으면 경계를 넘고, Usecase에 넣으면 도메인 로직이 유출됩니다. 도메인 서비스는 이 문제를 해결합니다.

## Introduction

"주문 금액이 고객의 신용 한도를 초과하는지 검증하는 로직은 Order에 넣어야 하나, Customer에 넣어야 하나?"
"여러 Aggregate를 참조하는 비즈니스 규칙이 Usecase에 있으면 도메인 로직이 유출되는 것 아닌가?"
"Domain Service와 Application Service(Usecase)의 경계는 어디인가?"
"Domain Service가 Repository를 사용해도 되는가?"

이러한 질문은 비즈니스 로직이 단일 Aggregate의 경계를 넘어설 때 반복적으로 발생합니다. 도메인 서비스는 여러 Aggregate를 참조하는 도메인 로직을 Domain Layer에 유지하는 빌딩블록입니다.

### What You Will Learn

1. **도메인 서비스의 배치 판단 기준** — Entity 메서드, Usecase, Domain Service 중 어디에 로직을 둘지 결정하는 의사결정 트리
2. **두 가지 구현 패턴** — 순수 패턴(기본)과 Repository 패턴(Evans Ch.9)의 차이와 선택 기준
3. **Usecase에서의 통합 방법** — 패턴별 생성 방식과 LINQ 체인 사용법

### Prerequisites

- [Aggregate 설계 원칙](./06a-aggregate-design) — Aggregate 경계와 트랜잭션 원칙
- [에러 시스템: 기초와 네이밍](./08a-error-system) — `Fin<T>` 반환 패턴

> Evans는 Domain Service에 **Stateless**(호출 간 가변 상태 없음)를 요구하지만, **Pure**(I/O 없음)를 요구하지 않습니다.
> Functorium은 기본적으로 더 엄격한 순수 함수 패턴을 권장하며, 교차 데이터 규모에 따라 Repository 사용 패턴도 제시합니다.

## Summary

### 순수 패턴 (기본) — 소규모 교차 데이터

```csharp
// Domain Service 정의 — 상태 없음, I/O 없음
public sealed class OrderCreditCheckService : IDomainService
{
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount) { ... }
}

// Usecase에서 직접 생성 (DI 불필요)
private readonly OrderCreditCheckService _creditCheckService = new();

// FinT<IO, T> LINQ 체인에서 사용 (Fin<T> 자동 리프팅)
from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)
```

### Repository 패턴 (Evans Ch.9) — 대규모 교차 데이터

```csharp
// Domain Service 정의 — Repository 인터페이스 의존
public sealed class ContactEmailCheckService : IDomainService
{
    private readonly IContactRepository _repository;
    public ContactEmailCheckService(IContactRepository repository) => _repository = repository;

    public FinT<IO, Unit> ValidateEmailUnique(
        EmailAddress email, Option<ContactId> excludeId = default) { ... }
}

// Usecase에서 DI 주입
public sealed class Usecase(
    IContactRepository repository,
    ContactEmailCheckService emailCheckService) { ... }

// FinT<IO, T> LINQ 체인에서 직접 사용 (이미 FinT<IO, T>)
from _ in _emailCheckService.ValidateEmailUnique(email, excludeId)
```

### 패턴 선택 기준

| 판단 질문 | 순수 패턴 | Repository 패턴 |
|----------|----------|----------------|
| Usecase가 필요한 데이터를 로드할 수 있는 규모인가? | YES | NO (전체 테이블 스캔 필요) |
| 교차 데이터가 1~수건인가? | YES | NO (대량) |
| Service가 쿼리 규칙을 소유해야 하는가? | NO | YES (Specification 생성) |

### Key Procedures

1. **배치 판단**: 로직이 여러 Aggregate에 걸치는지, 교차 데이터 규모가 어느 정도인지 확인
2. **패턴 선택**: 순수 패턴(기본) 또는 Repository 패턴(Evans Ch.9) 결정
3. **클래스 정의**: `sealed class`, `IDomainService` 마커 구현
4. **메서드 작성**: 순수 패턴은 `Fin<T>`, Repository 패턴은 `FinT<IO, T>` 반환
5. **에러 정의**: `DomainError.For<{ServiceName}>()` 패턴으로 에러 코드 생성
6. **Usecase 통합**: 순수 패턴은 `new()` 직접 생성, Repository 패턴은 DI 주입

### Key Concepts

| Concept | Description |
|------|------|
| `IDomainService` | 빈 마커 인터페이스, 아키텍처 테스트 검증용 |
| 순수 패턴 (기본) | 외부 I/O 없음, 상태 없음, `Fin<T>` 반환, DI 불필요 |
| Repository 패턴 (Evans Ch.9) | Repository 인터페이스 의존, `FinT<IO, T>` 반환, DI 필요 |
| 자동 리프팅 | `FinT<IO, T>` LINQ 체인에서 `Fin<T>` 자동 리프팅 (순수 패턴만) |

---

## Why Domain Services

도메인 서비스는 DDD(Domain-Driven Design)에서 **여러 Aggregate에 걸친 도메인 로직을** 배치하는 빌딩블록입니다.

### 도메인 서비스가 해결하는 문제

**도메인 로직 유출 방지**:
비즈니스 규칙이 여러 Aggregate를 참조해야 할 때, 해당 로직이 Application Layer(Usecase)로 유출되기 쉽습니다. 도메인 서비스는 이 로직을 Domain Layer에 유지합니다.

**역할 구분 명확화**:
Domain Service(도메인 로직)와 Application Service(Usecase, I/O 조율)의 경계가 명확해집니다.

**아키텍처 테스트 가능**:
`IDomainService` 마커 인터페이스로 아키텍처 규칙을 검증할 수 있습니다 (예: Domain Service가 [IObservablePort](../adapter/12-ports)를 의존하지 않는지).

### 도메인 로직 배치 판단

아래 의사결정 트리는 로직의 특성에 따라 어디에 배치해야 하는지 안내합니다.

```
로직이 단일 Aggregate에 속하는가?
├── YES → Entity 메서드 또는 Value Object
└── NO
    ├── 외부 I/O가 필요한가?
    │   ├── 교차 데이터를 Usecase가 로드할 수 있는 규모인가?
    │   │   ├── YES → 순수 Domain Service (Usecase가 데이터 전달)
    │   │   └── NO → Repository 사용 Domain Service (Evans Ch.9)
    │   └── I/O 불필요 → 순수 Domain Service
    └── 여러 Aggregate의 상태를 변경하는가?
        ├── YES → Domain Event + 별도 Handler
        └── NO → Domain Service
```

**Summary:**

| 조건 | 배치 |
|------|------|
| 단일 Aggregate 내 로직 | Entity 메서드 또는 Value Object |
| 다수 Aggregate 읽기 + 순수 로직 | Domain Service (순수 패턴) |
| 다수 Aggregate + 대규모 교차 데이터 | Domain Service (Repository 패턴) |
| 다수 Aggregate 쓰기 또는 외부 I/O 조율 | Usecase |

다음 표는 위 트리의 결과를 요약한 것입니다.

| 배치 위치 | 기준 | Example |
|----------|------|------|
| **Entity 메서드** | 단일 Aggregate 내부 상태 변경 | `Product.DeductStock()` |
| **Value Object** | 값의 검증, 변환, 연산 | `Money.Add()` |
| **Domain Service (순수)** | 여러 Aggregate 참조, Usecase가 데이터 로드 가능 | `OrderCreditCheckService.ValidateCreditLimit()` |
| **Domain Service (Repository)** | 여러 Aggregate 참조, 대규모 교차 데이터 | `ContactEmailCheckService.ValidateEmailUnique()` |
| **Usecase** | 조율, I/O 위임 | Repository 호출, Event 발행 |

Now that we understand the need for domain services, let us examine their precise definition and characteristics.

---

## 도메인 서비스란 무엇인가 (WHAT)

### Evans의 Domain Service 정의

Evans Blue Book Ch.9에서 Domain Service의 3가지 특성:

1. **도메인 개념에 해당하지만 Entity나 Value Object에 속하지 않는 연산**
2. **인터페이스가 도메인 모델의 다른 요소로 정의됨**
3. **Stateless** — 호출 간 가변 상태 없음

Evans는 **Stateless**를 요구하지만 **Pure**(I/O 없음)를 요구하지 않습니다. Repository 인터페이스는 도메인 레이어에 정의되므로, Domain Service가 이를 사용하는 것은 Evans DDD에서 정당합니다.

### Functorium의 두 가지 패턴

Functorium은 Evans의 Stateless 원칙을 기반으로, 교차 데이터 규모에 따라 두 가지 패턴을 제시합니다.

| 특성 | 순수 패턴 (기본) | Repository 패턴 (Evans Ch.9) |
|------|-----------------|---------------------------|
| **생성 방식** | `new()` 직접 생성 | DI 주입 |
| **I/O** | 없음 | Repository 인터페이스 사용 |
| **반환 타입** | `Fin<T>` | `FinT<IO, T>` |
| **인스턴스 필드** | 없음 | Repository 참조만 허용 |
| **테스트** | Mock 불필요 | Repository 스텁 필요 |
| **적용 시나리오** | 소규모 교차 데이터 | 대규모 교차 데이터 (DB 쿼리 필수) |

두 패턴 모두 Evans의 Stateless 요구사항을 충족합니다. 순수 패턴은 인스턴스 필드가 없고, Repository 패턴은 불변 Repository 참조만 보유합니다.

### IDomainService 마커 인터페이스

**Location**: `Functorium.Domains.Services`

```csharp
public interface IDomainService { }
```

빈 마커 인터페이스입니다. Domain Service임을 선언하고 아키텍처 테스트에서 검증할 수 있게 합니다. 두 패턴 모두 이 인터페이스를 구현합니다.

### Domain Service vs Application Service (Usecase)

아래 표는 Domain Service와 Application Service의 핵심 차이를 정리한 것입니다.

| Category | Domain Service | Application Service (Usecase) |
|------|---------------|-------------------------------|
| **위치** | Domain Layer | Application Layer |
| **I/O** | 없음 (순수 패턴) 또는 Repository만 (Evans 패턴) | 있음 (Repository, Event 발행) |
| **역할** | 비즈니스 규칙 | 조율 (orchestration) |
| **반환** | `Fin<T>` 또는 `FinT<IO, T>` | `FinResponse<T>` |
| **마커** | `IDomainService` | `ICommandUsecase<T,R>` / `IQueryUsecase<T,R>` |

### Functorium 타입 계층에서의 위치

```
Domain Layer
├── Value Object     (SimpleValueObject<T>, ...)
├── Entity           (Entity<TId>, AggregateRoot<TId>)
├── Domain Event     (IDomainEvent, DomainEvent)
├── Domain Service   (IDomainService)         ← 여기
├── Domain Error     (DomainError, DomainErrorType)
└── Repository       (IRepository<TAggregate, TId>)
```

Now that we have confirmed the definition and location of Domain Services, let us look at the implementation step by step.

---

## 도메인 서비스 구현 (HOW)

### Folder Structure

```
LayeredArch.Domain/
├── AggregateRoots/
│   ├── Customers/
│   └── Orders/
├── Services/                              ← Domain Service 배치
│   └── OrderCreditCheckService.cs
└── Using.cs
```

### Namespace

- 프레임워크 인터페이스: `Functorium.Domains.Services`
- 구현 클래스: `{프로젝트}.Domain.Services`

### 순수 패턴 (기본)

Usecase가 교차 데이터를 로드할 수 있는 소규모 시나리오에 적합합니다.

**기본 구조:**

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace {프로젝트}.Domain.Services;

public sealed class {ServiceName} : IDomainService
{
    public sealed record {ErrorName} : DomainErrorType.Custom;

    public Fin<Unit> {메서드명}({AggregateA} a, {AggregateB 데이터} b)
    {
        // 교차 Aggregate 비즈니스 규칙 검증
        if (/* 규칙 위반 */)
            return DomainError.For<{ServiceName}>(
                new {ErrorName}(),
                currentValue,
                "에러 메시지");

        return unit;
    }
}
```

**완전한 예제: OrderCreditCheckService**

Customer의 신용 한도와 Order의 주문 금액 간 교차 Aggregate 비즈니스 규칙을 구현합니다:

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Domain.Services;

public sealed class OrderCreditCheckService : IDomainService
{
    public sealed record CreditLimitExceeded : DomainErrorType.Custom;

    /// <summary>
    /// 주문 금액이 고객의 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"주문 금액 {(decimal)orderAmount}이(가) 고객 신용 한도 {(decimal)customer.CreditLimit}을(를) 초과합니다");

        return unit;
    }

    /// <summary>
    /// 기존 주문들과 신규 주문을 합산하여 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimitWithExistingOrders(
        Customer customer,
        Seq<Order> existingOrders,
        Money newOrderAmount)
    {
        var totalExisting = existingOrders.Fold(0m, (acc, o) => acc + (decimal)o.TotalAmount);
        var totalWithNew = totalExisting + (decimal)newOrderAmount;

        if (totalWithNew > (decimal)customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"총 주문 금액 {totalWithNew}이(가) 고객 신용 한도 {(decimal)customer.CreditLimit}을(를) 초과합니다");

        return unit;
    }
}
```

**Key Points:**

- `sealed class` — 상속 의도 없음
- `Fin<Unit>` 반환 — 성공(`unit`) 또는 `DomainError`
- `DomainError.For<OrderCreditCheckService>` — 에러 코드 자동 생성 (`DomainErrors.OrderCreditCheckService.CreditLimitExceeded`)
- `Money` 비교는 `ComparableSimpleValueObject<decimal>` 연산자 (`>`, `<`, `>=`, `<=`) 사용
- `Seq<T>.Fold` 사용 — `Sum()` 대신 사용 (LanguageExt와 System.Linq 간 모호성 방지)

### Repository 패턴 (Evans Ch.9)

Usecase가 교차 데이터를 로드하기 어려운 대규모 시나리오에 적합합니다. Domain Service가 Repository 인터페이스를 통해 직접 데이터를 조회합니다.

**기본 구조:**

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace {프로젝트}.Domain.Services;

public sealed class {ServiceName} : IDomainService
{
    private readonly I{Aggregate}Repository _repository;

    public {ServiceName}(I{Aggregate}Repository repository)
        => _repository = repository;

    public sealed record {ErrorName} : DomainErrorType.Custom;

    public FinT<IO, Unit> {메서드명}({파라미터})
    {
        // Specification 생성 → Repository 조회 → 검증
        var spec = new {Specification}({파라미터});
        return from exists in _repository.Exists(spec)
               from _ in CheckCondition(exists)
               select unit;
    }

    private static Fin<Unit> CheckCondition(bool condition)
    {
        if (condition)
            return DomainError.For<{ServiceName}>(
                new {ErrorName}(), currentValue, "에러 메시지");
        return unit;
    }
}
```

**완전한 예제: ContactEmailCheckService**

Contact의 이메일 고유성을 검증하는 교차 Aggregate 비즈니스 규칙을 구현합니다. 전체 Contact 테이블을 스캔해야 하므로 Usecase가 데이터를 전달하는 순수 패턴으로는 구현할 수 없습니다:

```csharp
public sealed class ContactEmailCheckService : IDomainService
{
    private readonly IContactRepository _repository;

    public ContactEmailCheckService(IContactRepository repository)
        => _repository = repository;

    public sealed record EmailAlreadyInUse : DomainErrorType.Custom;

    /// <summary>
    /// 이메일 주소가 다른 Contact에서 사용되지 않는지 검증합니다.
    /// </summary>
    public FinT<IO, Unit> ValidateEmailUnique(
        EmailAddress email, Option<ContactId> excludeId = default)
    {
        var spec = new ContactEmailUniqueSpec(email, excludeId);
        return from exists in _repository.Exists(spec)
               from _ in CheckNotExists(email, exists)
               select unit;
    }

    private static Fin<Unit> CheckNotExists(EmailAddress email, bool exists)
    {
        if (exists)
            return DomainError.For<ContactEmailCheckService>(
                new EmailAlreadyInUse(),
                (string)email,
                $"이메일 '{(string)email}'은(는) 이미 사용 중입니다");
        return unit;
    }
}
```

**Key Points:**

- `FinT<IO, Unit>` 반환 — Repository I/O를 포함하므로 `Fin<T>`가 아닌 `FinT<IO, T>`
- Repository는 **인터페이스로만 의존** — Domain Layer에 정의된 인터페이스
- `Specification` 생성 — 쿼리 규칙을 Domain Service가 소유
- LINQ query syntax — `from ... in ...` 체인으로 I/O와 순수 검증을 합성

### Global Using 설정

Domain 프로젝트의 `Using.cs`에 추가:

```csharp
global using Functorium.Domains.Services;
```

Now that we have completed the Domain Service implementation, let us see how to call and integrate it from a Usecase.

---

## Usecase에서 사용 (HOW)

### 순수 패턴: 직접 생성 + 자동 리프팅

순수 패턴의 Domain Service는 상태와 I/O가 없으므로 Usecase에서 **멤버 변수로 직접 생성합니다.** `Fin<Unit>` 반환값은 `FinT<IO, T>` LINQ 체인에서 **자동 리프팅됩니다.**

#### Fin<T> 자동 리프팅

`FinT<IO, T>` LINQ 체인에서 `Fin<T>`를 반환하는 메서드를 `from ... in` 구문으로 직접 사용할 수 있습니다:

```csharp
FinT<IO, Response> usecase =
    from customer in _customerRepository.GetById(customerId)      // FinT<IO, Customer>
    from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Fin<Unit> → 자동 리프팅
    from order in _orderRepository.Create(Order.Create(...))      // FinT<IO, Order>
    select new Response(...);
```

이 패턴은 `Product.DeductStock()` 같은 기존 Entity 메서드와 동일합니다:

```csharp
// Entity 메서드 (기존 패턴)
from _1 in product.DeductStock(quantity)        // Fin<Unit> → 자동 리프팅

// Domain Service (동일 패턴)
from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Fin<Unit> → 자동 리프팅
```

#### 완전한 Usecase 예제

```csharp
public sealed class Usecase(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IProductCatalog productCatalog)
    : ICommandUsecase<Request, Response>
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IProductCatalog _productCatalog = productCatalog;
    private readonly OrderCreditCheckService _creditCheckService = new();  // 직접 생성

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // 1. Value Object 생성 (순수 검증)
        var shippingAddressResult = ShippingAddress.Create(request.ShippingAddress);
        var quantityResult = Quantity.Create(request.Quantity);

        if (shippingAddressResult.IsFail)
            return FinResponse.Fail<Response>(shippingAddressResult.Match(
                Succ: _ => throw new InvalidOperationException(), Fail: e => e));
        if (quantityResult.IsFail)
            return FinResponse.Fail<Response>(quantityResult.Match(
                Succ: _ => throw new InvalidOperationException(), Fail: e => e));

        var customerId = CustomerId.Create(request.CustomerId);
        var productId = ProductId.Create(request.ProductId);
        var shippingAddress = (ShippingAddress)shippingAddressResult;
        var quantity = (Quantity)quantityResult;

        // 2. 조회 → 신용 검증(Domain Service) → 주문 생성 → event publishing
        FinT<IO, Response> usecase =
            from customer in _customerRepository.GetById(customerId)           // 1. 고객 조회
            from exists in _productCatalog.ExistsById(productId)               // 2. 상품 존재 확인
            from _1 in guard(exists, ApplicationError.For<...>(...))            // 3. 상품 없으면 실패
            from unitPrice in _productCatalog.GetPrice(productId)              // 4. 가격 조회
            from _2 in _creditCheckService.ValidateCreditLimit(                 // 5. 신용 한도 검증
                customer, unitPrice.Multiply(quantity))
            from order in _orderRepository.Create(                             // 6. 주문 생성
                Order.Create(productId, quantity, unitPrice, shippingAddress))
            select new Response(...);
            // SaveChanges + event publishing은 UsecaseTransactionPipeline이 자동 처리

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### Repository 패턴: DI 주입 + 직접 체이닝

Repository 패턴의 Domain Service는 DI를 통해 주입받습니다. `FinT<IO, T>`를 반환하므로 자동 리프팅이 아닌 **직접 체이닝됩니다.**

```csharp
public sealed class Usecase(
    IContactRepository repository,
    ContactEmailCheckService emailCheckService)
    : ICommandUsecase<Request, Response>
{
    private readonly IContactRepository _repository = repository;
    private readonly ContactEmailCheckService _emailCheckService = emailCheckService;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ...
        FinT<IO, Response> usecase =
            from _ in _emailCheckService.ValidateEmailUnique(email, excludeId)  // FinT<IO, Unit> 직접 체이닝
            from saved in _repository.Create(contact)
            select new Response(...);
        // ...
    }
}
```

### Flow Comparison

**순수 패턴:**

```
Usecase (Application Layer, I/O 조율)
│
├── Repository.GetById()        ← I/O (Adapter)
├── ProductCatalog.GetPrice()   ← I/O (Adapter)
├── CreditCheckService.Validate()  ← 순수 로직 (Domain Service)
└── Repository.Create()         ← I/O (Adapter)
    // SaveChanges + event publishing은 UsecaseTransactionPipeline이 자동 처리
```

**Repository 패턴:**

```
Usecase (Application Layer, I/O 조율)
│
├── EmailCheckService.ValidateEmailUnique()  ← Domain Service (내부에서 Repository 사용)
└── Repository.Create()                      ← I/O (Adapter)
    // SaveChanges + event publishing은 UsecaseTransactionPipeline이 자동 처리
```

---

## DI Registration

### 순수 패턴: DI 등록 불필요

순수 패턴의 Domain Service는 상태와 생성자 파라미터가 없으므로 **DI 컨테이너에 등록하지 않습니다**. Usecase에서 멤버 변수로 직접 생성합니다.

```csharp
// Usecase 내부에서 직접 생성
private readonly OrderCreditCheckService _creditCheckService = new();
```

### Repository 패턴: DI 등록 필요

Repository 패턴의 Domain Service는 생성자에서 Repository를 주입받으므로 **DI 컨테이너에 등록해야 합니다**.

```csharp
services.AddScoped<ContactEmailCheckService>();
```

### IObservablePort와의 차이

| Category | Domain Service (순수) | Domain Service (Repository) | Adapter (IObservablePort) |
|------|---------------------|---------------------------|-------------------|
| **생성 방식** | `new()` 직접 생성 | DI `AddScoped<>()` | DI `RegisterScopedObservablePort<I, P>()` |
| **Pipeline** | 불필요 | 불필요 | 자동 생성 (관찰 가능성) |
| **Lifetime** | Usecase와 동일 | Scoped (요청별) | Scoped (요청별) |
| **관찰 가능성** | 불필요 | 불필요 | 자동 적용 |

### Domain Service 간 상호 호출

순수 패턴의 Domain Service는 다른 순수 Domain Service를 호출할 수 있습니다:

```csharp
public sealed class OrderPricingService : IDomainService
{
    private readonly DiscountCalculationService _discountService = new();

    public Fin<Money> CalculateFinalPrice(Order order, Customer customer)
    {
        // 다른 Domain Service 호출
        var discount = _discountService.CalculateDiscount(customer, order.TotalAmount);
        return discount.Map(d => order.TotalAmount.Subtract(d));
    }
}
```

**Caution**: Domain Service 간 호출이 3개 이상으로 빈번해지면, 상위 조율 서비스(orchestrating Domain Service)를 도입하거나 Usecase에서 직접 조율하는 것을 검토하세요.

---

## Test Patterns

### 순수 패턴 단위 테스트

순수 패턴의 Domain Service는 Mock 없이 직접 테스트합니다:

```csharp
public class OrderCreditCheckServiceTests
{
    private readonly OrderCreditCheckService _sut = new();

    private static Customer CreateSampleCustomer(decimal creditLimit = 5000m)
    {
        return Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(creditLimit).ThrowIfFail());
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsSuccess_WhenAmountWithinLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(3000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsFail_WhenAmountExceedsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(6000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsSuccess_WhenAmountEqualsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(5000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
```

**Test Characteristics:**

- Mock 불필요 — 순수 함수이므로 입력/출력만 검증
- `_sut = new()` — 의존성 없이 직접 생성
- 경계값 테스트 — `=` (한도와 동일), `<` (한도 미만), `>` (한도 초과)

### Repository 패턴 단위 테스트

Repository 패턴의 Domain Service는 Repository 스텁을 사용하여 테스트합니다:

```csharp
public class ContactEmailCheckServiceTests
{
    private static ContactEmailCheckService CreateSut(bool existsResult)
    {
        var repository = Substitute.For<IContactRepository>();
        repository.Exists(Arg.Any<ContactEmailUniqueSpec>())
            .Returns(FinTFactory.Succ(existsResult));
        return new ContactEmailCheckService(repository);
    }

    [Fact]
    public async Task ValidateEmailUnique_ReturnsSuccess_WhenEmailNotExists()
    {
        // Arrange
        var sut = CreateSut(existsResult: false);
        var email = EmailAddress.Create("new@example.com").ThrowIfFail();

        // Act
        var actual = await sut.ValidateEmailUnique(email).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateEmailUnique_ReturnsFail_WhenEmailExists()
    {
        // Arrange
        var sut = CreateSut(existsResult: true);
        var email = EmailAddress.Create("existing@example.com").ThrowIfFail();

        // Act
        var actual = await sut.ValidateEmailUnique(email).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
```

**Test Characteristics:**

- Repository 스텁 필요 — `Substitute.For<IContactRepository>()`
- `async Task` — `FinT<IO, T>`는 비동기 실행
- `.Run().RunAsync()` — IO 모나드 실행

### Usecase 단위 테스트 (Domain Service 포함)

**순수 패턴**: Domain Service는 Usecase 내부에서 직접 생성되므로 별도 설정이 불필요합니다. Repository/Adapter만 Mock합니다:

```csharp
public class CreateOrderWithCreditCheckCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly CreateOrderWithCreditCheckCommand.Usecase _sut;

    public CreateOrderWithCreditCheckCommandTests()
    {
        _sut = new CreateOrderWithCreditCheckCommand.Usecase(
            _customerRepository, _orderRepository, _productCatalog);
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenCreditLimitExceeded()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 1000m);
        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(Money.Create(1000m).ThrowIfFail()));

        var request = new CreateOrderWithCreditCheckCommand.Request(
            customer.Id.ToString(),
            Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(
                ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert — 1000m × 2 = 2000m > 1000m 신용 한도
        actual.IsSucc.ShouldBeFalse();
    }
}
```

**Repository 패턴**: Domain Service도 DI로 주입되므로 Usecase 테스트에서 직접 생성하여 전달합니다:

```csharp
// Repository 스텁을 사용하여 Domain Service 생성 후 Usecase에 주입
var emailCheckService = new ContactEmailCheckService(stubRepository);
var sut = new CreateContactCommand.Usecase(contactRepository, emailCheckService);
```

### 테스트 폴더 구조

```
LayeredArch.Tests.Unit/
├── Domain/
│   ├── Customers/
│   ├── Orders/
│   ├── Products/
│   ├── Services/                              ← Domain Service 테스트
│   │   └── OrderCreditCheckServiceTests.cs
│   └── SharedModels/
└── Application/
    └── Orders/
        ├── CreateOrderCommandTests.cs
        └── CreateOrderWithCreditCheckCommandTests.cs  ← Usecase 테스트
```

---

## Checklist

### 공통 (두 패턴 모두)

- [ ] `IDomainService` 마커 인터페이스를 구현하는가?
- [ ] `sealed class`로 선언되어 있는가?
- [ ] Domain Layer (`{프로젝트}.Domain.Services` 네임스페이스)에 배치되어 있는가?
- [ ] `IObservablePort`를 상속하지 않는가?
- [ ] 로직이 실제로 여러 Aggregate에 걸쳐 있는가? (단일 Aggregate 로직은 Entity 메서드에 배치)
- [ ] `DomainError.For<{ServiceName}>`으로 에러를 생성하는가?
- [ ] 상태 변경 없이 검증/계산만 수행하는가?

### 순수 패턴 추가 체크리스트

- [ ] 외부 I/O 의존성이 없는가? (Repository, HttpClient 등)
- [ ] 인스턴스 필드가 없는가?
- [ ] `Fin<T>` 또는 `Fin<Unit>`을 반환하는가?
- [ ] Usecase에서 멤버 변수로 직접 생성하는가? (`new()`)
- [ ] `FinT<IO, T>` LINQ 체인에서 `from ... in` 구문으로 호출하는가?
- [ ] Domain Service 자체에 대한 단위 테스트가 Mock 없이 작성되어 있는가?

### Repository 패턴 추가 체크리스트

- [ ] 인스턴스 필드가 Repository 인터페이스 참조만 보유하는가?
- [ ] `FinT<IO, T>` 또는 `FinT<IO, Unit>`을 반환하는가?
- [ ] DI 컨테이너에 `AddScoped<>()`으로 등록되어 있는가?
- [ ] Usecase에서 생성자 주입으로 받는가?
- [ ] Repository 스텁을 사용한 단위 테스트가 있는가?

---

## Troubleshooting

### Domain Service에서 Repository를 사용해야 할지 판단이 어렵다

**판단 기준:** Usecase가 교차 데이터를 로드할 수 있는 규모인지 확인하세요.

- **소규모 (1~수건):** Usecase가 Repository로 데이터를 로드한 후 순수 Domain Service에 전달합니다 (순수 패턴).
  ```csharp
  from customer in _customerRepository.GetById(customerId)        // Usecase가 I/O 처리
  from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Domain Service는 순수 검증만
  ```
- **대규모 (전체 테이블 스캔 등):** Domain Service가 Repository 인터페이스를 통해 직접 조회합니다 (Repository 패턴, Evans Ch.9).
  ```csharp
  from _ in _emailCheckService.ValidateEmailUnique(email, excludeId)  // Domain Service가 내부에서 Repository 사용
  ```

기본적으로 **순수 패턴을 먼저 시도하고,** Usecase가 데이터를 로드하기 어려운 경우에만 Repository 패턴을 검토하세요.

### Domain Service 간 상호 호출이 너무 복잡해졌다

**Cause:** Domain Service 간 호출 체인이 3개 이상으로 늘어나면 복잡도가 증가합니다.

**Resolution:** 상위 조율 Domain Service를 도입하거나, Usecase에서 각 Domain Service를 개별적으로 호출하여 조율하는 방식으로 전환하세요.

### 아키텍처 테스트에서 Domain Service가 Port를 의존한다고 경고한다

**Cause:** Domain Service가 `IObservablePort`를 상속하거나 Port 인터페이스를 생성자 파라미터로 받고 있을 수 있습니다.

**Resolution:** Domain Service는 `IDomainService` 마커만 구현해야 합니다. `IObservablePort`는 Adapter 전용이며, Domain Service에서는 `IObservablePort` 의존성을 제거하세요. Repository 패턴의 경우 Repository 인터페이스는 `IObservablePort`가 아닌 Domain Layer에 정의된 인터페이스를 사용합니다.

### 아키텍처 테스트가 Repository 패턴의 인스턴스 필드를 차단한다

**Cause:** SingleHost의 `DomainServiceArchitectureRuleTests`는 `RequireNoInstanceFields()`로 순수 패턴을 강제합니다.

**Resolution:** 이 아키텍처 테스트는 SingleHost의 참조 구현(순수 패턴)에 적용되는 규칙입니다. Repository 패턴을 사용하는 프로젝트에서는 해당 규칙을 Repository 인터페이스 참조를 허용하도록 조정하거나, 별도 테스트로 분리하세요.

---

## FAQ

### Q1. Domain Service와 Usecase(Application Service)의 구분 기준은?

Domain Service는 비즈니스 규칙을 수행하며 Domain Layer에 위치합니다. Usecase는 I/O 조율을 담당하며 Application Layer에 위치합니다. 순수 패턴에서는 "이 로직에 I/O가 필요한가?"가 핵심 판단 기준이고, Repository 패턴에서는 "이 로직이 도메인 규칙인가, 조율인가?"가 핵심 판단 기준입니다.

### Q2. Evans DDD에서 Domain Service가 Repository를 사용하는 것이 맞나요?

맞습니다. Evans Blue Book Ch.9에서 Domain Service는 **Stateless**만 요구하고 **Pure**를 요구하지 않습니다. Repository 인터페이스는 도메인 레이어에 정의되므로 Domain Service가 이를 사용하는 것은 Evans DDD에서 정당합니다. Functorium의 순수 패턴은 Evans보다 엄격한 기본값이며, 유일한 정답은 아닙니다.

### Q3. 순수 패턴과 Repository 패턴 중 어떤 것을 선택해야 하나요?

**기본값은 순수 패턴입니다.** Usecase가 교차 데이터를 로드할 수 있는 소규모 시나리오에서는 순수 패턴이 단순하고 테스트하기 쉽습니다. Repository 패턴은 다음 조건을 모두 충족할 때만 사용합니다:

- Usecase가 교차 데이터를 로드하기 어려운 규모 (전체 테이블 스캔 등)
- Service가 쿼리 규칙(Specification)을 소유해야 함
- 도메인 로직이 조회와 검증을 하나의 응집된 연산으로 캡슐화해야 함

### Q4. 아키텍처 테스트가 Repository 패턴을 차단하지 않나요?

SingleHost의 `DomainServiceArchitectureRuleTests`는 `RequireNoInstanceFields()`로 순수 패턴을 강제합니다. 이는 SingleHost의 참조 구현에 적용되는 규칙이며, Repository 패턴을 사용하는 프로젝트에서는 해당 규칙을 조정해야 합니다.

### Q5. Domain Service를 DI 컨테이너에 등록하지 않는 이유는?

순수 패턴에만 해당됩니다. 순수 패턴은 상태가 없고 생성자 파라미터도 없으므로 DI가 불필요합니다. Repository 패턴은 생성자에서 Repository를 주입받으므로 `AddScoped<>()`으로 DI 등록이 필요합니다.

### Q6. Domain Service에서 에러를 어떻게 반환하나요?

`DomainError.For<{ServiceName}>(new {ErrorType}(), currentValue, message)` 패턴을 사용합니다. 에러 코드는 `DomainErrors.{ServiceName}.{ErrorType}` 형태로 자동 생성됩니다. 두 패턴 모두 동일합니다.

### Q7. 단일 Aggregate 내부 로직인데 메서드가 너무 복잡하면 Domain Service로 분리해도 되나요?

아닙니다. 단일 Aggregate 내부 로직은 Entity 메서드에 배치하는 것이 원칙입니다. 메서드가 복잡하면 Entity 내부에서 private 메서드로 분리하세요. Domain Service는 **여러 Aggregate에 걸친** 로직에만 사용합니다.

### Q8. Domain Service의 테스트에서 Mock이 필요한가요?

순수 패턴은 Mock 없이 직접 입력/출력만 검증합니다. Repository 패턴은 Repository 스텁(NSubstitute 등)이 필요합니다. 두 경우 모두 Domain Service의 비즈니스 규칙 자체를 검증하는 것이 핵심입니다.

---

## References

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview) - DDD 전술적 설계 개요, 타입 매핑 테이블
- [06a-aggregate-design.md](./06a-aggregate-design) - Aggregate 설계 원칙, [06b-entity-aggregate-core.md](./06b-entity-aggregate-core) - Entity/Aggregate 핵심 패턴, [06c-entity-aggregate-advanced.md](./06c-entity-aggregate-advanced) - 고급 패턴
- [08a-error-system.md](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [08b-error-system-domain-app.md](./08b-error-system-domain-app) - DomainError 정의 및 테스트 패턴
- [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) - Usecase 구현 (Application Service)
- [12-ports.md](../adapter/12-ports) - Port/Adapter 패턴 (IPort와의 차이)
- [15a-unit-testing.md](../testing/15a-unit-testing) - 단위 테스트 규칙 (T1_T2_T3, AAA 패턴)

### Practical Examples 파일

| File | Description |
|------|------|
| `Src/Functorium/Domains/Services/IDomainService.cs` | 마커 인터페이스 |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/Services/OrderCreditCheckService.cs` | 순수 패턴 구현 |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Orders/CreateOrderWithCreditCheckCommand.cs` | 순수 패턴 Usecase 사용 |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Services/OrderCreditCheckServiceTests.cs` | 순수 패턴 테스트 |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Orders/CreateOrderWithCreditCheckCommandTests.cs` | Usecase 테스트 |

### designing-with-types 예제 (Repository 패턴)

| File | Description |
|------|------|
| `Docs.Site/src/content/docs/samples/designing-with-types/Src/DesigningWithTypes/AggregateRoots/Contacts/Services/ContactEmailCheckService.cs` | Repository 패턴 구현 |
