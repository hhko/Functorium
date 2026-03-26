# Functorium 유스케이스 패턴 상세

Command, Query, EventHandler의 구현 패턴과 전체 코드 예제입니다.

---

## 1. Command 패턴 — CreateProductCommand

상품 생성 Command의 전체 구조입니다. 하나의 sealed class 안에 Request, Response, Validator, Usecase를 중첩합니다.

### Request + Response

```csharp
public sealed class CreateProductCommand
{
    /// Command Request — ICommandRequest<Response> 구현
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    /// Command Response — 성공 시 반환 DTO
    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);
}
```

### Validator

FluentValidation으로 Presentation Layer 검증을 수행합니다.
`MustSatisfyValidation`으로 VO의 `Validate()` 메서드를 재사용합니다.

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
        RuleFor(x => x.Description).MustSatisfyValidation(ProductDescription.Validate);
        RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
        RuleFor(x => x.StockQuantity).MustSatisfyValidation(Quantity.Validate);
    }
}
```

### MustSatisfyValidation 사용법

| 메서드 | 용도 | 예시 |
|--------|------|------|
| `MustSatisfyValidation` | 입력/출력 타입 동일 | `decimal` -> `Validation<Error, decimal>` |
| `MustSatisfyValidationOf<T>` | 입력/출력 타입 다름 | `string` -> `Validation<Error, ProductName>` |

입력 타입과 VO의 Validate 반환 타입이 같으면 `MustSatisfyValidation`,
다르면 `MustSatisfyValidationOf<TValueObject>`를 사용합니다.

```csharp
// decimal -> Validation<Error, decimal> : 타입 동일
RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);

// string -> Validation<Error, string> : 타입 동일
RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
```

### Usecase — ApplyT 패턴 + FinT LINQ 합성

```csharp
public sealed class Usecase(
    IProductRepository productRepository,
    IInventoryRepository inventoryRepository)
    : ICommandUsecase<Request, Response>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
        FinT<IO, Response> usecase =
            from vos in (
                ProductName.Create(request.Name),
                ProductDescription.Create(request.Description),
                Money.Create(request.Price),
                Quantity.Create(request.StockQuantity)
            ).ApplyT((name, desc, price, qty) => (Name: name, Desc: desc, Price: price, Qty: qty))
            let product = Product.Create(vos.Name, vos.Desc, vos.Price)
            from exists in _productRepository.Exists(new ProductNameUniqueSpec(vos.Name))
            from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(),
                request.Name,
                $"Product name already exists: '{request.Name}'"))
            from createdProduct in _productRepository.Create(product)
            from createdInventory in _inventoryRepository.Create(
                Inventory.Create(createdProduct.Id, vos.Qty))
            select new Response(
                createdProduct.Id.ToString(),
                createdProduct.Name,
                createdProduct.Description,
                createdProduct.Price,
                createdInventory.StockQuantity,
                createdProduct.CreatedAt);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

**ApplyT 패턴의 이점:**
- `Create()` 호출이 VO 생성 + 도메인 검증 + 정규화를 한번에 처리
- applicative 합성으로 모든 VO 에러를 병렬 수집
- `FinT<IO, R>` 리프팅이 자동 — 별도의 `Unwrap()`, 조기 반환, 임시 레코드 불필요
- Presentation Validator가 이미 검증했더라도, Handler의 `Create()` 호출이 **도메인 검증의 권위적 지점이다**

---

## 2. Query 패턴 — GetProductByIdQuery

단건 조회 Query입니다. Read Port (`IProductDetailQuery`)를 통해 Aggregate 재구성 없이 DTO 직접 프로젝션합니다.

