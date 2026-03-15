---
title: "애플리케이션 구현 결과"
---

[비즈니스 요구사항](../00-business-requirements/)에서 정의한 워크플로우 시나리오가, [타입 설계 의사결정](../01-type-design-decisions/)과 [코드 설계](../02-code-design/)의 패턴으로 실제 동작함을 증명합니다. 각 테스트는 NSubstitute로 Port를 Mock하고, `FinTFactory`로 IO 효과를 시뮬레이션합니다. 정상 시나리오는 병렬 검증, 배치 조회, 읽기/쓰기 분리가 올바르게 동작하는지, 거부 시나리오는 검증 실패와 에러 전파가 제대로 이루어지는지 확인합니다.

## 정상 시나리오

### 시나리오 1: CreateProduct -- Apply 패턴 + 고유성 검사

상품 생성 시 모든 Value Object 검증을 Apply 패턴으로 병렬 수행한 뒤, 이름 고유성 검사와 Inventory 생성까지 하나의 `FinT<IO, T>` 파이프라인으로 처리합니다.

```csharp
[Fact]
public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
{
    // Arrange
    var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

    _productRepository.Exists(Arg.Any<Specification<Product>>())
        .Returns(FinTFactory.Succ(false));
    _productRepository.Create(Arg.Any<Product>())
        .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
    _inventoryRepository.Create(Arg.Any<Inventory>())
        .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
    actual.ThrowIfFail().Name.ShouldBe("Test Product");
    actual.ThrowIfFail().Price.ShouldBe(100m);
}
```

**Apply 패턴의 동작 원리.** Usecase 내부에서는 `Validation<Error, T>` 타입의 병렬 검증이 이루어집니다.

```csharp
private static Fin<ProductData> CreateProductData(Request request)
{
    // 모든 필드: VO Validate() 사용 (Validation<Error, T> 반환)
    var name = ProductName.Validate(request.Name);
    var description = ProductDescription.Validate(request.Description);
    var price = Money.Validate(request.Price);
    var stockQuantity = Quantity.Validate(request.StockQuantity);

    // 모두 튜플로 병합 - Apply로 병렬 검증
    return (name, description, price, stockQuantity)
        .Apply((n, d, p, s) =>
            new ProductData(
                Product.Create(
                    ProductName.Create(n).ThrowIfFail(),
                    ProductDescription.Create(d).ThrowIfFail(),
                    Money.Create(p).ThrowIfFail()),
                Quantity.Create(s).ThrowIfFail()))
        .As()
        .ToFin();
}
```

4개의 `Validate()` 호출이 모두 `Validation<Error, T>`를 반환하므로, 하나라도 실패하면 **모든 에러가 누적**됩니다. `Bind`(순차 실행)가 아닌 `Apply`(병렬 검증)이기 때문에 첫 번째 에러에서 중단되지 않습니다.

검증 통과 후에는 `FinT<IO, T>` LINQ 합성으로 고유성 검사 -> 저장 -> Inventory 생성을 순차 실행합니다.

```csharp
FinT<IO, Response> usecase =
    from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
    from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
        new AlreadyExists(),
        request.Name,
        $"Product name already exists: '{request.Name}'"))
    from createdProduct in _productRepository.Create(product)
    from createdInventory in _inventoryRepository.Create(
        Inventory.Create(createdProduct.Id, stockQuantity))
    select new Response(
        createdProduct.Id.ToString(),
        createdProduct.Name,
        createdProduct.Description,
        createdProduct.Price,
        createdInventory.StockQuantity,
        createdProduct.CreatedAt);
```

이 테스트가 증명하는 것은 다음과 같습니다. Repository가 성공을 반환하도록 설정하여 Usecase 로직만 격리 테스트합니다. `IsSucc`가 true이므로 Apply 패턴 검증, 고유성 검사, 저장이 모두 성공했음을 증명합니다. 검증 → 중복 확인 → 저장 → Inventory 생성이라는 4단계 파이프라인이 하나의 FinT 체인으로 안전하게 합성되었습니다.

### 시나리오 2: CreateCustomer -- 이메일 고유성

고객 생성 시 `CustomerName`, `Email`, `Money`(CreditLimit)를 Apply 패턴으로 병렬 검증한 뒤, `CustomerEmailSpec`으로 이메일 중복을 검사합니다.

