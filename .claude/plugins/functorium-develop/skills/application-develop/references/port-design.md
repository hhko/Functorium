# Functorium 포트 설계 가이드

애플리케이션 레이어에서 사용하는 Port 인터페이스의 설계 원칙과 구현 패턴입니다.

---

## 1. 포트 분류 체계

| 포트 타입 | 인터페이스 | Layer | 용도 |
|-----------|-----------|-------|------|
| Write Port | `IRepository<T, TId>` | Domain | Aggregate 단위 CRUD |
| Read Port (제네릭) | `IQueryPort<TEntity, TDto>` | Application | Specification 기반 페이지네이션 검색 |
| Read Port (커스텀) | `IQueryPort` (마커) | Application | 특화 조회 (단건, 조인 등) |
| External Port | `IObservablePort` 기반 | Application | 외부 시스템 연동 |
| Persistence Port | `IUnitOfWork` | Application | 트랜잭션 관리 |

---

## 2. IRepository\<TAggregate, TId\> — Write Port

Domain Layer에 위치하는 Aggregate 단위 Repository 인터페이스입니다.

### 기본 인터페이스

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // 단건 CRUD
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // 벌크 CRUD
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

### 도메인별 확장

```csharp
// Domain Layer에 정의
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// Specification 기반 존재 여부 확인
    FinT<IO, bool> Exists(Specification<Product> spec);

    /// 삭제된 상품 포함 조회 (Restore용)
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

### 핵심 설계 원칙

- **Aggregate Root 제약**: `where TAggregate : AggregateRoot<TId>`로 Entity 직접 저장 방지
- **IEntityId 제약**: Ulid 기반 ID만 허용
- **FinT\<IO, T\> 반환**: LINQ 모나드 합성 지원
- **IObservablePort 상속**: OpenTelemetry 관측 자동 지원
- **Domain Layer 위치**: Aggregate와 같은 Layer에 정의

---

## 3. IQueryPort\<TEntity, TDto\> — 제네릭 Read Port

Application Layer에 위치하는 Specification 기반 검색 포트입니다.

### 기본 인터페이스

```csharp
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    /// Offset 기반 페이지네이션 검색
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);

    /// Keyset(Cursor) 기반 페이지네이션 검색 (deep page O(1))
    FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec,
        CursorPageRequest cursor,
        SortExpression sort);

    /// 스트리밍 조회 (대량 데이터용)
    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        CancellationToken cancellationToken = default);
}
```

### 도메인별 구현

```csharp
// Application Layer에 정의
public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price);
```

### 페이지네이션 타입

```csharp
// Offset 기반
public record PageRequest(int Page = 1, int PageSize = DefaultPageSize);
public record PagedResult<T>(
    IReadOnlyList<T> Items, int TotalCount,
    int Page, int PageSize, int TotalPages,
    bool HasNextPage, bool HasPreviousPage);

// Cursor 기반
public record CursorPageRequest(string? After, int PageSize = DefaultPageSize);
public record CursorPagedResult<T>(
    IReadOnlyList<T> Items, string? NextCursor, bool HasNextPage);
```

---

## 4. 커스텀 Read Port — 특화 조회

제네릭 `IQueryPort<T, TDto>`에 맞지 않는 특화 조회용 포트입니다.

### 단건 조회 포트

```csharp
// Application Layer에 정의
public interface IProductDetailQuery : IQueryPort  // 마커 인터페이스만 상속
{
    FinT<IO, ProductDetailDto> GetById(ProductId id);
}

public sealed record ProductDetailDto(
    string ProductId,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAt,
    Option<DateTime> UpdatedAt);
```

### 교차 Aggregate 조회 포트

```csharp
// 주문에서 상품 가격을 조회하는 공유 포트
public interface IProductCatalog : IQueryPort
{
    FinT<IO, Seq<ProductPriceDto>> GetPricesForProducts(IReadOnlyList<ProductId> productIds);
}

