---
title: "Apply+Bind 혼합 검증"
---

## 개요

주문 정보를 검증한다고 가정합니다. 고객명과 이메일은 서로 독립적이므로 병렬로 검증할 수 있지만, 할인 금액은 주문 금액보다 클 수 없으므로 두 값 사이에 의존 관계가 존재합니다. Apply만으로도, Bind만으로도 이 상황을 깔끔하게 처리할 수 없습니다. 독립적인 검증에는 Apply를, 의존적인 검증에는 Bind를 조합하여 적용하는 혼합 패턴이 필요합니다.

## 학습 목표

- Apply와 Bind를 적절히 조합하여 복잡한 비즈니스 로직을 효율적으로 검증하는 **혼합 검증 패턴을** 구현할 수 있습니다.
- 독립 정보와 의존 정보를 구분하여 각각에 적합한 검증 방식을 적용하는 **단계별 검증 전략을** 이해할 수 있습니다.
- 실제 도메인에서 자주 발생하는 복합적인 검증 요구사항을 **실무적으로 설계하고** 해결할 수 있습니다.

## 왜 필요한가?

Apply와 Bind를 각각 별도로 다뤘지만, 실제 비즈니스 도메인에서는 독립적인 정보와 의존적인 정보가 하나의 객체에 함께 존재합니다.

주문 정보가 대표적인 예입니다. 고객명과 이메일은 독립적으로 검증할 수 있지만, 주문 금액과 할인 금액은 서로 의존적인 관계를 가집니다. 독립적인 정보는 병렬로 검증하여 성능을 최적화하고, 의존적인 정보는 순차로 검증하여 논리적 일관성을 보장해야 합니다. 또한 각 검증 단계에서 발생하는 에러를 적절히 구분하고 처리해야 하는데, Apply 단계에서는 여러 에러가 수집되고 Bind 단계에서는 단일 에러가 발생할 수 있습니다.

**Apply+Bind 혼합 검증 패턴은** 이런 복합적인 도메인 요구사항을 효율적이고 논리적으로 처리합니다.

## 핵심 개념

### 2단계 검증 전략

혼합 검증은 Apply(독립) 단계와 Bind(의존) 단계를 순서대로 실행합니다. 독립적인 기본 정보를 먼저 병렬 검증하고, 그 결과가 성공하면 의존적인 정보를 순차 검증합니다.

다음 코드는 단일 방식 처리와 2단계 혼합 처리를 비교합니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 모든 검증을 하나의 방식으로 처리
public static Validation<Error, OrderInfo> ValidateOld(string customerName, string customerEmail, string orderAmountInput, string discountInput)
{
    // 모든 검증을 순차적으로 실행하여 비효율적
    var nameResult = ValidateCustomerName(customerName);
    var emailResult = ValidateCustomerEmail(customerEmail);
    var amountResult = ValidateOrderAmount(orderAmountInput);
    var discountResult = ValidateDiscount(discountInput);
    // 독립적인 검증도 순차 실행하여 성능 저하
}

