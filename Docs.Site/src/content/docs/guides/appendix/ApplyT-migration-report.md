---
title: "ApplyT 미적용 코드 전수 조사 보고서"
---

## 요약

`ApplyT`는 여러 `Fin<T>` 결과를 합성하여 `FinT<IO, R>`로 리프팅하는 확장 메서드입니다.
LINQ `from` 첫 구문에서 사용하면 Unwrap 없이 VO를 합성하고 에러를 수집합니다.

**적용 완료:** 6개 Command 핸들러 (CreateProduct, UpdateProduct, CreateCustomer, DeductStock + ecommerce-ddd 동일)
**미적용:** 아래 4가지 범주 (구조적 이유로 ApplyT 적용 불가)

---

## 범주 1: 컬렉션 순회 + 다단계 Aggregate 생성

### 해당 파일

| 프로젝트 | 파일 | Unwrap 수 |
|----------|------|-----------|
| Tests.Hosts | `CreateOrderCommand.cs` | 5개 |
| Tests.Hosts | `CreateOrderWithCreditCheckCommand.cs` | 5개 |
| ecommerce-ddd | `CreateOrderCommand.cs` | 5개 |
| ecommerce-ddd | `CreateOrderWithCreditCheckCommand.cs` | 5개 |
| ecommerce-ddd | `PlaceOrderCommand.cs` | 3개 |

### 이유

Order 핸들러는 `OrderLineRequest` 컬렉션을 순회하면서:
1. 각 항목의 ProductId와 Quantity로 가격 조회
2. 가격과 수량으로 `OrderLine.Create()` 호출
3. 전체 OrderLine 목록으로 `Order.Create()` 호출

```csharp
// 컬렉션 순회 — ApplyT는 고정 tuple만 지원
foreach (var (productId, quantity) in orderLineData)
{
    var unitPrice = priceLookup[productId];
    orderLines.Add(OrderLine.Create(productId, quantity, unitPrice).Unwrap());
}
var order = Order.Create(customerId, orderLines, shippingAddress).Unwrap();
```

**ApplyT 불가 이유:** ApplyT는 2~5 tuple의 고정 크기 합성만 지원.
컬렉션 크기가 동적이므로 tuple Apply 패턴을 사용할 수 없음.
또한 중간에 `priceLookup` 조회가 필요하여 순차 실행이 불가피.

---

## 범주 2: 루프 내 개별 항목 VO 합성

### 해당 파일

| 프로젝트 | 파일 | Unwrap 수 |
|----------|------|-----------|
| Tests.Hosts | `BulkCreateProductsCommand.cs` | 1개 |

### 이유

```csharp
foreach (var item in request.Products)
{
    var vos = (Create(), Create(), Create())
        .Apply((n, d, p) => (n, d, p)).Unwrap();  // Fin<R>.Unwrap()
    products.Add(Product.Create(vos.Name, vos.Desc, vos.Price));
}
```

**ApplyT 불가 이유:** `ApplyT`는 `FinT<IO, R>`을 반환하여 LINQ 체인 시작점으로 사용.
그러나 이 코드는 `foreach` 루프 내부에서 동기적으로 VO를 생성하므로
`FinT<IO>` 컨텍스트가 아닌 명령형 코드.
`Apply()` + `Unwrap()`으로 에러 수집 후 추출하는 것이 현재 가능한 최선.

---

## 범주 3: Query BuildSpecification (명령형 코드)

### 해당 파일

| 프로젝트 | 파일 | Unwrap 수 |
|----------|------|-----------|
| Tests.Hosts | `SearchProductsQuery.cs` | 3개 |
| Tests.Hosts | `SearchProductsWithStockQuery.cs` | 2개 |
| Tests.Hosts | `SearchProductsWithOptionalStockQuery.cs` | 2개 |
| Tests.Hosts | `SearchInventoryQuery.cs` | 1개 |
| ecommerce-ddd | `SearchProductsQuery.cs` | 3개 |
| ecommerce-ddd | `SearchProductsWithStockQuery.cs` | 2개 |
| ecommerce-ddd | `SearchProductsWithOptionalStockQuery.cs` | 2개 |
| ecommerce-ddd | `SearchInventoryQuery.cs` | 1개 |

### 이유

```csharp
// BuildSpecification — 조건부 Specification 조합
if (!string.IsNullOrEmpty(request.Name))
    spec &= new ProductNameSpec(ProductName.Create(request.Name).Unwrap());

if (request.MinPrice > 0 && request.MaxPrice > 0)
    spec &= new PriceRangeSpec(
        Money.Create(request.MinPrice).Unwrap(),
        Money.Create(request.MaxPrice).Unwrap());
```

**ApplyT 불가 이유:** `BuildSpecification`은 `FinT<IO>` LINQ 체인이 아닌 명령형 if 분기.
각 조건이 독립적이므로 tuple Apply로 합성할 수 없음.
VO 생성이 조건부(`if` 가드)이므로 고정 tuple 크기를 결정할 수 없음.

---

## 범주 4: 정적 필드 초기화

### 해당 파일

| 프로젝트 | 파일 | Unwrap 수 |
|----------|------|-----------|
| ecommerce-ddd | `DetectLowStockOnStockDeductedHandler.cs` | 1개 |

### 이유

```csharp
private static readonly Quantity DefaultThreshold = Quantity.Create(10).Unwrap();
```

**ApplyT 불가 이유:** 클래스 수준 정적 필드 초기화.
LINQ 체인이 아니며, 상수 값이므로 실패 불가.
`Unwrap()` 유지가 적절.

---

## 통계

| 범주 | Tests.Hosts | ecommerce-ddd | 합계 |
|------|-------------|---------------|------|
| ApplyT 적용 완료 | 4개 핸들러 | 4개 핸들러 | 8개 |
| 컬렉션 순회 (범주 1) | 2개 | 3개 | 5개 |
| 루프 내 개별 (범주 2) | 1개 | 0개 | 1개 |
| Query 명령형 (범주 3) | 4개 | 4개 | 8개 |
| 정적 초기화 (범주 4) | 0개 | 1개 | 1개 |

## 결론

ApplyT 미적용 코드는 **구조적으로 tuple Apply 패턴이 불가능한 경우**에 한정됩니다:
- 동적 크기 컬렉션 순회
- 조건부 분기 (if 가드)
- 명령형 코드 (FinT LINQ 체인 아님)
- 정적 필드 초기화

이들은 `Unwrap()` 유지가 적절하며, 파이프라인 Validator가 검증을 완료한 후
`Create()`가 반드시 성공하는 컨텍스트에서 안전하게 사용됩니다.
