---
title: "인메모리 쿼리 어댑터"
---
## 개요

Dapper 연동 전에 Query Adapter를 검증하려면 어떻게 해야 할까요? 데이터베이스를 띄우지 않고도 Specification 필터링, 페이지네이션, 정렬이 올바르게 동작하는지 확인하고 싶습니다. InMemoryQueryBase<TEntity, TDto>는 메모리 내 데이터 소스에 대한 Query Adapter의 공통 인프라로, 서브클래스가 세 가지만 구현하면 Search, SearchByCursor, Stream이 자동으로 동작합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. InMemoryQueryBase의 Template Method 패턴 구조를 설명할 수 있습니다
2. GetProjectedItems에서 Specification.IsSatisfiedBy를 사용하여 필터링할 수 있습니다
3. SortSelector로 정렬 키를 매핑하는 방법을 적용할 수 있습니다
4. 테스트용 InMemory Query Adapter를 구현할 수 있습니다

---

## 핵심 개념

### Template Method 패턴

InMemoryQueryBase는 Search/SearchByCursor/Stream의 알고리즘을 정의하고, 서브클래스에 세 가지를 위임합니다. 서브클래스는 "무엇을 필터링하고, 어떻게 프로젝션하고, 어떤 키로 정렬할지"만 결정하면 됩니다.

| 추상 멤버 | 역할 |
|-----------|------|
| `DefaultSortField` | 정렬 미지정 시 기본 정렬 필드 |
| `GetProjectedItems(spec)` | Specification 필터링 + DTO 프로젝션 |
| `SortSelector(fieldName)` | 필드명 → 정렬 키 셀렉터 함수 |

### Specification 기반 필터링

GetProjectedItems에서 Specification으로 필터링한 뒤 DTO로 프로젝션하는 과정을 보세요.

```csharp
protected override IEnumerable<ProductDto> GetProjectedItems(Specification<Product> spec) =>
    _store.Values
        .Where(p => spec.IsSatisfiedBy(p))  // Specification으로 필터링
        .Select(p => new ProductDto(...));   // DTO로 프로젝션
```

Specification이 필터 조건을 캡슐화하므로, Query Adapter는 필터 로직을 알 필요 없이 `IsSatisfiedBy`만 호출합니다.

### InStockSpec 예시

```csharp
public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.IsInStock;
}
```

Specification.All(항등원)은 모든 엔터티를 만족하므로 전체 조회에 사용됩니다.

---

## 프로젝트 설명

### InMemoryProductQuery

InMemoryQueryBase<Product, ProductDto>를 상속한 구체 Query Adapter입니다. ConcurrentDictionary를 내부 저장소로 사용하며, Add 메서드로 테스트 데이터를 추가합니다.

### InStockSpec

`Product.IsInStock`을 검사하는 단순 Specification입니다. `Specification<Product>.All`과 조합하여 사용할 수 있습니다.

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| InMemoryQueryBase | InMemory Query Adapter의 공통 베이스 |
| GetProjectedItems | 필터링 + DTO 프로젝션 (서브클래스 구현) |
| SortSelector | 필드명 → 정렬 키 셀렉터 (서브클래스 구현) |
| DefaultSortField | 기본 정렬 필드명 (서브클래스 구현) |
| IsSatisfiedBy | Specification의 핵심 메서드 — 엔터티 평가 |

---

## FAQ

### Q1: InMemoryQueryBase는 프로덕션에서도 사용하나요?
**A**: 주로 테스트와 프로토타이핑에 사용합니다. 프로덕션에서는 DapperQueryBase나 EF Core 기반 구현을 사용합니다. IQueryPort 인터페이스 덕분에 어댑터를 쉽게 교체할 수 있습니다.

### Q2: GetProjectedItems에서 JOIN이 필요하면 어떻게 하나요?
**A**: 여러 저장소를 주입받아 LINQ Join으로 처리합니다. InMemory 환경에서는 이것이 SQL JOIN의 대응입니다.

### Q3: ConcurrentDictionary를 사용하는 이유는?
**A**: 멀티스레드 환경에서의 안전성을 위해서입니다. 단위 테스트에서는 일반 Dictionary도 충분하지만, 통합 테스트나 병렬 테스트에서는 ConcurrentDictionary가 안전합니다.

---

InMemory 구현으로 DB 없이 빠른 테스트가 가능해졌습니다. 프로덕션에서 SQL을 직접 제어하려면 어떻게 해야 할까요? 다음 장에서는 Dapper Query Adapter를 통해 SQL 기반 조회를 구현하는 방법을 살펴봅니다.