public sealed record ProductPriceDto(ProductId Id, Money Price);
```

### 커스텀 포트 설계 원칙

- `IQueryPort` 마커 인터페이스 상속 (관측 자동 지원)
- DTO는 포트와 같은 파일에 정의
- Aggregate 재구성 없이 DB -> DTO 직접 프로젝션
- Application Layer에 위치 (Query Usecase에서 사용)

---

## 5. External Port — 외부 시스템 연동

외부 API, 메시지 큐 등과 연동하는 포트입니다.

### IExternalPricingService 예제

```csharp
// Application Layer에 정의
public interface IExternalPricingService : IObservablePort
{
    /// 외부 API에서 상품 가격 조회
    FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken);

    /// 일괄 가격 조회
    FinT<IO, Map<string, Money>> GetPricesAsync(
        Seq<string> productCodes, CancellationToken cancellationToken);
}
```

### External Port 설계 원칙

- `IObservablePort` 직접 상속 (Repository/QueryPort 계층 밖)
- `FinT<IO, T>` 반환으로 LINQ 합성 호환
- `CancellationToken` 파라미터 포함
- DTO는 포트와 같은 파일에 정의

---

## 6. IUnitOfWork — 트랜잭션 관리

명시적 트랜잭션이 필요한 경우 사용합니다.

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);

    /// ExecuteDeleteAsync/ExecuteUpdateAsync 등을 하나의 트랜잭션으로 묶을 때
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 사용 시점

- 단일 Repository 작업: 불필요 (EF Core SaveChanges가 자동 트랜잭션)
- 여러 Repository 작업: `IUnitOfWork.SaveChanges()` 사용
- 즉시 실행 SQL + SaveChanges 혼합: `BeginTransactionAsync()` 사용

---

## 7. IObservablePort — 관측 마커

모든 Port의 공통 마커 인터페이스입니다.

```csharp
public interface IObservablePort
{
    string RequestCategory { get; }
}
```

OpenTelemetry 관측 데코레이터가 이 인터페이스를 감지하여 자동으로 로깅/메트릭/트레이싱을 추가합니다.
Adapter 구현 내부에서 개발자가 직접 로그를 출력하려면 `ObservableSignal.Debug/Warning/Error`를 사용합니다. Observable 래퍼가 설정한 공통 컨텍스트(layer, category, handler, method)가 자동으로 포함됩니다.

---

## 8. 포트 위치 결정

| 조건 | Layer | 이유 |
|------|-------|------|
| Aggregate 단위 CRUD | Domain | Aggregate와 동일 경계 |
| Specification 기반 검색 | Application | Usecase 요구사항에 종속 |
| 커스텀 조회 (DTO 프로젝션) | Application | DTO가 Usecase 소속 |
| 외부 시스템 연동 | Application | Infrastructure 구현의 추상화 |
| 트랜잭션 관리 | Application | Usecase 오케스트레이션 영역 |

### 예외

- Repository 확장 메서드(`Exists`, `GetByIdIncludingDeleted` 등)는 Domain Layer의 Repository 인터페이스에 정의
- 이유: Aggregate의 도메인 불변식과 직결되는 쿼리이므로

---

## 9. CQRS 포트 분리 요약

```
Command 흐름:
  Request -> Validator -> Usecase -> IRepository<T, TId> -> Aggregate -> Response

Query 흐름:
  Request -> Validator -> Usecase -> IQueryPort<T, TDto> -> DTO -> Response
                                  또는 IXxxDetailQuery -> DTO -> Response

Event 흐름:
  DomainEvent -> IDomainEventHandler<T.Event> -> (로깅, 후속 Command 등)
```

### Command vs Query 포트 선택

| 기준 | Command (IRepository) | Query (IQueryPort) |
|------|----------------------|-------------------|
| 데이터 형태 | Aggregate 전체 | DTO 프로젝션 |
| 상태 변경 | O | X |
| 이벤트 발행 | O (Aggregate에서) | X |
| SQL 최적화 | 불필요 (전체 로드) | 중요 (필요 컬럼만) |
| Specification | 존재 확인, 유니크 검증 | 검색 필터 |

---

## 6. IDomainEventCollector

도메인 이벤트를 수집하는 인터페이스입니다. Repository의 Create/Update에서 Aggregate를 추적하고,
Domain Service의 벌크 이벤트를 직접 추적합니다.

```csharp
public interface IDomainEventCollector
{
    // Aggregate 추적 (Repository에서 호출)
    void Track(IHasDomainEvents aggregate);
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();

    // Domain Service 벌크 이벤트 직접 추적
    void TrackEvent(IDomainEvent domainEvent);
    IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents();
}
```

### 사용 패턴

| 메서드 | 호출자 | 용도 |
|--------|--------|------|
| `Track(aggregate)` | Repository (Create/Update) | Aggregate 내부 이벤트 수집 |
| `TrackRange(aggregates)` | Repository (CreateRange/UpdateRange) | 벌크 Aggregate 이벤트 수집 |
| `TrackEvent(domainEvent)` | Use Case (Domain Service 결과) | 벌크 이벤트 직접 추적 |
| `GetTrackedAggregates()` | Publisher | Aggregate 이벤트 수집 + ClearDomainEvents |
| `GetDirectlyTrackedEvents()` | Publisher | Domain Service 이벤트 수집 |

### Domain Service 벌크 이벤트 흐름

```
Use Case
  → productRepository.GetByIds(ids)         // Aggregate 로드
  → ProductBulkOperations.BulkDelete(...)    // Domain Service: 상태 변경 + 벌크 이벤트 생성
  → eventCollector.TrackEvent(bulkEvent)     // 벌크 이벤트 직접 추적
  → productRepository.UpdateRange(...)       // 영속화
  → Publisher.PublishTrackedEvents()          // Aggregate + 직접 추적 이벤트 모두 발행
```