```csharp
[Fact]
public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
{
    // Arrange
    var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

    _customerRepository.Exists(Arg.Any<Specification<Customer>>())
        .Returns(FinTFactory.Succ(false));
    _customerRepository.Create(Arg.Any<Customer>())
        .Returns(call => FinTFactory.Succ(call.Arg<Customer>()));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
    actual.ThrowIfFail().Name.ShouldBe("John");
    actual.ThrowIfFail().Email.ShouldBe("john@example.com");
}
```

Mock 설정 패턴이 CreateProduct와 동일합니다. `Exists(Specification)`은 `false`를 반환하여 중복이 없음을 표현하고, `Create()`는 전달받은 엔티티를 그대로 반환합니다.

Mock 설정 패턴이 CreateProduct와 동일한 것은 우연이 아닙니다. 모든 Command Usecase가 동일한 `Exists → guard → Create` 파이프라인 구조를 따르기 때문입니다. 이 일관성 덕분에 새로운 고유성 검사가 필요한 Use Case를 추가할 때 기존 패턴을 그대로 적용할 수 있습니다.

### 시나리오 3: CreateOrderWithCreditCheck -- 배치 조회 + 신용한도

주문 생성 시 `IProductCatalog.GetPricesForProducts()`로 상품 가격을 **단일 라운드트립**으로 배치 조회하고, `OrderCreditCheckService`로 신용 한도를 검증합니다.

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenCreditLimitIsSufficient()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 5000m);
    var productId = ProductId.New();
    var request = new CreateOrderWithCreditCheckCommand.Request(
        customer.Id.ToString(),
        Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(productId.ToString(), 2)),
        "Seoul, Korea");

    _customerRepository.GetById(Arg.Any<CustomerId>())
        .Returns(FinTFactory.Succ(customer));
    _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
        .Returns(call =>
        {
            var ids = call.Arg<IReadOnlyList<ProductId>>();
            var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
            return FinTFactory.Succ(prices);
        });
    _orderRepository.Create(Arg.Any<Order>())
        .Returns(call => FinTFactory.Succ(call.Arg<Order>()));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
    actual.ThrowIfFail().TotalAmount.ShouldBe(2000m);
}
```

`IProductCatalog` Mock은 `call.Arg<IReadOnlyList<ProductId>>()`로 전달된 상품 ID 목록을 받아 각각에 가격을 매핑하여 반환합니다. 단가 1000원 x 수량 2 = 총액 2000원이며, 고객 신용한도 5000원 이내이므로 성공합니다.

`CreateSampleCustomer` 헬퍼는 도메인 VO를 직접 조합하여 테스트 엔티티를 생성합니다.

```csharp
private static Customer CreateSampleCustomer(decimal creditLimit = 5000m)
{
    return Customer.Create(
        CustomerName.Create("John").ThrowIfFail(),
        Email.Create("john@example.com").ThrowIfFail(),
        Money.Create(creditLimit).ThrowIfFail());
}
```

이 테스트는 Application 레이어의 가장 복잡한 Use Case를 검증합니다. 배치 가격 조회(`IProductCatalog`), 교차 Aggregate 검증(`OrderCreditCheckService`), 다중 Repository 조율이 하나의 FinT 파이프라인에서 올바르게 작동하는지 확인합니다.

### 시나리오 4: SearchProducts -- Specification 합성 + 페이지네이션

Query는 `IProductQuery` Read Adapter를 통해 Aggregate 재구성 없이 DTO로 직접 프로젝션합니다. Specification 합성과 페이지네이션을 조합합니다.

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
{
    // Arrange
    var pagedResult = CreateSamplePagedResult();
    var request = new SearchProductsQuery.Request();

    _readAdapter.Search(
            Arg.Any<Specification<Product>>(),
            Arg.Any<PageRequest>(),
            Arg.Any<SortExpression>())
        .Returns(FinTFactory.Succ(pagedResult));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
    actual.ThrowIfFail().Products.Count.ShouldBe(3);
    actual.ThrowIfFail().TotalCount.ShouldBe(3);
}
```

페이지네이션 메타데이터 검증 테스트는 `PagedResult`의 연산 결과를 확인합니다.

