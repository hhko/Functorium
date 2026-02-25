# Part 5 - Chapter 18: 고객 관리 시스템

> **Part 5: 도메인 예제** | [← 이전: 17장 전자상거래 상품 필터링](../01-Ecommerce-Product-Filtering/README.md) | [← 목차로](../../README.md)

---

## 개요

상품(Product)과는 다른 집합체(Aggregate)인 고객(Customer) 도메인에서 Specification 패턴을 적용합니다. 이 장에서는 ExpressionSpecification과 non-Expression Specification을 혼합하여 사용하는 방법을 학습합니다.

> **Expression과 non-Expression Specification을 상황에 맞게 선택하여 사용합니다.**

## 학습 목표

1. **다른 집합체에 Specification 적용**: Product가 아닌 Customer 도메인에서의 활용
2. **Expression vs non-Expression 선택 기준**: 각 방식의 적합한 사용 시나리오 이해
3. **혼합 조합**: ExpressionSpecification과 Specification을 `&`, `|` 연산자로 조합
4. **대소문자 무시 검색**: Expression Tree에서의 문자열 비교 전략

## 핵심 개념

### Expression vs non-Expression Specification

모든 조건이 Expression Tree로 표현될 필요는 없습니다. 단순한 속성 확인은 `IsSatisfiedBy`만 오버라이드하는 것이 더 간결합니다.

```csharp
// ExpressionSpecification: EF Core SQL 번역이 필요한 경우
public sealed class CustomerEmailSpec : ExpressionSpecification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = Email;
        return customer => (string)customer.Email == emailStr;
    }
}

// non-Expression Specification: 인메모리 검증만 필요한 경우
public sealed class ActiveCustomerSpec : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer entity) => entity.IsActive;
}
```

### Expression에서 대소문자 무시 검색

Expression Tree 내부에서는 `string.Contains(string, StringComparison)`을 사용할 수 없습니다. 대신 `.ToLower().Contains()` 패턴을 사용합니다.

```csharp
public override Expression<Func<Customer, bool>> ToExpression()
{
    string searchLower = ((string)SearchName).ToLower();
    return customer => ((string)customer.Name).ToLower().Contains(searchLower);
}
```

### 혼합 조합

ExpressionSpecification과 non-Expression Specification은 기반 클래스인 `Specification<T>`의 `&`, `|`, `!` 연산자를 통해 자유롭게 조합할 수 있습니다.

```csharp
// ActiveCustomerSpec(non-Expression) & CustomerNameContainsSpec(Expression)
var spec = new ActiveCustomerSpec() & new CustomerNameContainsSpec(new CustomerName("수"));
```

## 프로젝트 설명

### 프로젝트 구조
```
CustomerManagement/
├── Domain/
│   ├── ValueObjects/
│   │   ├── CustomerId.cs
│   │   ├── CustomerName.cs
│   │   └── Email.cs
│   ├── Customer.cs
│   ├── ICustomerRepository.cs
│   └── Specifications/
│       ├── CustomerEmailSpec.cs          # ExpressionSpecification
│       ├── CustomerNameContainsSpec.cs   # ExpressionSpecification
│       └── ActiveCustomerSpec.cs         # non-Expression Specification
├── Infrastructure/
│   └── InMemoryCustomerRepository.cs
├── SampleCustomers.cs
└── Program.cs
```

### Specification 목록

| Specification | 기반 클래스 | 설명 |
|---------------|-------------|------|
| `CustomerEmailSpec` | ExpressionSpecification | 이메일 정확 일치 |
| `CustomerNameContainsSpec` | ExpressionSpecification | 이름 부분 일치 (대소문자 무시) |
| `ActiveCustomerSpec` | Specification | 활성 고객 확인 |

## 한눈에 보는 정리

| 구분 | 내용 |
|------|------|
| **도메인** | 고객 관리 (Customer) |
| **Value Objects** | CustomerId, CustomerName, Email |
| **ExpressionSpecification** | CustomerEmailSpec, CustomerNameContainsSpec |
| **non-Expression Specification** | ActiveCustomerSpec |
| **핵심 패턴** | Expression/non-Expression 혼합 조합 |

### Expression vs non-Expression 선택 기준

| 기준 | ExpressionSpecification | Specification |
|------|------------------------|---------------|
| **EF Core SQL 번역** | 지원 | 미지원 |
| **구현 복잡도** | 높음 (Expression Tree) | 낮음 (직접 로직) |
| **사용 시나리오** | DB 쿼리 필터링 | 인메모리 검증 |
| **Value Object 처리** | implicit 변환 필요 | 직접 접근 가능 |

## FAQ

### Q1: 언제 ExpressionSpecification 대신 일반 Specification을 사용하나요?
**A**: EF Core 등 ORM을 통한 SQL 번역이 필요하지 않고, 인메모리에서만 검증하는 단순한 조건이라면 일반 Specification이 더 간결합니다. `ActiveCustomerSpec`처럼 단순 속성 확인이 대표적인 예입니다.

### Q2: Expression과 non-Expression Specification을 조합할 수 있는 이유는?
**A**: 두 가지 모두 `Specification<T>` 기반 클래스를 상속하므로, `&`, `|`, `!` 연산자를 통해 자유롭게 조합할 수 있습니다. 조합 결과는 인메모리에서 `IsSatisfiedBy`를 통해 평가됩니다.

### Q3: Expression Tree에서 StringComparison을 사용할 수 없는 이유는?
**A**: Expression Tree는 SQL 등으로 번역되어야 하므로, .NET 전용 API인 `StringComparison`은 번역할 수 없습니다. 대신 `.ToLower().Contains()` 패턴을 사용하면 SQL의 `LOWER()` 함수로 자연스럽게 번역됩니다.
