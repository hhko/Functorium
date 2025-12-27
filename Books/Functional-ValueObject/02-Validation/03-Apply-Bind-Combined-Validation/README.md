# Apply Bind 혼합 검증 (Apply Bind Combined Validation)

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 ValueObject에서 **독립적인 검증과 의존적인 검증을 혼합**하여 사용하는 패턴을 학습합니다. 주문 정보 값 객체를 통해 기본 정보는 Apply로 병렬 검증하고, 금액 정보는 Bind로 순차 검증하는 실무적인 검증 전략을 알아봅니다.

## 학습 목표

### **핵심 학습 목표**
1. **혼합 검증 패턴 구현**: Apply와 Bind를 적절히 조합하여 복잡한 비즈니스 로직을 효율적으로 검증합니다.
2. **단계별 검증 전략 이해**: 독립 정보와 의존 정보를 구분하여 각각에 적합한 검증 방식을 적용합니다.
3. **실무적 검증 설계**: 실제 도메인에서 자주 발생하는 복합적인 검증 요구사항을 해결합니다.

### **실습을 통해 확인할 내용**
- **2단계 검증**: Apply(독립) → Bind(의존) 순서로 검증 실행
- **에러 구분**: Apply 단계와 Bind 단계의 에러를 구분하여 처리
- **비즈니스 로직**: 주문 금액과 할인 금액 간의 의존성 검증

## 왜 필요한가?

이전 단계인 `Apply-Parallel-Validation`과 `Bind-Sequential-Validation`에서는 각각 독립적이거나 의존적인 검증만을 다뤘습니다. 하지만 실제 비즈니스 도메인에서는 **독립적인 정보와 의존적인 정보가 함께 존재**하는 경우가 많습니다.

**첫 번째 문제는 복합적인 도메인 요구사항입니다.** 마치 마이크로서비스 아키텍처처럼, 하나의 도메인 객체가 여러 종류의 검증 규칙을 포함해야 합니다. 예를 들어, 주문 정보에서 고객명과 이메일은 독립적으로 검증할 수 있지만, 주문 금액과 할인 금액은 서로 의존적인 관계를 가집니다.

**두 번째 문제는 검증 단계의 최적화입니다.** 마치 파이프라인 아키텍처처럼, 각 단계에서 적절한 검증 전략을 선택해야 합니다. 독립적인 정보는 병렬로 검증하여 성능을 최적화하고, 의존적인 정보는 순차로 검증하여 논리적 일관성을 보장해야 합니다.

**세 번째 문제는 에러 처리의 복잡성입니다.** 마치 계층적 에러 처리처럼, 각 검증 단계에서 발생하는 에러를 적절히 구분하고 처리해야 합니다. Apply 단계에서는 여러 에러가 수집되고, Bind 단계에서는 단일 에러가 발생할 수 있습니다.

이러한 문제들을 해결하기 위해 **Apply Bind 혼합 검증 패턴**을 도입했습니다. 이 패턴을 사용하면 복잡한 도메인 요구사항을 효율적이고 논리적으로 처리할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 2가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 2단계 검증 전략

**핵심 아이디어는 "단계별 최적화된 검증 전략"입니다.** 마치 계층적 아키텍처처럼, 각 단계에서 적절한 검증 방식을 선택합니다.

예를 들어, 주문 정보 검증을 생각해보세요. 마치 마이크로서비스 아키텍처처럼 각 서비스가 독립적이면서도 상호 의존적인 관계를 가집니다.

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

이 방식의 장점은 각 검증 단계에서 최적화된 전략을 사용할 수 있다는 것입니다.

### 비즈니스 규칙의 의존성 표현

**핵심 아이디어는 "도메인 지식의 코드화"입니다.** 마치 비즈니스 규칙 엔진처럼, 복잡한 비즈니스 로직을 명확하고 타입 안전하게 표현합니다.

```csharp
// 할인 금액과 주문 금액 간의 의존성 검증
private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
    decimal.TryParse(orderAmountInput, out var orderAmount) && 
    decimal.TryParse(discountInput, out var discount) && 
    discount >= 0 && discount <= orderAmount
        ? orderAmount - discount
        : DomainErrors.DiscountAmountExceedsOrder(orderAmountInput, discountInput);
```

