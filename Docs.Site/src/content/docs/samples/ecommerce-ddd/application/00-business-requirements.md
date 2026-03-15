---
title: "애플리케이션 비즈니스 요구사항"
---

## 배경

Application 레이어는 도메인 로직을 조율하는 얇은 오케스트레이션 계층입니다. Domain 레이어가 정의한 비즈니스 규칙과 불변식을 활용하여 Use Case를 구현하며, 스스로 비즈니스 로직을 포함하지 않습니다. 핵심 책임은 다음과 같습니다.

- **CQRS 분리:** Command(상태 변경)와 Query(데이터 조회)를 분리하여 각 경로를 독립적으로 최적화합니다.
- **이중 검증 전략:** FluentValidation(구문 검증)과 VO.Validate()(도메인 검증)를 결합합니다.
- **교차 Aggregate 조율:** 여러 Repository와 Port를 조합하여 트랜잭션을 구성합니다.
- **읽기 전용 포트:** Aggregate 재구성 없이 DTO 프로젝션을 위한 Query Port를 정의합니다.
- **통합 응답 타입:** `FinResponse<T>` 모나드를 통해 성공/실패를 일관되게 전파합니다.
- **파이프라인 자동화:** Mediator 기반 Command/Query 라우팅과 FluentValidation 파이프라인 행동을 적용합니다.

도메인 레이어가 '비즈니스 규칙을 어떻게 보장하는가'에 집중했다면, Application 레이어는 '사용자 요청을 어떻게 처리하는가'에 집중합니다. 클라이언트의 HTTP 요청이 도메인 객체를 거쳐 데이터베이스에 도달하기까지의 전체 흐름을 오케스트레이션하는 것이 이 레이어의 핵심 역할입니다.

## 워크플로우 요구사항

### 4.1 CQRS 분리

읽기와 쓰기를 분리하는 이유는 트래픽 패턴의 비대칭성 때문입니다. 상품 검색은 초당 수천 건이 발생하지만 주문 생성은 수십 건에 불과합니다. 읽기 경로는 Aggregate 재구성 없이 DTO로 직접 프로젝션하여 성능을 최적화하고, 쓰기 경로는 도메인 모델을 거쳐 불변식을 보장합니다.

| ID | 규칙 |
|----|------|
| APP-W01 | Command는 상태를 변경하고 Repository write path를 사용한다 |
| APP-W02 | Query는 데이터를 조회하고 Query Port read path를 사용한다 |
| APP-W03 | Query는 Aggregate를 재구성하지 않고 DTO로 직접 프로젝션한다 |
| APP-W04 | Command와 Query는 독립적으로 최적화할 수 있다 |

### 4.2 이중 검증 전략

검증을 두 단계로 나누는 이유는 각 단계의 관심사가 다르기 때문입니다. 1차 검증(FluentValidation)은 null, 빈 문자열, 형식 오류 같은 구문적 문제를 걸러내고, 2차 검증(VO.Validate + Apply)은 도메인 규칙에 따른 의미적 문제를 검증합니다. 구문 검증을 통과하지 못한 요청은 Use Case에 진입하지 않으므로 불필요한 DB 조회를 방지합니다.

| ID | 규칙 |
|----|------|
| APP-W05 | 1차 검증: FluentValidation으로 구문 검증을 수행한다 (null, 범위, 형식) |
| APP-W06 | 2차 검증: VO.Validate()와 Apply 패턴으로 도메인 검증을 병렬 수행한다 |
| APP-W07 | `MustSatisfyValidation`으로 FluentValidation과 VO.Validate를 통합한다 |
| APP-W08 | Apply 패턴 실패 시 모든 검증 오류를 누적하여 한번에 반환한다 |

### 4.3 교차 Aggregate 조율

하나의 Use Case가 여러 Aggregate에 걸쳐 동작할 때, Application 레이어가 이를 조율합니다. 주문 생성 시 10개 상품의 가격을 개별 조회하면 10번의 DB 라운드트립이 발생합니다. `IProductCatalog`로 배치 조회하면 단일 WHERE IN 쿼리로 해결됩니다.

