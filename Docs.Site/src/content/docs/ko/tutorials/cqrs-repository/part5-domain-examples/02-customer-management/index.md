---
title: "고객 관리"
---
## 개요

동일한 이메일로 고객을 중복 등록하면 안 됩니다. 이메일 중복 검사를 어떻게 구현할까요? 전체 고객 목록을 불러와서 직접 비교하면 비효율적이고, 검색 조건이 늘어날수록 코드가 복잡해집니다.

이 장에서는 고객(Customer) 도메인을 통해 **Specification 패턴**으로 검색 조건을 캡슐화하고, Repository의 `Exists()` 메서드로 유일성을 검증하는 방법을 구현합니다. IAuditable 인터페이스로 생성/수정 시각을 추적하고, 동적 필터 빌더 패턴도 함께 살펴봅니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **Specification 패턴**으로 검색 조건을 캡슐화할 수 있습니다
2. **Specification 조합** (And, Or, Not 연산자)을 활용할 수 있습니다
3. **Repository.Exists()** 로 유일성을 검증할 수 있습니다
4. **동적 필터 빌더** (All + 조건부 체이닝)를 구현할 수 있습니다
5. **InMemoryQueryBase** 기반 Query Adapter를 구현할 수 있습니다

---

## 핵심 개념

### Specification 패턴

검색 조건을 객체로 캡슐화하면 재사용과 조합이 쉬워집니다. 단일 Specification을 만들고 `&`, `|` 연산자로 조합하는 방법을 보세요.

```csharp
// 단일 Specification
var emailSpec = new CustomerEmailSpec("kim@example.com");
var nameSpec = new CustomerNameSpec("김");

// 조합: & 연산자로 And
var composedSpec = nameSpec & emailSpec;

// 동적 빌더: All을 시드로 조건부 추가
var filter = Specification<Customer>.All;
if (!string.IsNullOrEmpty(nameFilter))
    filter = filter & new CustomerNameSpec(nameFilter);
```

`Specification<T>.All`은 And 연산의 항등원이므로, 조건이 하나도 추가되지 않으면 모든 데이터를 반환합니다.

### Repository에서 Specification 활용

이메일 중복 검사처럼 "조건에 맞는 데이터가 존재하는가?"를 확인할 때 `Exists()`를 사용합니다. 전체 Aggregate를 로드하지 않으므로 성능상 유리합니다.

```csharp
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    FinT<IO, bool> Exists(Specification<Customer> spec);
}

// 이메일 중복 확인
var exists = await repository
    .Exists(new CustomerEmailSpec("kim@example.com"))
    .Run().RunAsync();
```

---

## 프로젝트 설명

### 파일 구조

각 파일이 Specification 패턴에서 어떤 역할을 하는지 확인하세요.

| 파일 | 역할 |
|------|------|
| `CustomerId.cs` | Ulid 기반 고객 식별자 |
| `Customer.cs` | 고객 Aggregate Root (IAuditable) |
| `CustomerDto.cs` | Query 측 DTO |
| `CustomerEmailSpec.cs` | 이메일 Specification (대소문자 무시) |
| `CustomerNameSpec.cs` | 이름 Specification (부분 일치) |
| `ICustomerRepository.cs` | Repository + Exists(Specification) |
| `InMemoryCustomerRepository.cs` | InMemory Repository 구현 |
| `InMemoryCustomerQuery.cs` | InMemory Query Adapter |

---

## 한눈에 보는 정리

이 예제에서 사용된 Specification 패턴 요소를 정리하면 다음과 같습니다.

| 개념 | 구현 |
|------|------|
| Specification | `CustomerEmailSpec`, `CustomerNameSpec` |
| 조합 | `spec1 & spec2` (And), `spec1 \| spec2` (Or) |
| 항등원 | `Specification<Customer>.All` |
| Exists 검증 | `ICustomerRepository.Exists(spec)` |
| Query Adapter | `InMemoryCustomerQuery : InMemoryQueryBase<Customer, CustomerDto>` |
| 감사 추적 | `IAuditable` → `CreatedAt`, `UpdatedAt` |

---

## FAQ

### Q1: Specification을 ExpressionSpecification으로 구현하지 않는 이유는?
**A**: 이 예제는 InMemory 환경이므로 `IsSatisfiedBy()`만 필요합니다. EF Core/Dapper와 연동할 때는 `ExpressionSpecification<T>`을 사용하여 SQL 자동 번역을 지원합니다.

### Q2: 동적 필터에서 All을 시드로 사용하는 이유는?
**A**: `Specification<T>.All`은 And 연산의 항등원입니다. `All & X = X`이므로 조건이 하나도 추가되지 않으면 모든 데이터를 반환합니다. 이 패턴은 nullable 필터 매개변수를 깔끔하게 처리합니다.

### Q3: Exists()를 별도 메서드로 제공하는 이유는?
**A**: `GetById()` 후 null 체크보다 의도가 명확하고, 전체 Aggregate를 로드하지 않아 성능상 유리합니다. 실제 DB 환경에서는 `SELECT COUNT(1)` 같은 경량 쿼리로 변환됩니다.

---

고객 관리와 Specification 패턴을 완성했습니다. 다음은 재고 관리입니다. 상품을 삭제하면 관련 주문 이력도 함께 사라질까요? 다음 장에서 소프트 삭제 패턴으로 데이터를 보존하면서 삭제하는 방법을 살펴봅니다.

→ [3장: 재고 관리](../03-Inventory-Management/)
