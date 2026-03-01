# Part 3 - Chapter 9: IQueryPort Interface

> **Part 3: Query 측 — 읽기 전용 패턴** | [← 이전: 8장 Unit of Work →](../../Part2-Command-Repository/04-Unit-Of-Work/) | [다음: 10장 DTO Separation →](../02-DTO-Separation/)

---

## 개요

IQueryPort<TEntity, TDto>는 CQRS Query 측의 핵심 인터페이스입니다. Command 측이 Repository를 통해 쓰기를 수행하는 것과 대칭적으로, Query 측은 IQueryPort를 통해 읽기 전용 조회를 수행합니다. 이 인터페이스는 Specification 기반 필터링과 페이지네이션을 지원하며, 도메인 엔터티 대신 DTO를 반환합니다.

---

## 학습 목표

- IQueryPort<TEntity, TDto>의 두 가지 타입 파라미터 역할 이해
- Search, SearchByCursor, Stream 세 가지 조회 메서드의 차이점 파악
- PagedResult<T>와 CursorPagedResult<T> 반환 타입의 구조 이해
- Query Port가 도메인 엔터티가 아닌 DTO를 반환하는 이유 이해

---

## 핵심 개념

### 타입 파라미터

| 파라미터 | 역할 | 예시 |
|----------|------|------|
| `TEntity` | Specification 필터링 대상 (도메인 엔터티) | `Product` |
| `TDto` | 클라이언트에 반환할 읽기 전용 프로젝션 | `ProductDto` |

### 세 가지 조회 메서드

| 메서드 | 반환 타입 | 용도 |
|--------|-----------|------|
| `Search` | `FinT<IO, PagedResult<TDto>>` | Offset 기반 페이지네이션 |
| `SearchByCursor` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset 기반 페이지네이션 |
| `Stream` | `IAsyncEnumerable<TDto>` | 대량 데이터 스트리밍 |

### Command 측 vs Query 측

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

**Q: IQueryPort가 IRepository와 별도로 존재하는 이유는?**
A: CQRS 원칙에 따라 Command(쓰기)와 Query(읽기)를 분리합니다. Repository는 도메인 엔터티의 영속성을 담당하고, QueryPort는 읽기 전용 프로젝션을 담당합니다. 이 분리를 통해 각각 독립적으로 최적화할 수 있습니다.

**Q: DTO를 반환하는 이유는?**
A: 도메인 엔터티를 직접 반환하면 (1) 불필요한 도메인 로직이 클라이언트에 노출되고, (2) N+1 문제가 발생하며, (3) 읽기 최적화가 어렵습니다. DTO를 사용하면 필요한 필드만 조회하고, JOIN을 통해 한 번의 쿼리로 필요한 데이터를 모두 가져올 수 있습니다.

**Q: FinT<IO, T>는 무엇인가요?**
A: LanguageExt의 모나드 트랜스포머로, IO 효과(부수 효과)와 Fin<T>(성공/실패 결과)를 합성합니다. 데이터베이스 조회같은 부수 효과가 있는 연산의 결과를 안전하게 표현합니다.