| ID | 규칙 |
|----|------|
| APP-W09 | 주문 생성 시 `IProductCatalog`로 상품 가격을 배치 조회한다 (N+1 방지) |
| APP-W10 | `OrderCreditCheckService` 도메인 서비스를 호출하여 신용한도를 검증한다 |
| APP-W11 | 상품 생성 시 Product와 Inventory를 함께 생성한다 (다중 Repository 조율) |

### 4.4 읽기 전용 포트

읽기 전용 포트는 CQRS의 Query 경로를 구현합니다. Command가 Repository를 통해 Aggregate를 재구성하는 반면, Query는 Read Port를 통해 DB에서 DTO로 직접 프로젝션합니다. 이 분리 덕분에 복잡한 JOIN이나 집계 쿼리를 도메인 모델의 제약 없이 최적화할 수 있습니다.

| ID | 규칙 |
|----|------|
| APP-W12 | Query Port는 읽기 전용 인터페이스로 정의한다 |
| APP-W13 | `IQueryPort<TAggregate, TDto>`를 통해 Specification 기반 검색을 지원한다 |
| APP-W14 | `IQueryPort`를 통해 단건 조회를 지원한다 |
| APP-W15 | Query Port는 Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션한다 |

### 4.5 응답 타입 전략

| ID | 규칙 |
|----|------|
| APP-W16 | 모든 Use Case는 `FinResponse<T>`를 반환한다 |
| APP-W17 | 성공/실패 모나드를 통해 에러를 전파한다 |
| APP-W18 | `FinT<IO, T>` LINQ 체이닝으로 비동기 효과를 합성한다 |

### 4.6 파이프라인 자동화

| ID | 규칙 |
|----|------|
| APP-W19 | Mediator를 통해 Command/Query를 자동 라우팅한다 |
| APP-W20 | FluentValidation 파이프라인 행동으로 요청을 사전 검증한다 |

## Use Case 카탈로그

다음은 이 샘플에서 구현하는 전체 Use Case 목록입니다. Command는 도메인 모델을 거쳐 상태를 변경하고, Query는 Read Port를 통해 데이터를 조회합니다. 각 Use Case는 하나의 sealed class 안에 Request, Response, Validator, Usecase를 중첩 타입으로 응집합니다.

### Products

| Use Case | 유형 | 설명 |
|----------|------|------|
| `CreateProductCommand` | Command | 상품 생성 + 재고 초기화 (Apply 패턴으로 VO 병렬 검증) |
| `UpdateProductCommand` | Command | 상품 수정 (삭제 가드, 상품명 고유성 검사) |
| `DeleteProductCommand` | Command | 상품 논리 삭제 (Soft Delete, 도메인 이벤트 발행) |
| `RestoreProductCommand` | Command | 삭제된 상품 복원 (도메인 이벤트 발행) |
| `DeductStockCommand` | Command | Inventory Aggregate를 통한 재고 차감 |
| `GetProductByIdQuery` | Query | 상품 상세 조회 (단건, `IProductDetailQuery`) |
| `GetAllProductsQuery` | Query | 전체 상품 조회 (`IProductQuery`) |
| `SearchProductsQuery` | Query | 상품 검색 — 이름, 가격 범위, 페이지네이션/정렬 (`IProductQuery`) |
| `SearchProductsWithStockQuery` | Query | 상품+재고 조회 — INNER JOIN (`IProductWithStockQuery`) |
| `SearchProductsWithOptionalStockQuery` | Query | 상품+재고 조회 — LEFT JOIN (`IProductWithOptionalStockQuery`) |

### Customers

| Use Case | 유형 | 설명 |
|----------|------|------|
| `CreateCustomerCommand` | Command | 고객 생성 (Apply 패턴, 이메일 고유성 검사) |
| `GetCustomerByIdQuery` | Query | 고객 상세 조회 (단건, `ICustomerDetailQuery`) |
| `GetCustomerOrdersQuery` | Query | 고객 주문 조회 — 4-table JOIN (`ICustomerOrdersQuery`) |
| `SearchCustomerOrderSummaryQuery` | Query | 고객 주문 요약 검색 — LEFT JOIN + GROUP BY 집계 (`ICustomerOrderSummaryQuery`) |