```csharp
[Fact]
public async Task Handle_ReturnsPaginationMetadata_WhenPageProvided()
{
    // Arrange
    List<ProductSummaryDto> items = [new(ProductId.New().ToString(), "Item", 100m)];
    var pagedResult = new PagedResult<ProductSummaryDto>(items, 50, 2, 10);
    var request = new SearchProductsQuery.Request(Page: 2, PageSize: 10);

    _readAdapter.Search(
            Arg.Any<Specification<Product>>(),
            Arg.Any<PageRequest>(),
            Arg.Any<SortExpression>())
        .Returns(FinTFactory.Succ(pagedResult));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
    var response = actual.ThrowIfFail();
    response.Page.ShouldBe(2);
    response.PageSize.ShouldBe(10);
    response.TotalCount.ShouldBe(50);
    response.TotalPages.ShouldBe(5);
    response.HasPreviousPage.ShouldBeTrue();
    response.HasNextPage.ShouldBeTrue();
}
```

Usecase 내부에서는 `Specification<Product>.All`을 기본으로 하여 요청 파라미터에 따라 `ProductNameSpec`, `ProductPriceRangeSpec`을 `&=`로 합성합니다.

```csharp
private static Specification<Product> BuildSpecification(Request request)
{
    var spec = Specification<Product>.All;

    if (request.Name.Length > 0)
        spec &= new ProductNameSpec(
            ProductName.Create(request.Name).ThrowIfFail());

    if (request.MinPrice > 0 && request.MaxPrice > 0)
        spec &= new ProductPriceRangeSpec(
            Money.Create(request.MinPrice).ThrowIfFail(),
            Money.Create(request.MaxPrice).ThrowIfFail());

    return spec;
}
```

정상 시나리오에서 각 패턴이 올바르게 작동함을 확인했습니다. 이제 거부 시나리오에서 에러가 어떻게 발생하고 전파되는지 살펴봅니다. Application 레이어의 에러 처리는 try-catch가 아닌 타입 시스템(`Fin<T>`, `FinT<IO, T>`)에 의해 자동으로 이루어집니다.

## 거부 시나리오

### 시나리오 5: 다중 VO 검증 실패 (에러 누적)

Apply 패턴은 개별 VO 검증을 병렬로 수행하므로, 여러 필드가 동시에 실패하면 **모든 에러가 누적**됩니다. 빈 이름과 0원 가격을 동시에 전달하면 두 에러가 한번에 반환됩니다.

```csharp
[Fact]
public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
{
    // Arrange
    var request = new CreateProductCommand.Request("", "Description", 100m, 10);

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}

[Fact]
public async Task Handle_ShouldReturnFailure_WhenPriceIsZero()
{
    // Arrange
    var request = new CreateProductCommand.Request("Test Product", "Description", 0m, 10);

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}
```

VO 검증 실패 시 Repository Mock 설정이 필요하지 않습니다. Apply 패턴이 `CreateProductData()` 단계에서 즉시 `Fail`을 반환하므로 IO 효과가 실행되지 않습니다. 이것이 "검증 실패 시 조기 반환"의 핵심입니다.

이것이 Apply 패턴과 Bind 패턴의 실질적 차이입니다. Bind였다면 빈 이름에서 즉시 중단되어 가격 오류는 보고되지 않았을 것입니다. Apply는 독립적인 검증을 모두 실행하여 사용자가 한 번의 요청으로 모든 문제를 파악할 수 있게 합니다.

### 시나리오 6: AlreadyExists (중복 이름/이메일)

`Specification` 기반 고유성 검사에서 중복이 발견되면 `ApplicationError.For<T>(new AlreadyExists(), ...)`를 반환합니다.

```csharp
[Fact]
public async Task Handle_ShouldReturnFailure_WhenDuplicateName()
{
    // Arrange
    var request = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);

    _productRepository.Exists(Arg.Any<Specification<Product>>())
        .Returns(FinTFactory.Succ(true));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}
```

Customer의 이메일 중복도 동일한 패턴입니다.

```csharp
[Fact]
public async Task Handle_ShouldReturnFailure_WhenDuplicateEmail()
{
    // Arrange
    var request = new CreateCustomerCommand.Request("John", "john@example.com", 5000m);

    _customerRepository.Exists(Arg.Any<Specification<Customer>>())
        .Returns(FinTFactory.Succ(true));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}
```

