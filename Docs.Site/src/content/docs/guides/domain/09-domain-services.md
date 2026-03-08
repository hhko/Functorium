---
title: "도메인 서비스 (Domain Services)"
---

여러 Aggregate에 걸친 비즈니스 규칙은 어디에 두어야 할까요? Entity에 넣으면 경계를 넘고, Usecase에 넣으면 도메인 로직이 유출됩니다. 도메인 서비스는 이 문제를 해결합니다.

## 들어가며

"주문 금액이 고객의 신용 한도를 초과하는지 검증하는 로직은 Order에 넣어야 하나, Customer에 넣어야 하나?"
"여러 Aggregate를 참조하는 순수 비즈니스 규칙이 Usecase에 있으면 도메인 로직이 유출되는 것 아닌가?"
"Domain Service와 Application Service(Usecase)의 경계는 어디인가?"

이러한 질문은 비즈니스 로직이 단일 Aggregate의 경계를 넘어설 때 반복적으로 발생합니다. 도메인 서비스는 외부 I/O 없이 여러 Aggregate를 참조하는 순수 도메인 로직을 Domain Layer에 유지하는 빌딩블록입니다.

### 이 문서에서 배우는 내용

1. **도메인 서비스의 배치 판단 기준** — Entity 메서드, Usecase, Domain Service 중 어디에 로직을 둘지 결정하는 의사결정 트리
2. **`IDomainService` 마커 인터페이스와 순수 함수 패턴** — 상태 없음, I/O 없음, DI 불필요
3. **Usecase에서의 통합 방법** — `FinT<IO, T>` LINQ 체인에서 `Fin<T>` 자동 리프팅

### 사전 지식

- [Aggregate 설계 원칙](./06a-aggregate-design) — Aggregate 경계와 트랜잭션 원칙
- [에러 시스템: 기초와 네이밍](./08a-error-system) — `Fin<T>` 반환 패턴

> 도메인 서비스의 핵심 원칙은 **"순수 함수"입니다.** 외부 I/O 없이, 상태 없이, 여러 Aggregate의 값을 받아 비즈니스 규칙을 검증합니다. DI 컨테이너 등록 없이 Usecase에서 `new()`로 직접 생성하여 사용합니다.

## 요약

### 주요 명령

```csharp
// Domain Service 정의
public sealed class OrderCreditCheckService : IDomainService
{
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount) { ... }
}

// Usecase에서 직접 생성 (DI 불필요)
private readonly OrderCreditCheckService _creditCheckService = new();

// FinT<IO, T> LINQ 체인에서 사용 (자동 리프팅)
from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)
```

### 주요 절차

1. **배치 판단**: 로직이 여러 Aggregate에 걸치고 I/O가 없는지 확인
2. **클래스 정의**: `sealed class`, `IDomainService` 마커 구현
3. **메서드 작성**: `Fin<T>` 또는 `Fin<Unit>` 반환, 상태 없는 순수 함수
4. **에러 정의**: `DomainError.For<{ServiceName}>()` 패턴으로 에러 코드 생성
5. **Usecase 통합**: 멤버 변수로 `new()` 직접 생성, `from ... in` 구문으로 호출

### 주요 개념

| 개념 | 설명 |
|------|------|
| `IDomainService` | 빈 마커 인터페이스, 아키텍처 테스트 검증용 |
| 순수 함수 | 외부 I/O 없음, 상태 없음, Repository/HttpClient 의존 금지 |
| `Fin<Unit>` 반환 | 성공(`unit`) 또는 `DomainError` 반환 |
| DI 불필요 | Usecase에서 `new()`로 직접 생성 |
| 자동 리프팅 | `FinT<IO, T>` LINQ 체인에서 `Fin<T>` 자동 리프팅 |

---

## 왜 도메인 서비스인가

도메인 서비스는 DDD(Domain-Driven Design)에서 **여러 Aggregate에 걸친 순수 도메인 로직을** 배치하는 빌딩블록입니다.

### 도메인 서비스가 해결하는 문제

**도메인 로직 유출 방지**:
비즈니스 규칙이 여러 Aggregate를 참조해야 할 때, 해당 로직이 Application Layer(Usecase)로 유출되기 쉽습니다. 도메인 서비스는 이 로직을 Domain Layer에 유지합니다.

**역할 구분 명확화**:
Domain Service(순수 도메인 로직)와 Application Service(Usecase, I/O 조율)의 경계가 명확해집니다.

**아키텍처 테스트 가능**:
`IDomainService` 마커 인터페이스로 아키텍처 규칙을 검증할 수 있습니다 (예: Domain Service가 [IObservablePort](../adapter/12-ports)를 의존하지 않는지).

### 도메인 로직 배치 판단

아래 의사결정 트리는 로직의 특성에 따라 어디에 배치해야 하는지 안내합니다.

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