### Orders

| Use Case | 유형 | 설명 |
|----------|------|------|
| `CreateOrderCommand` | Command | 주문 생성 — `IProductCatalog`로 배치 가격 조회 |
| `CreateOrderWithCreditCheckCommand` | Command | 주문 생성 + `OrderCreditCheckService`로 신용한도 검증 |
| `GetOrderByIdQuery` | Query | 주문 상세 조회 (단건, `IOrderDetailQuery`) |
| `GetOrderWithProductsQuery` | Query | 주문+상품 조회 — 3-table JOIN (`IOrderWithProductsQuery`) |

### Inventories

| Use Case | 유형 | 설명 |
|----------|------|------|
| `SearchInventoryQuery` | Query | 재고 검색 — 저재고 필터, 페이지네이션/정렬 (`IInventoryQuery`) |

## 포트 카탈로그

### 교차 Aggregate 포트

| 포트 | 유형 | 설명 |
|------|------|------|
| `IProductCatalog` | 배치 조회 | 복수 상품의 가격을 일괄 조회 (WHERE IN, N+1 방지) |
| `IExternalPricingService` | 외부 서비스 | 외부 API에서 상품 가격을 조회 |

### Query 포트 (읽기 전용)

| 포트 | 패턴 | 프로젝션 DTO |
|------|------|-------------|
| `IProductQuery` | `IQueryPort<Product, ProductSummaryDto>` | ProductId, Name, Price |
| `IProductDetailQuery` | `IQueryPort` (단건) | ProductId, Name, Description, Price, CreatedAt, UpdatedAt |
| `IProductWithStockQuery` | `IQueryPort<Product, ProductWithStockDto>` | ProductId, Name, Price, StockQuantity (INNER JOIN) |
| `IProductWithOptionalStockQuery` | `IQueryPort<Product, ProductWithOptionalStockDto>` | ProductId, Name, Price, StockQuantity? (LEFT JOIN) |
| `ICustomerDetailQuery` | `IQueryPort` (단건) | CustomerId, Name, Email, CreditLimit, CreatedAt |
| `ICustomerOrdersQuery` | `IQueryPort` (단건) | 고객별 주문 + 주문라인 + 상품명 (4-table JOIN) |
| `ICustomerOrderSummaryQuery` | `IQueryPort<Customer, CustomerOrderSummaryDto>` | CustomerName, OrderCount, TotalSpent, LastOrderDate (LEFT JOIN + GROUP BY) |
| `IOrderDetailQuery` | `IQueryPort` (단건) | OrderId, OrderLines, TotalAmount, ShippingAddress, CreatedAt |
| `IOrderWithProductsQuery` | `IQueryPort` (단건) | 주문 + 주문라인 + 상품명 (3-table JOIN) |
| `IInventoryQuery` | `IQueryPort<Inventory, InventorySummaryDto>` | InventoryId, ProductId, StockQuantity |

## Use Case별 요구사항

### 5.1 CreateProductCommand

| ID | 규칙 |
|----|------|
| APP-CP01 | Apply 패턴으로 ProductName, ProductDescription, Money, Quantity를 병렬 검증한다 |
| APP-CP02 | 검증 실패 시 모든 오류를 누적하여 반환한다 |
| APP-CP03 | `ProductNameUniqueSpec`으로 상품명 고유성을 검사한다 |
| APP-CP04 | 고유성 위반 시 `AlreadyExists` 에러를 반환한다 |
| APP-CP05 | Product 생성 후 Inventory도 함께 생성한다 (동일 트랜잭션) |

### 5.2 UpdateProductCommand

| ID | 규칙 |
|----|------|
| APP-UP01 | Apply 패턴으로 ProductName, ProductDescription, Money를 병렬 검증한다 |
| APP-UP02 | `ProductNameUniqueSpec`에서 자기 자신을 제외하여 고유성을 검사한다 |
| APP-UP03 | 삭제된 상품은 도메인 모델의 Update 가드에 의해 거부된다 |

### 5.3 DeleteProductCommand

