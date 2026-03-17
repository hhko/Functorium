---
title: "IQueryPort 인터페이스"
---
## 개요

Repository의 `GetById`로 주문 목록을 조회하면 어떻게 될까요? Aggregate를 하나씩 로드한 뒤 메모리에서 필터링해야 합니다. 목록 조회, 검색, 페이지네이션처럼 **읽기에 특화된 요구사항을** Repository로 해결하려 하면 비효율이 누적됩니다. IQueryPort<TEntity, TDto>는 이 문제를 해결하는 CQRS Query 측의 핵심 인터페이스입니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. IQueryPort<TEntity, TDto>의 두 가지 타입 파라미터 역할을 설명할 수 있습니다
2. Search, SearchByCursor, Stream 세 가지 조회 메서드를 용도에 맞게 선택할 수 있습니다
3. PagedResult<T>와 CursorPagedResult<T> 반환 타입의 구조를 이해할 수 있습니다
4. Query Port가 도메인 엔터티가 아닌 DTO를 반환하는 이유를 설명할 수 있습니다

---

## 핵심 개념

### "왜 필요한가?" — Repository 기반 읽기의 한계

Repository는 단건 조회에 최적화되어 있습니다. "재고 있는 상품 목록을 가격순으로 20개씩 보여주세요"라는 요청을 Repository로 처리한다고 생각해 보세요.

```csharp
// Repository로 목록을 조회하려면?
var allProducts = await repository.GetAll();      // 모든 Aggregate를 메모리로 로드
var filtered = allProducts
    .Where(p => p.IsInStock)                      // 메모리에서 필터링
    .OrderBy(p => p.Price)                        // 메모리에서 정렬
    .Skip(20).Take(20);                           // 메모리에서 페이지네이션
```

모든 Aggregate를 로드한 뒤 메모리에서 필터링하고, 정렬하고, 잘라냅니다. 데이터가 늘어날수록 성능은 급격히 저하됩니다. **읽기 전용 인터페이스가** 필요한 이유입니다.

### 타입 파라미터

IQueryPort는 두 가지 타입 파라미터로 필터링 대상과 반환 타입을 분리합니다.

| 파라미터 | 역할 | 예시 |
|----------|------|------|
| `TEntity` | Specification 필터링 대상 (도메인 엔터티) | `Product` |
| `TDto` | 클라이언트에 반환할 읽기 전용 프로젝션 | `ProductDto` |

### 세 가지 조회 메서드

데이터 양과 사용 패턴에 따라 적합한 조회 방식이 다릅니다. IQueryPort는 세 가지 메서드를 모두 제공합니다.

| 메서드 | 반환 타입 | 용도 |
|--------|-----------|------|
| `Search` | `FinT<IO, PagedResult<TDto>>` | Offset 기반 페이지네이션 |
| `SearchByCursor` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset 기반 페이지네이션 |
| `Stream` | `IAsyncEnumerable<TDto>` | 대량 데이터 스트리밍 |

### IObservablePort

IQueryPort는 IRepository와 마찬가지로 `IObservablePort`를 상속합니다. Query 측 구현체는 `RequestCategory => "Query"`를 반환하여, Observability 파이프라인이 Command와 Query의 메트릭을 별도로 수집할 수 있게 합니다.

### Command 측 vs Query 측

CQRS에서 쓰기와 읽기는 서로 다른 경로를 탑니다. 아래 표에서 두 경로가 어떻게 대칭을 이루는지 확인해 보세요.

| 구분 | Command 측 | Query 측 |
|------|-----------|----------|
| Port | IRepository<TEntity> | IQueryPort<TEntity, TDto> |
| 반환 | Entity (도메인 모델) | DTO (읽기 전용 프로젝션) |
| 목적 | 상태 변경 (CUD) | 데이터 조회 (R) |
| 필터링 | ID 기반 | Specification 기반 |

---

## 프로젝트 설명

### ProductId / Product

Ulid 기반 EntityId와 AggregateRoot를 상속한 도메인 엔터티입니다. Specification<Product>의 TEntity로 사용됩니다.

### ProductDto

읽기 전용 record로, 도메인 엔터티의 모든 필드가 아닌 클라이언트가 필요로 하는 필드만 포함합니다.

### IProductQuery

```csharp
public interface IProductQuery : IQueryPort<Product, ProductDto> { }
```

Product 도메인 전용 Query Port입니다. IQueryPort<Product, ProductDto>를 확장하여 세 가지 조회 메서드를 상속받습니다.

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| IQueryPort | CQRS Query 측 포트 인터페이스 |
| TEntity | Specification 필터링 대상 (도메인 엔터티) |
| TDto | 클라이언트 반환용 읽기 전용 프로젝션 |
| Search | Offset 기반 페이지네이션 (PagedResult) |
| SearchByCursor | Keyset 기반 페이지네이션 (CursorPagedResult) |
| Stream | 대량 데이터 스트리밍 (IAsyncEnumerable) |

---

## FAQ

### Q1: IQueryPort가 IRepository와 별도로 존재하는 이유는?
**A**: CQRS 원칙에 따라 Command(쓰기)와 Query(읽기)를 분리합니다. Repository는 도메인 엔터티의 영속성을 담당하고, QueryPort는 읽기 전용 프로젝션을 담당합니다. 이 분리를 통해 각각 독립적으로 최적화할 수 있습니다.

### Q2: DTO를 반환하는 이유는?
**A**: 도메인 엔터티를 직접 반환하면 (1) 불필요한 도메인 로직이 클라이언트에 노출되고, (2) N+1 문제가 발생하며, (3) 읽기 최적화가 어렵습니다. DTO를 사용하면 필요한 필드만 조회하고, JOIN을 통해 한 번의 쿼리로 필요한 데이터를 모두 가져올 수 있습니다.

### Q3: FinT<IO, T>는 무엇인가요?
**A**: LanguageExt의 모나드 트랜스포머로, IO 효과(부수 효과)와 Fin<T>(성공/실패 결과)를 합성합니다. 데이터베이스 조회같은 부수 효과가 있는 연산의 결과를 안전하게 표현합니다.

---

읽기 전용 인터페이스를 정의했습니다. 그런데 Command에서 쓰는 `Order`와 Query에서 반환하는 `OrderDto`가 같은 클래스여야 할까요? 다음 장에서는 Command DTO와 Query DTO를 분리하는 설계 기준을 살펴봅니다.