이 방식의 장점은 복잡한 비즈니스 규칙을 명확하고 검증 가능한 코드로 표현할 수 있다는 것입니다.

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
     1. DomainErrors.OrderInfo.CustomerNameTooShort: ''
     2. DomainErrors.OrderInfo.CustomerEmailMissingAt: 'invalid'

--- Bind 실패 - 할인금액 초과 ---
입력: '홍길동', 'hong@example.com', '100000', '150000'
실패:
   → Bind 단계에서 단일 에러: DomainErrors.OrderInfo.DiscountAmountExceedsOrder: '100000:150000'
```

### 핵심 구현 포인트
1. **2단계 구조**: Apply(독립) → Bind(의존) 순서로 검증
2. **에러 구분**: Apply 단계는 ManyErrors, Bind 단계는 단일 Error
3. **Map 활용**: 최종 결과를 원본 매개변수와 계산된 값으로 구성

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

**OrderInfo.cs - 혼합 검증 패턴 구현**
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
            : DomainErrors.DiscountAmountExceedsOrder(orderAmountInput, discountInput);
}
```

## 한눈에 보는 정리

### 비교 표
| 구분 | Apply 병렬 검증 | Bind 순차 검증 | Apply Bind 혼합 검증 |
|------|----------------|----------------|---------------------|
| **적용 대상** | 모든 검증이 독립적 | 모든 검증이 의존적 | 독립 + 의존 혼합 |
| **실행 방식** | 모든 검증을 병렬 실행 | 모든 검증을 순차 실행 | 단계별 최적화된 실행 |
| **에러 처리** | ManyErrors로 모든 에러 수집 | 단일 Error로 조기 중단 | 단계별 에러 구분 |
| **성능** | 병렬 실행으로 빠름 | 조기 중단으로 효율적 | 각 단계에서 최적화 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **유연성** | 복잡한 도메인 요구사항에 적합 | **복잡성** | 검증 로직이 복잡해짐 |
| **최적화** | 각 단계에서 적절한 전략 사용 | **디버깅** | 단계별 에러 원인 파악 필요 |
| **실무성** | 실제 비즈니스 로직에 가까움 | **설계** | 검증 단계를 신중히 설계해야 함 |

## FAQ

### Q1: 언제 혼합 검증 패턴을 사용해야 하나요?
**A**: 도메인 객체가 독립적인 정보와 의존적인 정보를 모두 포함할 때 사용하세요. 마치 마이크로서비스 아키텍처처럼, 일부 서비스는 독립적으로 동작하고 일부 서비스는 상호 의존적일 때 적합합니다. 예를 들어, 주문 정보에서 고객 정보는 독립적이지만 금액 정보는 의존적인 경우입니다.

### Q2: Apply와 Bind의 순서는 어떻게 결정하나요?
**A**: 일반적으로 독립적인 검증을 먼저 실행하고, 의존적인 검증을 나중에 실행합니다. 마치 파이프라인 아키텍처처럼, 전제 조건이 되는 검증을 먼저 수행하고, 그 결과를 바탕으로 복잡한 비즈니스 규칙을 검증합니다. 이렇게 하면 불필요한 복잡한 검증을 피할 수 있습니다.

### Q3: 에러 처리는 어떻게 구분하나요?
**A**: Apply 단계에서는 ManyErrors 타입으로 여러 에러를 수집하고, Bind 단계에서는 단일 Error 타입으로 조기 중단합니다. 마치 계층적 에러 처리처럼, 각 단계에서 발생하는 에러의 특성을 고려하여 적절히 처리해야 합니다. 사용자에게는 어떤 단계에서 실패했는지 명확하게 알려주는 것이 중요합니다.

### Q4: Map에서 원본 매개변수를 사용하는 이유는?
**A**: Bind 체인에서 중간 결과들을 무시하고 원본 입력값들을 사용하여 최종 객체를 구성하기 위해서입니다. 마치 함수형 프로그래밍의 합성처럼, 검증 과정에서 변환된 값이 아닌 원본 입력값들을 사용하여 일관된 결과를 보장합니다. 이는 검증과 객체 생성을 분리하여 코드의 명확성을 높입니다.