| ID | 규칙 |
|----|------|
| APP-DP01 | `GetByIdIncludingDeleted`로 삭제된 상품도 조회한다 |
| APP-DP02 | 도메인 모델의 `Delete(deletedBy)` 메서드를 호출하여 논리 삭제한다 |
| APP-DP03 | Repository `Update`로 저장한다 (삭제 이벤트 자동 발행) |

### 5.4 RestoreProductCommand

| ID | 규칙 |
|----|------|
| APP-RP01 | `GetByIdIncludingDeleted`로 삭제된 상품을 조회한다 |
| APP-RP02 | 도메인 모델의 `Restore()` 메서드를 호출하여 복원한다 |
| APP-RP03 | Repository `Update`로 저장한다 (복원 이벤트 자동 발행) |

### 5.5 DeductStockCommand

| ID | 규칙 |
|----|------|
| APP-DS01 | `Quantity` VO를 생성하여 차감 수량을 검증한다 |
| APP-DS02 | Inventory Aggregate의 `DeductStock` 메서드를 호출한다 |
| APP-DS03 | 재고 부족 시 도메인 모델이 에러를 반환한다 |

### 5.6 CreateCustomerCommand

| ID | 규칙 |
|----|------|
| APP-CC01 | Apply 패턴으로 CustomerName, Email, Money(CreditLimit)를 병렬 검증한다 |
| APP-CC02 | `CustomerEmailSpec`으로 이메일 고유성을 검사한다 |
| APP-CC03 | 고유성 위반 시 `AlreadyExists` 에러를 반환한다 |

### 5.7 CreateOrderCommand

| ID | 규칙 |
|----|------|
| APP-CO01 | ShippingAddress VO를 생성하여 배송주소를 검증한다 |
| APP-CO02 | 라인별 Quantity VO를 생성하여 수량을 검증한다 |
| APP-CO03 | `IProductCatalog.GetPricesForProducts`로 상품 가격을 배치 조회한다 (단일 라운드트립) |
| APP-CO04 | 조회 결과에 없는 ProductId는 `NotFound` 에러를 반환한다 |
| APP-CO05 | OrderLine을 생성하고 Order를 조립하여 저장한다 |

### 5.8 CreateOrderWithCreditCheckCommand

| ID | 규칙 |
|----|------|
| APP-OC01 | `CreateOrderCommand`와 동일한 검증 및 배치 조회를 수행한다 |
| APP-OC02 | `ICustomerRepository.GetById`로 고객을 조회한다 |
| APP-OC03 | `OrderCreditCheckService.ValidateCreditLimit`로 신용한도를 검증한다 |
| APP-OC04 | 신용한도 초과 시 도메인 서비스가 에러를 반환한다 |

### 5.9 SearchProductsQuery

| ID | 규칙 |
|----|------|
| APP-SP01 | 선택적 필터(이름, 가격 범위)를 Specification으로 합성한다 |
| APP-SP02 | `Specification<Product>.All`을 기본값으로 사용한다 |
| APP-SP03 | `PageRequest`와 `SortExpression`으로 페이지네이션/정렬을 지원한다 |
| APP-SP04 | 허용 정렬 필드: Name, Price |

### 5.10 GetCustomerOrdersQuery

| ID | 규칙 |
|----|------|
| APP-GC01 | `ICustomerOrdersQuery`로 4-table JOIN 결과를 조회한다 |
| APP-GC02 | Customer, Order, OrderLine, Product를 결합하여 상품명을 포함한다 |

### 5.11 SearchCustomerOrderSummaryQuery

| ID | 규칙 |
|----|------|
| APP-SC01 | `ICustomerOrderSummaryQuery`로 LEFT JOIN + GROUP BY 집계를 수행한다 |
| APP-SC02 | 고객별 총 주문 수, 총 지출, 마지막 주문일을 집계한다 |
| APP-SC03 | 허용 정렬 필드: CustomerName, OrderCount, TotalSpent, LastOrderDate |

### 5.12 SearchInventoryQuery

