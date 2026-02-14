# 도메인 서비스 (Domain Services)

이 문서는 Functorium 프레임워크에서 도메인 서비스를 정의하고 사용하는 방법을 설명합니다.

## 목차

- [1. 왜 도메인 서비스인가 (WHY)](#1-왜-도메인-서비스인가-why)
- [2. 도메인 서비스란 무엇인가 (WHAT)](#2-도메인-서비스란-무엇인가-what)
- [3. 도메인 서비스 구현 (HOW)](#3-도메인-서비스-구현-how)
- [4. Usecase에서 사용 (HOW)](#4-usecase에서-사용-how)
- [5. DI 등록](#5-di-등록)
- [6. 테스트 패턴](#6-테스트-패턴)
- [7. 체크리스트](#7-체크리스트)
- [참고 문서](#참고-문서)

---

## 1. 왜 도메인 서비스인가 (WHY)

도메인 서비스는 DDD(Domain-Driven Design)에서 **여러 Aggregate에 걸친 순수 도메인 로직**을 배치하는 빌딩블록입니다.

### 도메인 서비스가 해결하는 문제

**도메인 로직 유출 방지**:
비즈니스 규칙이 여러 Aggregate를 참조해야 할 때, 해당 로직이 Application Layer(Usecase)로 유출되기 쉽습니다. 도메인 서비스는 이 로직을 Domain Layer에 유지합니다.

**역할 구분 명확화**:
Domain Service(순수 도메인 로직)와 Application Service(Usecase, I/O 조율)의 경계가 명확해집니다.

**아키텍처 테스트 가능**:
`IDomainService` 마커 인터페이스로 아키텍처 규칙을 검증할 수 있습니다 (예: Domain Service가 IAdapter를 의존하지 않는지).

### 도메인 로직 배치 판단

```
로직이 단일 Aggregate에 속하는가?
├── YES → Entity 메서드 또는 Value Object
└── NO
    ├── 외부 I/O가 필요한가?
    │   ├── YES → Usecase에서 조율
    │   └── NO → Domain Service ← 여기
    └── 여러 Aggregate의 상태를 변경하는가?
        ├── YES → Domain Event + 별도 Handler
        └── NO → Domain Service ← 여기
```

| 배치 위치 | 기준 | 예시 |
|----------|------|------|
| **Entity 메서드** | 단일 Aggregate 내부 상태 변경 | `Product.DeductStock()` |
| **Value Object** | 값의 검증, 변환, 연산 | `Money.Add()` |
| **Domain Service** | 여러 Aggregate 참조, 순수 로직 | `OrderCreditCheckService.ValidateCreditLimit()` |
| **Usecase** | 조율, I/O 위임 | Repository 호출, Event 발행 |

---

## 2. 도메인 서비스란 무엇인가 (WHAT)

### IDomainService 마커 인터페이스

**위치**: `Functorium.Domains.Services`

```csharp
public interface IDomainService { }
```

빈 마커 인터페이스입니다. Domain Service임을 선언하고 아키텍처 테스트에서 검증할 수 있게 합니다.

### Domain Service의 특성

| 특성 | 설명 |
|------|------|
| **순수 함수** | 외부 I/O 없음 (Repository, HTTP 호출 없음) |
| **여러 Aggregate 참조** | 단일 Aggregate에 속하지 않는 비즈니스 로직 |
| **IAdapter 미상속** | Port/Adapter가 아님, Pipeline 불필요 |
| **상태 없음** | 인스턴스 필드 없이 메서드만 보유 |
| **Fin<T> 반환** | 도메인 규칙 위반 시 `DomainError` 반환 |

### Domain Service vs Application Service (Usecase)

| 구분 | Domain Service | Application Service (Usecase) |
|------|---------------|-------------------------------|
| **위치** | Domain Layer | Application Layer |
| **I/O** | 없음 (순수 함수) | 있음 (Repository, Event 발행) |
| **역할** | 비즈니스 규칙 | 조율 (orchestration) |
| **반환** | `Fin<T>` | `FinResponse<T>` |
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

---

## 3. 도메인 서비스 구현 (HOW)

### 폴더 구조

```
LayeredArch.Domain/
├── AggregateRoots/
│   ├── Customers/
│   └── Orders/
├── Services/                              ← Domain Service 배치
│   └── OrderCreditCheckService.cs
└── Using.cs
```

### 네임스페이스

- 프레임워크 인터페이스: `Functorium.Domains.Services`
- 구현 클래스: `{프로젝트}.Domain.Services`

### 기본 구조

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace {프로젝트}.Domain.Services;

public sealed class {ServiceName} : IDomainService
{
    public Fin<Unit> {메서드명}({AggregateA} a, {AggregateB 데이터} b)
    {
        // 교차 Aggregate 비즈니스 규칙 검증
        if (/* 규칙 위반 */)
            return DomainError.For<{ServiceName}>(
                new Custom("{ErrorName}"),
                currentValue,
                "에러 메시지");

        return unit;
    }
}
```

### 완전한 예제: OrderCreditCheckService

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
    /// <summary>
    /// 주문 금액이 고객의 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new Custom("CreditLimitExceeded"),
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
                new Custom("CreditLimitExceeded"),
                customer.Id.ToString(),
                $"총 주문 금액 {totalWithNew}이(가) 고객 신용 한도 {(decimal)customer.CreditLimit}을(를) 초과합니다");

        return unit;
    }
}
```

**핵심 포인트:**

- `sealed class` — 상속 의도 없음
- `Fin<Unit>` 반환 — 성공(`unit`) 또는 `DomainError`
- `DomainError.For<OrderCreditCheckService>` — 에러 코드 자동 생성 (`DomainErrors.OrderCreditCheckService.CreditLimitExceeded`)
- `Money` 비교는 `ComparableSimpleValueObject<decimal>` 연산자 (`>`, `<`, `>=`, `<=`) 사용
- `Seq<T>.Fold` 사용 — `Sum()` 대신 사용 (LanguageExt와 System.Linq 간 모호성 방지)

### Global Using 설정

Domain 프로젝트의 `Using.cs`에 추가:

```csharp
global using Functorium.Domains.Services;
```

---

## 4. Usecase에서 사용 (HOW)

Domain Service는 Usecase에서 생성자 주입으로 사용합니다. `Fin<Unit>` 반환값은 `FinT<IO, T>` LINQ 체인에서 **자동 리프팅**됩니다.

### Fin<T> 자동 리프팅

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

### 완전한 Usecase 예제

```csharp
public sealed class Usecase(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IProductCatalog productCatalog,
    OrderCreditCheckService creditCheckService,  // Domain Service 주입
    IDomainEventPublisher eventPublisher)
    : ICommandUsecase<Request, Response>
{
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

        // 2. 조회 → 신용 검증(Domain Service) → 주문 생성 → 이벤트 발행
        FinT<IO, Response> usecase =
            from customer in _customerRepository.GetById(customerId)           // 1. 고객 조회
            from exists in _productCatalog.ExistsById(productId)               // 2. 상품 존재 확인
            from _1 in guard(exists, ApplicationError.For<...>(...))            // 3. 상품 없으면 실패
            from unitPrice in _productCatalog.GetPrice(productId)              // 4. 가격 조회
            from _2 in _creditCheckService.ValidateCreditLimit(                 // 5. 신용 한도 검증
                customer, unitPrice.Multiply(quantity))
            from order in _orderRepository.Create(                             // 6. 주문 생성
                Order.Create(productId, quantity, unitPrice, shippingAddress))
            from _3 in _eventPublisher.PublishEvents(order, cancellationToken)  // 7. 이벤트 발행
            select new Response(...);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### 흐름 요약

```
Usecase (Application Layer, I/O 조율)
│
├── Repository.GetById()        ← I/O (Adapter)
├── ProductCatalog.GetPrice()   ← I/O (Adapter)
├── CreditCheckService.Validate()  ← 순수 로직 (Domain Service)
├── Repository.Create()         ← I/O (Adapter)
└── EventPublisher.Publish()    ← I/O (Adapter)
```

Domain Service는 I/O 없이 순수 비즈니스 규칙만 수행하고, Usecase가 I/O를 조율합니다.

---

## 5. DI 등록

Domain Service는 상태가 없으므로 **Singleton**으로 등록합니다.

```csharp
// AdapterInfrastructureRegistration.cs
services.AddSingleton<OrderCreditCheckService>();
```

**`IAdapter`와의 차이:**

| 구분 | Domain Service | Adapter (IAdapter) |
|------|---------------|-------------------|
| **등록 방식** | `AddSingleton<T>()` | `RegisterScopedAdapterPipeline<I, P>()` |
| **Pipeline** | 불필요 (순수 로직) | 자동 생성 (관찰 가능성) |
| **Lifetime** | Singleton (상태 없음) | Scoped (요청별) |
| **관찰 가능성** | 불필요 | 자동 적용 |

---

## 6. 테스트 패턴

### Domain Service 단위 테스트

Domain Service는 순수 함수이므로 Mock 없이 직접 테스트합니다:

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

**테스트 특성:**

- Mock 불필요 — 순수 함수이므로 입력/출력만 검증
- `_sut = new()` — 의존성 없이 직접 생성
- 경계값 테스트 — `=` (한도와 동일), `<` (한도 미만), `>` (한도 초과)

### Usecase 단위 테스트 (Domain Service 포함)

Usecase 테스트에서 Domain Service는 실제 인스턴스를 사용하고, Repository/Adapter만 Mock합니다:

```csharp
public class CreateOrderWithCreditCheckCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly OrderCreditCheckService _creditCheckService = new();  // 실제 인스턴스
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();

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
            customer.Id.ToString(), ProductId.New().ToString(), 2, "Seoul, Korea");

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert — 1000m × 2 = 2000m > 1000m 신용 한도
        actual.IsSucc.ShouldBeFalse();
    }
}
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
│   └── SharedKernel/
└── Application/
    └── Orders/
        ├── CreateOrderCommandTests.cs
        └── CreateOrderWithCreditCheckCommandTests.cs  ← Usecase 테스트
```

---

## 7. 체크리스트

### 도메인 서비스 정의

- [ ] `IDomainService` 마커 인터페이스를 구현하는가?
- [ ] `sealed class`로 선언되어 있는가?
- [ ] Domain Layer (`{프로젝트}.Domain.Services` 네임스페이스)에 배치되어 있는가?
- [ ] 외부 I/O 의존성이 없는가? (Repository, HttpClient 등)
- [ ] `IAdapter`를 상속하지 않는가?
- [ ] 로직이 실제로 여러 Aggregate에 걸쳐 있는가? (단일 Aggregate 로직은 Entity 메서드에 배치)

### 메서드 설계

- [ ] `Fin<T>` 또는 `Fin<Unit>`을 반환하는가?
- [ ] `DomainError.For<{ServiceName}>`으로 에러를 생성하는가?
- [ ] 상태 변경 없이 검증/계산만 수행하는가?

### Usecase 통합

- [ ] Usecase에서 생성자 주입으로 사용하는가?
- [ ] `FinT<IO, T>` LINQ 체인에서 `from ... in` 구문으로 호출하는가?
- [ ] DI에 Singleton으로 등록되어 있는가?

### 테스트

- [ ] Domain Service 자체에 대한 단위 테스트가 있는가?
- [ ] 경계값 (같음, 미만, 초과) 테스트가 있는가?
- [ ] Usecase 테스트에서 실제 인스턴스를 사용하는가? (Mock 아님)

---

## 참고 문서

- [01-ddd-tactical-overview.md](./01-ddd-tactical-overview.md) - DDD 전술적 설계 개요, 타입 매핑 테이블
- [03-entities-and-aggregates.md](./03-entities-and-aggregates.md) - Entity/Aggregate 설계 (단일 Aggregate 로직)
- [05-error-system.md](./05-error-system.md) - DomainError 정의 및 테스트 패턴
- [07-usecases-and-cqrs.md](./07-usecases-and-cqrs.md) - Usecase 구현 (Application Service)
- [08-ports-and-adapters.md](./08-ports-and-adapters.md) - Port/Adapter 패턴 (IAdapter와의 차이)
- [09-unit-testing.md](./09-unit-testing.md) - 단위 테스트 규칙 (T1_T2_T3, AAA 패턴)
- [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) §6 - Domain Service 구현 완료 기록

### 실전 예제 파일

| 파일 | 설명 |
|------|------|
| `Src/Functorium/Domains/Services/IDomainService.cs` | 마커 인터페이스 |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/Services/OrderCreditCheckService.cs` | Domain Service 구현 |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Orders/CreateOrderWithCreditCheckCommand.cs` | Usecase에서 사용 |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Services/OrderCreditCheckServiceTests.cs` | Domain Service 테스트 |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Orders/CreateOrderWithCreditCheckCommandTests.cs` | Usecase 테스트 |