```csharp
public sealed class GetProductByIdQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;

    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        DateTime CreatedAt,
        Option<DateTime> UpdatedAt);

    public sealed class Usecase(IProductDetailQuery productDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductDetailQuery _productDetailQuery = productDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);
            FinT<IO, Response> usecase =
                from result in _productDetailQuery.GetById(productId)
                select new Response(
                    result.ProductId,
                    result.Name,
                    result.Description,
                    result.Price,
                    result.CreatedAt,
                    result.UpdatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

---

## 3. 검색 Query 패턴 — SearchProductsQuery

Specification 조합 + 페이지네이션/정렬 패턴입니다.

```csharp
public sealed class SearchProductsQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price"];

    public sealed record Request(
        string Name = "",
        decimal MinPrice = 0,
        decimal MaxPrice = 0,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    public sealed record Response(
        IReadOnlyList<ProductSummaryDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .MustSatisfyValidation(ProductName.Validate)
                .When(x => x.Name.Length > 0);

            RuleFor(x => x.SortBy)
                .Must(sortBy => AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                .When(x => x.SortBy.Length > 0)
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}");

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>()
                .When(x => x.SortDirection.Length > 0);
        }
    }

    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = BuildSortExpression(request);

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(spec, pageRequest, sortExpression)
                select new Response(
                    result.Items, result.TotalCount, result.Page,
                    result.PageSize, result.TotalPages,
                    result.HasNextPage, result.HasPreviousPage);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Specification<Product> BuildSpecification(Request request)
        {
            var spec = Specification<Product>.All;

            if (request.Name.Length > 0)
                spec &= new ProductNameSpec(ProductName.Create(request.Name).Unwrap());

            if (request.MinPrice > 0 && request.MaxPrice > 0)
                spec &= new ProductPriceRangeSpec(
                    Money.Create(request.MinPrice).Unwrap(),
                    Money.Create(request.MaxPrice).Unwrap());

            return spec;
        }

        private static SortExpression BuildSortExpression(Request request)
        {
            if (request.SortBy.Length == 0)
                return SortExpression.Empty;

            return SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));
        }
    }
}
```

---

## 4. EventHandler 패턴 — ProductCreatedEvent

도메인 이벤트에 반응하는 핸들러입니다. `IDomainEventHandler<T.Event>`를 구현합니다.

```csharp
public sealed class ProductCreatedEvent : IDomainEventHandler<Product.CreatedEvent>
{
    private readonly ILogger<ProductCreatedEvent> _logger;

    public ProductCreatedEvent(ILogger<ProductCreatedEvent> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
            notification.ProductId,
            notification.Name,
            notification.Price);

        return ValueTask.CompletedTask;
    }
}
```

### EventHandler 명명 규칙

- 클래스명: `{Aggregate}{Event}` (예: `ProductCreatedEvent`, `OrderCreatedEvent`)
- 이벤트 타입: `{Aggregate}.{Event}` (예: `Product.CreatedEvent`, `Order.CreatedEvent`)
- 여러 핸들러가 같은 이벤트를 처리할 수 있음

---

## 5. Apply 패턴 — 병렬 검증

여러 VO를 동시에 검증하고 모든 에러를 한 번에 수집합니다.

### 기본 패턴

```csharp
private static Fin<UpdateData> CreateUpdateData(Request request)
{
    // 1. 각 VO의 Validate() 호출 (Validation<Error, T> 반환)
    var name = ProductName.Validate(request.Name);
    var description = ProductDescription.Validate(request.Description);
    var price = Money.Validate(request.Price);

    // 2. 튜플 Apply로 병렬 검증 병합
    return (name, description, price)
        .Apply((n, d, p) =>
            new UpdateData(
                ProductName.Create(n).Unwrap(),
                ProductDescription.Create(d).Unwrap(),
                Money.Create(p).Unwrap()))
        .As()
        .ToFin();
}
```

### 흐름 설명

1. `Validate()` -> `Validation<Error, T>` (검증만, 객체 생성 안 함)
2. `.Apply()` -> 모든 검증을 병렬로 수행, 에러 누적
3. `.As().ToFin()` -> `Validation` -> `Fin` 변환
4. Apply 콜백 내에서 `Create().Unwrap()` -> 이미 검증된 값이므로 안전

---

## 6. guard() 패턴 — 조건부 단락

LINQ 체인 내에서 비즈니스 조건을 검증하고 실패 시 체인을 중단합니다.

```csharp
FinT<IO, Response> usecase =
    from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
    from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
        new AlreadyExists(),
        request.Name,
        $"Product name already exists: '{request.Name}'"))
    from createdProduct in _productRepository.Create(product)
    select new Response(...);
```

### guard 사용 패턴

| 패턴 | 코드 |
|------|------|
| 존재 여부 검증 | `guard(!exists, ApplicationError.For<T>(new AlreadyExists(), ...))` |
| 상태 검증 | `guard(order.Status == "Pending", ApplicationError.For<T>(new InvalidState(), ...))` |
| 권한 검증 | `guard(user.HasPermission("admin"), ApplicationError.For<T>(new Forbidden(), ...))` |

---

## 7. FinT -> FinResponse 변환 패턴

모든 Usecase의 마지막 단계입니다.

```csharp
// 1. FinT LINQ 합성으로 usecase 정의
FinT<IO, Response> usecase =
    from product in _productRepository.GetById(productId)
    select new Response(...);

// 2. IO 실행 -> Fin<T> 추출
Fin<Response> response = await usecase.Run().RunAsync();

// 3. Fin -> FinResponse 변환 (Mediator 반환 타입)
return response.ToFinResponse();
```

### 조기 반환 (Apply 검증 실패)

```csharp
var createData = CreateProductData(request);

