---
title: "고객 관리"
---

> **Part 5: 도메인별 실전 예제** | [← 이전: 19장 E-commerce Order Management →](../01-Ecommerce-Order-Management) | [다음: 21장 Inventory Management →](../03-Inventory-Management)

---

## 개요

고객(Customer) 도메인을 통해 Specification 패턴을 활용한 검색과 Repository의 `Exists()` 메서드를 구현합니다. IAuditable 인터페이스로 생성/수정 시각을 추적하고, 동적 필터 빌더 패턴을 보여줍니다.

---

## 학습 목표

- **Specification 패턴**으로 검색 조건 캡슐화
- **Specification 조합** (And, Or, Not 연산자)
- **Repository.Exists()** 로 유일성 검증
- **IAuditable** 인터페이스 구현
- **InMemoryQueryBase** 기반 Query Adapter 구현
- **동적 필터 빌더** (All + 조건부 체이닝)

---

## 핵심 개념

### Specification 패턴

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

### Repository에서 Specification 활용

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

**Q: Specification을 ExpressionSpecification으로 구현하지 않는 이유는?**
A: 이 예제는 InMemory 환경이므로 `IsSatisfiedBy()`만 필요합니다. EF Core/Dapper와 연동할 때는 `ExpressionSpecification<T>`을 사용하여 SQL 자동 번역을 지원합니다.

**Q: 동적 필터에서 All을 시드로 사용하는 이유는?**
A: `Specification<T>.All`은 And 연산의 항등원입니다. `All & X = X`이므로 조건이 하나도 추가되지 않으면 모든 데이터를 반환합니다. 이 패턴은 nullable 필터 매개변수를 깔끔하게 처리합니다.

**Q: Exists()를 별도 메서드로 제공하는 이유는?**
A: `GetById()` 후 null 체크보다 의도가 명확하고, 전체 Aggregate를 로드하지 않아 성능상 유리합니다. 실제 DB 환경에서는 `SELECT COUNT(1)` 같은 경량 쿼리로 변환됩니다.