// 개선된 방식 (현재 방식) - 2단계 검증 전략
public static Validation<Error, (string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount)> Validate(
    string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
    // 1단계: 독립 검증 (Apply) - 기본 정보들을 병렬로 검증
    (ValidateCustomerName(customerName), ValidateCustomerEmail(customerEmail))
        .Apply((validName, validEmail) => (validName, validEmail))
        .As()
        // 2단계: 의존 검증 (Bind) - 금액 정보들을 순차적으로 검증
        .Bind(_ => ValidateOrderAmount(orderAmountInput))
        .Bind(_ => ValidateFinalAmount(orderAmountInput, discountInput))
        .Map(_ => (customerName: customerName,
                   customerEmail: customerEmail,
                   orderAmount: decimal.Parse(orderAmountInput),
                   finalAmount: decimal.Parse(orderAmountInput) - decimal.Parse(discountInput)));
```

이 방식에서는 Apply 단계에서 고객명과 이메일의 에러를 한 번에 수집하고, Bind 단계에서는 금액 관련 의존성을 순차적으로 검증합니다.

### 비즈니스 규칙의 의존성 표현

할인 금액이 주문 금액을 초과하는지 확인하는 것은 두 값 사이의 의존 관계를 검증하는 것입니다. 이런 규칙은 Bind로 자연스럽게 표현됩니다.

```csharp
// 할인 금액과 주문 금액 간의 의존성 검증
private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
    decimal.TryParse(orderAmountInput, out var orderAmount) &&
    decimal.TryParse(discountInput, out var discount) &&
    discount >= 0 && discount <= orderAmount
        ? orderAmount - discount
        : DomainError.For<OrderInfo>(new DiscountAmountExceedsOrder(), $"{orderAmountInput}:{discountInput}", "Discount amount exceeds order amount");
```

## 실전 지침

### 예상 출력
```
=== 혼합 검증 (Mixed Validation) 예제 ===
Apply(독립) + Bind(의존) 혼용 패턴을 보여주는 예제입니다.

--- 성공 ---
입력: '홍길동', 'hong@example.com', '100000', '10000'
성공: 홍길동 - ₩100,000 → ₩90,000

--- Apply 실패 - 둘 다 ---
입력: '', 'invalid', '100000', '10000'
실패:
   → Apply 단계에서 2개 에러 수집
     1. Domain.OrderInfo.CustomerNameTooShort: ''
     2. Domain.OrderInfo.CustomerEmailMissingAt: 'invalid'

--- Bind 실패 - 할인금액 초과 ---
입력: '홍길동', 'hong@example.com', '100000', '150000'
실패:
   → Bind 단계에서 단일 에러: Domain.OrderInfo.DiscountAmountExceedsOrder: '100000:150000'
```

### 핵심 구현 포인트

구현 시 세 가지 포인트에 주목합니다. Apply(독립) 단계와 Bind(의존) 단계를 순서대로 구성하고, Apply 단계는 ManyErrors로 에러를 수집하고 Bind 단계는 단일 Error로 조기 중단하며, `.Map()`으로 최종 결과를 원본 매개변수와 계산된 값으로 구성합니다.

## 프로젝트 설명

### 프로젝트 구조
```
03-Apply-Bind-Combined-Validation/
├── Program.cs              # 메인 실행 파일
├── ValueObjects/
│   └── OrderInfo.cs        # 주문 정보 값 객체 (혼합 검증 패턴 구현)
├── ApplyBindCombinedValidation.csproj
└── README.md               # 메인 문서
```

### 핵심 코드

OrderInfo 값 객체는 Apply로 고객 정보를 병렬 검증하고, Bind로 금액 정보를 순차 검증합니다.

```csharp
public sealed class OrderInfo : ValueObject
{
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public decimal OrderAmount { get; }
    public decimal FinalAmount { get; }

    // 혼합 검증 패턴 구현 (Apply + Bind)
    public static Validation<Error, (string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount)> Validate(
        string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
        // 1단계: 독립 검증 (Apply) - 기본 정보들을 병렬로 검증
        (ValidateCustomerName(customerName), ValidateCustomerEmail(customerEmail))
            .Apply((validName, validEmail) => (validName, validEmail))
            .As()
            // 2단계: 의존 검증 (Bind) - 금액 정보들을 순차적으로 검증
            .Bind(_ => ValidateOrderAmount(orderAmountInput))
            .Bind(_ => ValidateFinalAmount(orderAmountInput, discountInput))
            .Map(_ => (customerName: customerName,
                       customerEmail: customerEmail,
                       orderAmount: decimal.Parse(orderAmountInput),
                       finalAmount: decimal.Parse(orderAmountInput) - decimal.Parse(discountInput)));

    // 비즈니스 규칙 검증 - 할인 금액이 주문 금액을 초과할 수 없음
    private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
        decimal.TryParse(orderAmountInput, out var orderAmount) &&
        decimal.TryParse(discountInput, out var discount) &&
        discount >= 0 && discount <= orderAmount
            ? orderAmount - discount
            : DomainError.For<OrderInfo>(new DiscountAmountExceedsOrder(), $"{orderAmountInput}:{discountInput}", "Discount amount exceeds order amount");
}
```

## 한눈에 보는 정리

다음 표는 세 가지 검증 패턴의 특성을 비교합니다.

| 구분 | Apply 병렬 검증 | Bind 순차 검증 | Apply+Bind 혼합 검증 |
|------|----------------|----------------|---------------------|
| **적용 대상** | 모든 검증이 독립적 | 모든 검증이 의존적 | 독립 + 의존 혼합 |
| **실행 방식** | 모든 검증을 병렬 실행 | 모든 검증을 순차 실행 | 단계별 최적화된 실행 |
| **에러 처리** | ManyErrors로 모든 에러 수집 | 단일 Error로 조기 중단 | 단계별 에러 구분 |
| **성능** | 병렬 실행으로 빠름 | 조기 중단으로 효율적 | 각 단계에서 최적화 |

다음 표는 혼합 검증 패턴의 장단점을 정리합니다.

| 장점 | 단점 |
|------|------|
| 복잡한 도메인 요구사항에 적합 | 검증 로직이 복잡해짐 |
| 각 단계에서 적절한 전략 사용 | 단계별 에러 원인 파악 필요 |
| 실제 비즈니스 로직에 가까움 | 검증 단계를 신중히 설계해야 함 |

## FAQ

### Q1: 언제 혼합 검증 패턴을 사용해야 하나요?
**A:** 도메인 객체가 독립적인 정보와 의존적인 정보를 모두 포함할 때 사용합니다. 주문 정보에서 고객 정보는 독립적이지만 금액 정보는 의존적인 경우가 대표적입니다.

### Q2: Apply와 Bind의 순서는 어떻게 결정하나요?
**A:** 일반적으로 독립적인 검증을 먼저 실행하고, 의존적인 검증을 나중에 실행합니다. 전제 조건이 되는 검증을 먼저 수행하고, 그 결과를 바탕으로 복잡한 비즈니스 규칙을 검증하면 불필요한 연산을 피할 수 있습니다.

### Q3: 에러 처리는 어떻게 구분하나요?
**A:** Apply 단계에서는 ManyErrors 타입으로 여러 에러를 수집하고, Bind 단계에서는 단일 Error 타입으로 조기 중단합니다. 사용자에게는 어떤 단계에서 실패했는지 명확하게 알려주는 것이 중요합니다.

지금까지 Apply와 Bind를 하나의 흐름에서 순서대로 조합했습니다. 하지만 각 필드 내부에서도 복잡한 다단계 검증이 필요하다면 어떻게 해야 할까요? 다음 장에서는 Apply 내부에 Bind를 중첩하여 필드별 세밀한 검증을 구현합니다.

---

→ [4장: 내부 Bind 외부 Apply](../04-Apply-Internal-Bind-Validation/)