| ID | 규칙 |
|----|------|
| APP-SI01 | `InventoryLowStockSpec`으로 저재고 필터를 지원한다 |
| APP-SI02 | 허용 정렬 필드: StockQuantity, ProductId |

## 시나리오

다음 시나리오는 Application 레이어의 패턴들이 실제로 동작하는 방식을 검증합니다. 정상 시나리오는 Apply 패턴, 배치 조회, CQRS 분리가 올바르게 작동하는지, 거부 시나리오는 검증 실패와 에러 전파가 제대로 이루어지는지 확인합니다.

### 정상 시나리오

1. **CreateProduct** — Apply 패턴으로 4개 VO를 병렬 검증한다. 상품명 고유성 검사를 통과하면 Product와 Inventory를 함께 생성한다.
2. **CreateCustomer** — Apply 패턴으로 3개 VO를 병렬 검증한다. 이메일 고유성 검사를 통과하면 고객을 생성한다.
3. **CreateOrderWithCreditCheck** — 배치 가격 조회로 N+1을 방지한다. OrderLine을 조립하고 신용한도를 검증한 후 주문을 생성한다.
4. **SearchProducts** — Specification을 합성하여 이름/가격 범위 필터와 페이지네이션/정렬을 적용한다.
5. **GetCustomerOrders** — 4-table JOIN으로 고객의 모든 주문과 상품명을 한번에 조회한다.
6. **SearchCustomerOrderSummary** — LEFT JOIN + GROUP BY 집계로 고객별 주문 요약을 조회한다.

### 거부 시나리오

7. **다중 VO 검증 실패** — Apply 패턴이 모든 오류를 누적하여 한번에 반환한다. 첫 번째 오류에서 멈추지 않는다.
8. **AlreadyExists** — 중복 상품명 또는 중복 이메일로 생성을 시도하면 `AlreadyExists` 에러를 반환한다.
9. **NotFound** — 존재하지 않는 상품 ID로 주문라인을 구성하면 `NotFound` 에러를 반환한다.
10. **도메인 에러 전파** — `CreditLimitExceeded` 등 도메인 에러가 `FinResponse.Fail`로 래핑되어 호출자에게 전파된다.
11. **FluentValidation 거부** — 구문 검증 실패 시 파이프라인 행동이 Use Case 진입 전에 요청을 거부한다.

## 존재해서는 안 되는 상태

다음은 Application 레이어에서 절대 발생해서는 안 되는 상태입니다. 이 상태가 존재한다면 CQRS 분리, 검증 파이프라인, 또는 포트 추상화가 깨진 것입니다.

- VO 검증을 우회하여 생성된 도메인 객체
- N+1 쿼리 (배치 조회를 사용하지 않은 교차 Aggregate 조회)
- Command에서 DTO 프로젝션을 반환하는 CQRS 위반
- Adapter 구현체에 대한 의존성이 Application 레이어에 침투한 상태
- FluentValidation 검증 없이 Use Case에 진입한 요청
- Query에서 Aggregate를 재구성하여 반환하는 상태 (DTO 프로젝션 우회)

## 제약조건 요약

| 대상 | 제약조건 | 규칙 ID |
|------|----------|---------|
| Command/Query 분리 | Command는 write, Query는 read | APP-W01, APP-W02 |
| 1차 검증 | FluentValidation 구문 검증 | APP-W05 |
| 2차 검증 | VO.Validate() + Apply 병렬 검증 | APP-W06, APP-W08 |
| MustSatisfyValidation | FluentValidation ↔ VO.Validate 통합 | APP-W07 |
| 배치 조회 | IProductCatalog로 N+1 방지 | APP-W09 |
| 도메인 서비스 호출 | OrderCreditCheckService 신용한도 검증 | APP-W10 |
| Query Port | Aggregate 재구성 없이 DTO 프로젝션 | APP-W12, APP-W15 |
| 응답 타입 | FinResponse\<T\> 통합 반환 | APP-W16 |
| 파이프라인 | Mediator 라우팅 + FluentValidation 행동 | APP-W19, APP-W20 |

다음 단계에서는 이 비즈니스 요구사항을 기반으로 Application 레이어의 타입 설계 의사결정을 도출합니다.