다음 표는 위 트리의 결과를 요약한 것입니다.

| 배치 위치 | 기준 | 예시 |
|----------|------|------|
| **Entity 메서드** | 단일 Aggregate 내부 상태 변경 | `Product.DeductStock()` |
| **Value Object** | 값의 검증, 변환, 연산 | `Money.Add()` |
| **Domain Service** | 여러 Aggregate 참조, 순수 로직 | `OrderCreditCheckService.ValidateCreditLimit()` |
| **Usecase** | 조율, I/O 위임 | Repository 호출, Event 발행 |

도메인 서비스의 필요성을 이해했다면, 이제 그 정확한 정의와 특성을 살펴봅니다.

---

## 도메인 서비스란 무엇인가 (WHAT)

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
| **IObservablePort 미상속** | Port/Adapter가 아님, Pipeline 불필요 |
| **상태 없음** | 인스턴스 필드 없이 메서드만 보유 |
| **Fin<T> 반환** | 도메인 규칙 위반 시 `DomainError` 반환 |

### Domain Service vs Application Service (Usecase)

아래 표는 Domain Service와 Application Service의 핵심 차이를 정리한 것입니다.

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

Domain Service의 정의와 위치를 확인했으니, 이제 실제 구현 방법을 단계별로 살펴봅니다.

---

## 도메인 서비스 구현 (HOW)

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

Domain Service의 구현을 완료했다면, 이제 Usecase에서 어떻게 호출하고 통합하는지 확인합니다.

---

## Usecase에서 사용 (HOW)

Domain Service는 순수 함수(상태 없음, I/O 없음)이므로 Usecase에서 **멤버 변수로 직접 생성합니다.** DI 주입이 불필요합니다. `Fin<Unit>` 반환값은 `FinT<IO, T>` LINQ 체인에서 **자동 리프팅됩니다.**

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
            select new Response(...);
            // SaveChanges + 이벤트 발행은 UsecaseTransactionPipeline이 자동 처리

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
└── Repository.Create()         ← I/O (Adapter)
    // SaveChanges + 이벤트 발행은 UsecaseTransactionPipeline이 자동 처리