if (createData.IsFail)
{
    return createData.Match(
        Succ: _ => throw new InvalidOperationException(),
        Fail: error => FinResponse.Fail<Response>(error));
}
```

---

## 8. Delete Command — let 바인딩 패턴

도메인 메서드 호출 후 Repository 저장이 필요한 패턴입니다.
`let`은 LINQ 내에서 순수 변환(IO 없음)에 사용합니다.

```csharp
public sealed class Usecase(IProductRepository productRepository)
    : ICommandUsecase<Request, Response>
{
    private readonly IProductRepository _productRepository = productRepository;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var productId = ProductId.Create(request.ProductId);

        FinT<IO, Response> usecase =
            from product in _productRepository.GetByIdIncludingDeleted(productId)
            let deleted = product.Delete(request.DeletedBy)     // 순수 변환 (IO 아님)
            from updated in _productRepository.Update(deleted)  // IO 작업
            select new Response(updated.Id.ToString());

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### from vs let

| 키워드 | 용도 | 예시 |
|--------|------|------|
| `from x in` | IO 작업 (`FinT<IO, T>` 반환) | `from product in repo.GetById(id)` |
| `let x =` | 순수 변환 (IO 아님) | `let deleted = product.Delete(by)` |
| `from _ in guard()` | 조건부 단락 | `from _ in guard(!exists, error)` |
| `from x in fin` | `Fin<T>` -> `FinT` 승격 | `from updated in product.Update(...)` |

---

## 9. ApplicationError 사용법

```csharp
using static Functorium.Applications.Errors.ApplicationErrorType;

// 표준 에러 타입
ApplicationError.For<CreateProductCommand>(new AlreadyExists(), productId, "message");
ApplicationError.For<CreateProductCommand>(new NotFound(), productId, "message");
ApplicationError.For<CreateProductCommand>(new ValidationFailed("Name"), value, "message");

// 커스텀 에러 (Usecase 내부 정의)
public sealed record CannotProcess : ApplicationErrorType.Custom;
ApplicationError.For<MyCommand>(new CannotProcess(), value, "message");
```

### 에러 코드 형식

`ApplicationErrors.{UsecaseName}.{ErrorName}`

예시:
- `ApplicationErrors.CreateProductCommand.AlreadyExists`
- `ApplicationErrors.UpdateOrderCommand.NotFound`
- `ApplicationErrors.DeleteOrderCommand.CannotProcess`

---

## 10. 인터페이스 요약

| 인터페이스 | 역할 | 반환 타입 |
|-----------|------|-----------|
| `ICommandRequest<TSuccess>` | Command 요청 | - |
| `ICommandUsecase<TCommand, TSuccess>` | Command 핸들러 | `ValueTask<FinResponse<TSuccess>>` |
| `IQueryRequest<TSuccess>` | Query 요청 | - |
| `IQueryUsecase<TQuery, TSuccess>` | Query 핸들러 | `ValueTask<FinResponse<TSuccess>>` |
| `IDomainEventHandler<TEvent>` | 이벤트 핸들러 | `ValueTask` |

모든 Usecase 핸들러 메서드 시그니처:
```csharp
public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
```

---

## Domain Service 벌크 패턴 예제

```csharp
public sealed class BulkDeleteProductsCommand
{
    public sealed record Request(List<string> ProductIds) : ICommandRequest<Response>;
    public sealed record Response(int AffectedCount);

    public sealed class Usecase(
        IProductRepository productRepository,
        IDomainEventCollector eventCollector)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
        {
            var ids = request.ProductIds.Select(id => ProductId.Parse(id, null)).ToList();

            FinT<IO, Response> usecase =
                from products in productRepository.GetByIds(ids)
                let bulkResult = BulkDeleteAndTrack(products)
                from saved in productRepository.UpdateRange(bulkResult.Deleted.ToList())
                select new Response(bulkResult.Deleted.Count);

            return (await usecase.Run().RunAsync()).ToFinResponse();
        }

        private (Seq<Product> Deleted, ProductBulkOperations.BulkDeletedEvent Event)
            BulkDeleteAndTrack(Seq<Product> products)
        {
            var result = ProductBulkOperations.BulkDelete(products.ToList(), "system");
            eventCollector.TrackEvent(result.Event);
            return result;
        }
    }
}
```

---

## IUsecaseCtxEnricher 구현 예제

```csharp
public interface IUsecaseCtxEnricher<in TRequest, in TResponse>
    where TResponse : IFinResponse
{
    IDisposable? EnrichRequest(TRequest request);
    IDisposable? EnrichResponse(TRequest request, TResponse response);
}
```

Source Generator가 Request/Response record의 공개 프로퍼티를 자동 감지하여
`IUsecaseCtxEnricher` 구현체를 생성합니다.
`[CtxRoot]`로 루트 승격, `[CtxTarget]`으로 Pillar 타겟팅, `[CtxIgnore]`로 제외합니다.
