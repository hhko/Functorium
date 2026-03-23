---
title: "Application 레이어 개발 스킬"
---

## 배경

CQRS 기반 Application 레이어를 구현할 때, Command/Query/EventHandler 유스케이스는 동일한 중첩 클래스 구조(Request, Response, Validator, Usecase)를 반복합니다. Value Object 검증의 Apply 병합 패턴, LINQ 기반 함수형 체이닝, `guard()` 조건 검사 등은 모든 유스케이스에 공통으로 적용되는 패턴입니다.

`/application-develop` 스킬은 이 반복을 자동화합니다. 자연어로 유스케이스 요구사항을 전달하면, Functorium 프레임워크 패턴에 맞는 Command, Query, EventHandler, Validator를 4단계로 생성합니다.

## 스킬 개요

### 4단계 프로세스

| 단계 | 작업 | 산출물 |
|------|------|--------|
| 1 | 유스케이스 분해 | Command/Query/Event 식별, Request/Response 설계 |
| 2 | 포트 식별 | Repository, Query Adapter, External API 등 필요한 Port 인터페이스 |
| 3 | 핸들러 구현 | Apply 패턴, LINQ 체이닝, guard 조건 검사 적용 |
| 4 | 구현 검증 | `dotnet build` + `dotnet test` 통과 |

### 지원하는 패턴

| 패턴 | Request 인터페이스 | Handler 인터페이스 | 설명 |
|------|-------------------|-------------------|------|
| Command | `ICommandRequest<TSuccess>` | `ICommandUsecase<TCommand, TSuccess>` | 상태 변경 (쓰기) |
| Query | `IQueryRequest<TSuccess>` | `IQueryUsecase<TQuery, TSuccess>` | 데이터 조회 (읽기) |
| Event | `IDomainEvent` | `IDomainEventHandler<TEvent>` | 도메인 이벤트 처리 |
| Validation | `AbstractValidator<Request>` | — | FluentValidation 검증 규칙 |

### 핵심 API 패턴

| 패턴 | 사용법 |
|------|--------|
| LINQ 함수형 체이닝 | `from x in repo.Method() select new Response(...)` |
| Apply 병합 | `(v1, v2, v3).Apply((a, b, c) => ...).As().ToFin()` |
| guard 조건 검사 | `from _ in guard(!exists, ApplicationError.For<T>(...))` |
| 실행 흐름 | `await usecase.Run().RunAsync()` → `.ToFinResponse()` |
| Application 에러 | `ApplicationError.For<TUsecase>(new AlreadyExists(), value, message)` |

## 사용 방법

### 기본 호출

```text
/application-develop 상품 생성 Command Usecase를 만들어줘. Request에 Name, Price 포함.
```

### 대화형 모드

인자 없이 `/application-develop`만 호출하면, 스킬이 대화형으로 요구사항을 수집합니다.

### 실행 흐름

1. **유스케이스 분석** — Command/Query/Event 식별 결과를 표로 보여줍니다
2. **사용자 확인** — 분석 결과를 확인한 후 코드 생성으로 진행합니다
3. **코드 생성** — 중첩 클래스 패턴으로 유스케이스를 생성합니다
4. **빌드/테스트 검증** — `dotnet build`와 `dotnet test`를 실행하여 통과를 확인합니다

## 예제 1: 초급 — 단일 상품 생성 Command

가장 기본적인 Command 유스케이스입니다. 중첩 클래스 패턴(Request, Response, Validator, Usecase)의 전체 구조와, Value Object 검증을 Apply 패턴으로 병합하는 방법을 보여줍니다.

### 프롬프트

```text
/application-develop 상품 생성 Command Usecase를 만들어줘. Request에 Name, Price 포함.
```

### 기대 결과

| 산출물 | 타입 | 설명 |
|--------|------|------|
| Command | `CreateProductCommand` | 중첩 클래스 패턴 (Request, Response, Validator, Usecase) |
| Request | `Request(string Name, decimal Price)` | `ICommandRequest<Response>` |
| Validator | `Validator` | FluentValidation + `MustSatisfyValidation` |
| Usecase | `Usecase` | `ICommandUsecase<Request, Response>`, LINQ 체이닝 |

### 핵심 스니펫

**중첩 클래스 패턴** — Request, Response, Validator, Usecase를 하나의 파일에 응집:

```csharp
public sealed class CreateProductCommand
{
    public sealed record Request(string Name, decimal Price)
        : ICommandRequest<Response>;

    public sealed record Response(string ProductId, string Name, decimal Price, DateTime CreatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
            RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
        }
    }

    public sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 검증 (Apply 패턴)
            var createData = CreateProductData(request);
            if (createData.IsFail)
            {
                return createData.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            var product = (Product)createData;

            // 2. LINQ 함수형 체이닝
            FinT<IO, Response> usecase =
                from created in _productRepository.Create(product)
                select new Response(
                    created.Id.ToString(),
                    created.Name,
                    created.Price,
                    created.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Fin<Product> CreateProductData(Request request)
        {
            var name = ProductName.Validate(request.Name);
            var price = Money.Validate(request.Price);

            return (name, price)
                .Apply((n, p) =>
                    Product.Create(
                        ProductName.Create(n).ThrowIfFail(),
                        Money.Create(p).ThrowIfFail()))
                .As()
                .ToFin();
        }
    }
}
```

## 예제 2: 중급 — Command + Query + Validator

예제 1에 Query 유스케이스와 중복 검증을 추가합니다. Command에서 `guard()`를 사용한 이름 중복 검사, Query에서 LINQ 체이닝을 통한 조회, FluentValidation과 Value Object의 이중 검증 전략을 보여줍니다.