```

Domain Service는 I/O 없이 순수 비즈니스 규칙만 수행하고, Usecase가 I/O를 조율합니다.

---

## DI 등록 불필요

Domain Service는 순수 함수(상태 없음, I/O 없음, 생성자 파라미터 없음)이므로 **DI 컨테이너에 등록하지 않습니다**. Usecase에서 멤버 변수로 직접 생성하는 것이 권장 패턴입니다.

```csharp
// Usecase 내부에서 직접 생성
private readonly OrderCreditCheckService _creditCheckService = new();
```

**`IObservablePort`와의 차이:**

| 구분 | Domain Service | Adapter (IObservablePort) |
|------|---------------|-------------------|
| **생성 방식** | Usecase에서 `new()` 직접 생성 | DI `RegisterScopedObservablePort<I, P>()` |
| **Pipeline** | 불필요 (순수 로직) | 자동 생성 (관찰 가능성) |
| **Lifetime** | Usecase와 동일 | Scoped (요청별) |
| **관찰 가능성** | 불필요 | 자동 적용 |

### Domain Service 간 상호 호출

Domain Service는 순수 함수이므로 다른 Domain Service를 호출할 수 있습니다:

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

**주의**: Domain Service 간 호출이 3개 이상으로 빈번해지면, 상위 조율 서비스(orchestrating Domain Service)를 도입하거나 Usecase에서 직접 조율하는 것을 검토하세요.

---

## 테스트 패턴

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

Usecase 테스트에서 Domain Service는 Usecase 내부에서 직접 생성되므로 별도 설정이 불필요합니다. Repository/Adapter만 Mock합니다:

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

## 체크리스트

### 도메인 서비스 정의

- [ ] `IDomainService` 마커 인터페이스를 구현하는가?
- [ ] `sealed class`로 선언되어 있는가?
- [ ] Domain Layer (`{프로젝트}.Domain.Services` 네임스페이스)에 배치되어 있는가?
- [ ] 외부 I/O 의존성이 없는가? (Repository, HttpClient 등)
- [ ] `IObservablePort`를 상속하지 않는가?
- [ ] 로직이 실제로 여러 Aggregate에 걸쳐 있는가? (단일 Aggregate 로직은 Entity 메서드에 배치)

### 메서드 설계

- [ ] `Fin<T>` 또는 `Fin<Unit>`을 반환하는가?
- [ ] `DomainError.For<{ServiceName}>`으로 에러를 생성하는가?
- [ ] 상태 변경 없이 검증/계산만 수행하는가?

### Usecase 통합

- [ ] Usecase에서 멤버 변수로 직접 생성하는가? (`new()`)
- [ ] `FinT<IO, T>` LINQ 체인에서 `from ... in` 구문으로 호출하는가?

### 테스트

- [ ] Domain Service 자체에 대한 단위 테스트가 있는가?
- [ ] 경계값 (같음, 미만, 초과) 테스트가 있는가?
- [ ] Usecase 테스트에서 실제 인스턴스를 사용하는가? (Mock 아님)

---

## 트러블슈팅

### Domain Service에서 Repository를 사용하고 싶다

**원인:** Domain Service는 순수 함수여야 하므로 Repository 같은 I/O 의존성을 가질 수 없습니다. I/O가 필요한 로직은 Domain Service가 아닙니다.

**해결:** Repository 호출이 필요한 로직은 Usecase(Application Service)에서 조율하세요. Usecase가 Repository에서 데이터를 조회한 후, 조회된 데이터를 Domain Service에 전달하는 패턴을 사용합니다:
```csharp
from customer in _customerRepository.GetById(customerId)        // Usecase가 I/O 처리
from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Domain Service는 순수 검증만
```

### Domain Service 간 상호 호출이 너무 복잡해졌다

**원인:** Domain Service 간 호출 체인이 3개 이상으로 늘어나면 복잡도가 증가합니다.

**해결:** 상위 조율 Domain Service를 도입하거나, Usecase에서 각 Domain Service를 개별적으로 호출하여 조율하는 방식으로 전환하세요.

### 아키텍처 테스트에서 Domain Service가 Port를 의존한다고 경고한다

**원인:** Domain Service가 `IObservablePort`를 상속하거나 Port 인터페이스를 생성자 파라미터로 받고 있을 수 있습니다.

**해결:** Domain Service는 `IDomainService` 마커만 구현해야 합니다. `IObservablePort`는 Adapter 전용이며, Domain Service에서는 외부 의존성을 일체 제거하세요.

---

## FAQ

### Q1. Domain Service와 Usecase(Application Service)의 구분 기준은?

Domain Service는 순수 비즈니스 규칙만 수행하며 I/O가 없습니다. Usecase는 Repository 호출, 이벤트 발행 등 I/O를 조율합니다. "이 로직에 Repository 호출이 필요한가?"가 핵심 판단 기준입니다.

### Q2. Domain Service를 DI 컨테이너에 등록하지 않는 이유는?

Domain Service는 상태가 없고 생성자 파라미터도 없는 순수 함수입니다. DI를 통해 주입할 이유가 없으며, Usecase에서 `new()`로 직접 생성하는 것이 더 단순하고 명시적입니다.

### Q3. Domain Service에서 에러를 어떻게 반환하나요?

`DomainError.For<{ServiceName}>(new {ErrorType}(), currentValue, message)` 패턴을 사용합니다. 에러 코드는 `DomainErrors.{ServiceName}.{ErrorType}` 형태로 자동 생성됩니다.

### Q4. 단일 Aggregate 내부 로직인데 메서드가 너무 복잡하면 Domain Service로 분리해도 되나요?

아닙니다. 단일 Aggregate 내부 로직은 Entity 메서드에 배치하는 것이 원칙입니다. 메서드가 복잡하면 Entity 내부에서 private 메서드로 분리하세요. Domain Service는 **여러 Aggregate에 걸친** 로직에만 사용합니다.

### Q5. Domain Service의 테스트에서 Mock이 필요한가요?

아닙니다. Domain Service는 순수 함수이므로 Mock 없이 직접 입력/출력만 검증합니다. `_sut = new()` 후 메서드를 호출하고 반환값을 Assert하면 됩니다.

---

## 참고 문서

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview) - DDD 전술적 설계 개요, 타입 매핑 테이블
- [06a-aggregate-design.md](./06a-aggregate-design) - Aggregate 설계 원칙, [06b-entity-aggregate-core.md](./06b-entity-aggregate-core) - Entity/Aggregate 핵심 패턴, [06c-entity-aggregate-advanced.md](./06c-entity-aggregate-advanced) - 고급 패턴
- [08a-error-system.md](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [08b-error-system-domain-app.md](./08b-error-system-domain-app) - DomainError 정의 및 테스트 패턴
- [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) - Usecase 구현 (Application Service)
- [12-ports.md](../adapter/12-ports) - Port/Adapter 패턴 (IPort와의 차이)
- [15a-unit-testing.md](../testing/15a-unit-testing) - 단위 테스트 규칙 (T1_T2_T3, AAA 패턴)

### 실전 예제 파일

| 파일 | 설명 |
|------|------|
| `Src/Functorium/Domains/Services/IDomainService.cs` | 마커 인터페이스 |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/Services/OrderCreditCheckService.cs` | Domain Service 구현 |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Orders/CreateOrderWithCreditCheckCommand.cs` | Usecase에서 사용 |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Services/OrderCreditCheckServiceTests.cs` | Domain Service 테스트 |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Orders/CreateOrderWithCreditCheckCommandTests.cs` | Usecase 테스트 |