`Exists()`가 `true`를 반환하면 `guard(!exists, ...)` 에서 실패하여 `AlreadyExists` 에러가 전파됩니다. `Create()` Mock은 설정하지 않아도 됩니다 -- `guard`에서 이미 중단되었기 때문입니다.

`guard`에서 중단되었으므로 `Create()` Mock을 설정하지 않아도 테스트가 통과합니다. 이는 FinT 체인의 단락(short-circuit) 특성을 증명합니다 — 실패 이후의 모든 IO 연산이 실행되지 않습니다.

### 시나리오 7: NotFound (존재하지 않는 상품)

**UpdateProduct** -- `GetById()`가 `Fail`을 반환하면 LINQ 합성의 첫 `from`에서 즉시 중단됩니다.

```csharp
[Fact]
public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
{
    // Arrange
    var request = new UpdateProductCommand.Request(
        ProductId.New().ToString(), "Updated", "Desc", 200m);

    _productRepository.GetById(Arg.Any<ProductId>())
        .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}
```

**DeleteProduct** -- `GetByIdIncludingDeleted()`가 `Fail`을 반환하는 경우입니다.

```csharp
[Fact]
public async Task Handle_ReturnsFail_WhenProductNotFound()
{
    // Arrange
    var request = new DeleteProductCommand.Request(
        ProductId.New().ToString(), "admin");

    _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
        .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}
```

`FinTFactory.Fail<T>(Error.New(...))` 패턴으로 Repository가 실패를 반환하면, `FinT<IO, T>` LINQ 합성의 모나드 바인딩에 의해 후속 단계가 실행되지 않고 에러가 그대로 전파됩니다.

### 시나리오 8: 도메인 에러 전파 (CreditLimitExceeded)

`OrderCreditCheckService`의 `CreditLimitExceeded` 도메인 에러가 Application 레이어까지 전파됩니다.

```csharp
[Fact]
public async Task Handle_ReturnsFail_WhenCreditLimitExceeded()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 1000m);
    var productId = ProductId.New();
    var request = new CreateOrderWithCreditCheckCommand.Request(
        customer.Id.ToString(),
        Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(productId.ToString(), 2)),
        "Seoul, Korea");

    _customerRepository.GetById(Arg.Any<CustomerId>())
        .Returns(FinTFactory.Succ(customer));
    _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
        .Returns(call =>
        {
            var ids = call.Arg<IReadOnlyList<ProductId>>();
            var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
            return FinTFactory.Succ(prices);
        });

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeFalse();
}
```

단가 1000원 x 수량 2 = 총액 2000원인데 신용한도가 1000원이므로, Domain Service에서 `CreditLimitExceeded` 에러를 반환합니다. 이 에러는 `FinT<IO, T>` LINQ 합성 내부의 `_creditCheckService.ValidateCreditLimit(customer, newOrder.TotalAmount)`에서 발생하여, `_orderRepository.Create()`가 실행되지 않고 에러가 상위로 전파됩니다.

에러는 발생 지점에서 FinT 체인을 따라 최종 `FinResponse`까지 자동으로 전파됩니다. 중간에 try-catch가 없어도 타입 시스템이 에러 흐름을 보장합니다. 다음 표는 각 에러 유형별 발생 원천과 전파 경로를 정리합니다.

## 에러 전파 경로

| 에러 원천 | 에러 타입 | 전파 경로 | 최종 ApplicationError |
|-----------|-----------|-----------|----------------------|
| VO 검증 (`ProductName.Validate` 등) | `Validation<Error, T>` | `Apply()` -> `.ToFin()` -> 조기 반환 | `FinResponse.Fail<T>(error)` |
| 고유성 검사 (`Exists` + `guard`) | `ApplicationError` (`AlreadyExists`) | `FinT<IO, T>` LINQ 합성 내 `guard` | `ApplicationError.For<T>(new AlreadyExists(), ...)` |
| 엔티티 조회 (`GetById`) | `Error` (Repository 반환) | `FinT<IO, T>` LINQ 합성 모나드 바인딩 | `FinTFactory.Fail<T>(Error.New(...))` |
| 도메인 서비스 (`OrderCreditCheckService`) | `DomainError` (`CreditLimitExceeded`) | `FinT<IO, T>` LINQ 합성 -> `Fin.ToFinResponse()` | `DomainError.For<T>(new CreditLimitExceeded(), ...)` |
| 배치 조회 후 존재 검증 | `ApplicationError` (`NotFound`) | 명시적 반환 | `ApplicationError.For<T>(new NotFound(), ...)` |