### 프롬프트

```text
/application-develop 상품 CRUD를 구현해줘. 생성/수정 Command, ID 조회/검색 Query, 이름 중복 검증 포함.
```

### 기대 결과

| 산출물 | 타입 | 설명 |
|--------|------|------|
| Command | `CreateProductCommand` | 생성 + 이름 중복 검사 (`guard`) |
| Command | `UpdateProductCommand` | 수정 + Apply 병합 |
| Query | `GetProductByIdQuery` | ID 기반 단건 조회 |
| Query | `SearchProductsQuery` | 페이지네이션/정렬 지원 검색 |
| Validator | 각 Command의 `Validator` | FluentValidation + `MustSatisfyValidation` |

### 핵심 스니펫

**guard를 활용한 중복 검사** — LINQ 체인 안에서 선언적 조건 검사:

```csharp
using static Functorium.Applications.Errors.ApplicationErrorType;

FinT<IO, Response> usecase =
    from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
    from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
        new AlreadyExists(),
        request.Name,
        $"Product name already exists: '{request.Name}'"))
    from created in _productRepository.Create(product)
    select new Response(created.Id.ToString(), created.Name, created.Price, created.CreatedAt);
```

**Query 유스케이스** — Port를 통한 조회, LINQ 체이닝으로 DTO 매핑:

```csharp
public sealed class GetProductByIdQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;
    public sealed record Response(string ProductId, string Name, decimal Price, DateTime CreatedAt);

    public sealed class Usecase(IProductDetailQuery productDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductDetailQuery _productDetailQuery = productDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);
            FinT<IO, Response> usecase =
                from result in _productDetailQuery.GetById(productId)
                select new Response(result.ProductId, result.Name, result.Price, result.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

## 예제 3: 고급 — EventHandler + Domain Service 연동

예제 2에 도메인 이벤트 핸들러와 Domain Service를 추가합니다. 주문 생성 시 신용 한도 검증(Domain Service), 주문 생성 이벤트 핸들러를 통한 재고 차감, 주문 취소 시 재고 복원까지 교차 Aggregate 흐름을 구현합니다.

### 프롬프트

```text
/application-develop 주문 생성 시 재고 차감 EventHandler, 신용한도 초과 시 Domain Service 검증,
주문 취소 시 재고 복원까지 구현해줘.
```

### 기대 결과

| 산출물 | 타입 | 설명 |
|--------|------|------|
| Command | `CreateOrderWithCreditCheckCommand` | Domain Service 연동 주문 생성 |
| EventHandler | `OnOrderCreated` | `IDomainEventHandler<Order.CreatedEvent>`, 재고 차감 |
| EventHandler | `OnOrderCancelled` | `IDomainEventHandler<Order.CancelledEvent>`, 재고 복원 |
| Domain Service | `OrderCreditCheckService` | 교차 Aggregate 신용 한도 검증 |

### 핵심 스니펫

**Domain Service 연동 Command** — LINQ 체인에서 Domain Service를 자연스럽게 합성:

```csharp
public sealed class Usecase(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IProductCatalog productCatalog)
    : ICommandUsecase<Request, Response>
{
    private readonly OrderCreditCheckService _creditCheckService = new();

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ... Value Object 생성, OrderLine 구성 생략 ...

        // Domain Service를 LINQ 체인에서 합성
        FinT<IO, Response> usecase =
            from customer in _customerRepository.GetById(customerId)
            from _ in _creditCheckService.ValidateCreditLimit(customer, newOrder.TotalAmount)
            from saved in _orderRepository.Create(newOrder)
            select new Response(
                saved.Id.ToString(),
                Seq(saved.OrderLines.Select(l => new OrderLineResponse(...))),
                saved.TotalAmount,
                saved.CreatedAt);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

**도메인 이벤트 핸들러** — `IDomainEventHandler<TEvent>` 구현, 교차 Aggregate 부수 효과:

```csharp
public sealed class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
{
    private readonly IInventoryRepository _inventoryRepository;

    public OnOrderCreated(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async ValueTask Handle(Order.CreatedEvent notification, CancellationToken cancellationToken)
    {
        // 주문 라인별 재고 차감
        foreach (var line in notification.OrderLines)
        {
            var inventory = await _inventoryRepository.GetByProductId(line.ProductId).Run().RunAsync();
            if (inventory.IsSucc)
            {
                var inv = inventory.ThrowIfFail();
                inv.Deduct(line.Quantity, DateTime.UtcNow);
                await _inventoryRepository.Update(inv).Run().RunAsync();
            }
        }
    }
}
```

## 참고 자료

### 프레임워크 가이드

- [Use Case와 CQRS](../guides/application/11-usecases-and-cqrs/)
- [DTO 전략](../guides/application/17-dto-strategy/)
- [Port 정의](../guides/adapter/12-ports/)
- [에러 시스템](../guides/domain/08a-error-system/)
- [도메인 이벤트](../guides/domain/07-domain-events/)
- [도메인 서비스](../guides/domain/09-domain-services/)
- [단위 테스트](../guides/testing/15a-unit-testing/)

### 관련 스킬

- [도메인 개발 스킬](./domain-develop/) — Aggregate, Value Object, Event 등 도메인 빌딩블록 생성
- [Adapter 레이어 개발 스킬](./adapter-develop/) — Repository, Query Adapter, Endpoint, DI 등록 생성
- [테스트 개발 스킬](./test-develop/) — 단위/통합/아키텍처 테스트 생성