## 시나리오 커버리지 매트릭스

| Use Case | 정상 | 실패 | 검증 방법 |
|----------|------|------|-----------|
| `CreateProductCommand` | 상품 + Inventory 생성 | VO 검증 실패, 중복 이름 | Apply 패턴 에러 누적, `guard` + `AlreadyExists` |
| `CreateCustomerCommand` | 고객 생성 | VO 검증 실패, 중복 이메일 | Apply 패턴 에러 누적, `guard` + `AlreadyExists` |
| `CreateOrderCommand` | 주문 생성 | 상품 미존재, 배송지 빈값 | 배치 조회 후 `NotFound`, VO 조기 반환 |
| `CreateOrderWithCreditCheckCommand` | 주문 + 신용 검증 | 신용한도 초과, 상품 미존재, 고객 미존재 | Domain Service 에러 전파, `NotFound`, Repository `Fail` |
| `UpdateProductCommand` | 상품 업데이트 | 상품 미존재, 중복 이름, VO 검증 실패 | Repository `Fail`, `guard` + `AlreadyExists`, Apply 패턴 |
| `DeleteProductCommand` | Soft Delete (이미 삭제 포함) | 상품 미존재 | Repository `Fail` |
| `SearchProductsQuery` | 필터 없음, 이름, 가격 범위, 복합 필터, 페이지네이션 | Validator 에러 (가격 범위 불완전, 잘못된 정렬) | Specification 합성, `PagedResult` 메타데이터 |

지금까지 개별 시나리오를 통해 Application 레이어의 동작을 검증했습니다. 이제 CQRS, Apply 패턴, FinT 모나드, Port/Adapter가 함께 작동하여 어떤 가치를 제공하는지 종합적으로 정리합니다.

## CQRS + Apply 패턴의 가치 요약

**Command/Query 독립 최적화.** Command는 `IProductRepository`(Write Port)를 통해 Aggregate를 재구성하고 도메인 불변식을 검증합니다. Query는 `IProductQuery`(Read Port)를 통해 Aggregate 재구성 없이 DTO로 직접 프로젝션하여 불필요한 객체 생성 비용을 제거합니다.

**병렬 검증으로 사용자 경험 개선.** `Validation<Error, T>` + Apply 패턴은 모든 VO 검증을 병렬 수행하여 에러를 누적합니다. 사용자는 한 번의 요청으로 모든 입력 오류를 확인할 수 있습니다. Bind 기반 순차 검증이었다면 첫 번째 에러에서 중단되어 여러 번 재시도가 필요했을 것입니다.

**`FinT<IO, T>`로 안전한 에러 전파.** Repository 실패, `guard` 실패, Domain Service 에러 모두 `FinT<IO, T>` 모나드 바인딩에 의해 자동으로 전파됩니다. `try-catch` 없이 LINQ 합성만으로 에러 흐름이 제어되며, 실패 시 후속 IO 효과가 실행되지 않는 것이 타입 시스템에 의해 보장됩니다.

**Port/Adapter로 테스트 용이성.** 모든 외부 의존성이 Port 인터페이스(`IProductRepository`, `IProductCatalog`, `IProductQuery`)로 추상화되어 있으므로, NSubstitute로 Mock을 생성하고 `FinTFactory.Succ/Fail`로 성공/실패를 시뮬레이션할 수 있습니다. Domain Service(`OrderCreditCheckService`)는 순수 로직이므로 Mock 없이 실제 인스턴스를 사용합니다.

## 아키텍처 테스트

Application 레이어의 구조적 규칙을 Functorium의 `ArchitectureRules` 프레임워크로 자동 검증합니다.

| 테스트 클래스 | 검증 대상 | 핵심 규칙 |
|-------------|----------|----------|
| `UsecaseArchitectureRuleTests` | 10 Commands + 9 Queries | 중첩 `Usecase` 클래스 (sealed, `ICommandUsecase`/`IQueryUsecase`), 선택적 `Validator` 클래스 (sealed, `AbstractValidator`) |
| `LayerDependencyArchitectureRuleTests` | Domain ↛ Application | Domain 레이어가 Application 레이어에 의존하지 않음 |
