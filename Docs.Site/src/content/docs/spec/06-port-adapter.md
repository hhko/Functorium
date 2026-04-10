---
title: "포트와 어댑터 사양"
---

Functorium 프레임워크가 제공하는 포트(Port) 인터페이스, 어댑터(Adapter) 구현 베이스 클래스, Specification 패턴, DI 등록, 소스 생성기 어트리뷰트의 API 사양입니다. 설계 원칙과 구현 가이드는 [Port 아키텍처와 정의](../guides/adapter/12-ports)와 [Adapter 구현](../guides/adapter/13-adapters)을 참조하십시오.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `IObservablePort` | `Functorium.Abstractions.Observabilities` | 모든 Port/Adapter의 기반 인터페이스 (관측성 카테고리 제공) |
| `IRepository<TAggregate, TId>` | `Functorium.Domains.Repositories` | Aggregate Root 단위 Repository CRUD 계약 |
| `IQueryPort` | `Functorium.Applications.Queries` | 비제네릭 QueryPort 마커 인터페이스 |
| `IQueryPort<TEntity, TDto>` | `Functorium.Applications.Queries` | Specification 기반 조회 + 페이지네이션 계약 |
| `PageRequest` | `Functorium.Applications.Queries` | Offset 기반 페이지네이션 요청 |
| `PagedResult<T>` | `Functorium.Applications.Queries` | Offset 기반 페이지네이션 결과 |
| `CursorPageRequest` | `Functorium.Applications.Queries` | Keyset(Cursor) 기반 페이지네이션 요청 |
| `CursorPagedResult<T>` | `Functorium.Applications.Queries` | Keyset(Cursor) 기반 페이지네이션 결과 |
| `SortExpression` | `Functorium.Applications.Queries` | 다중 필드 정렬 표현 |
| `SortField` | `Functorium.Applications.Queries` | 정렬 필드 + 방향 쌍 |
| `SortDirection` | `Functorium.Applications.Queries` | 정렬 방향 SmartEnum (`Ascending`, `Descending`) |
| `Specification<T>` | `Functorium.Domains.Specifications` | Specification 패턴 추상 기반 클래스 |
| `ExpressionSpecification<T>` | `Functorium.Domains.Specifications` | Expression Tree 기반 Specification 추상 클래스 |
| `IExpressionSpec<T>` | `Functorium.Domains.Specifications` | Expression Tree 제공 능력을 나타내는 인터페이스 |
| `PropertyMap<TEntity, TModel>` | `Functorium.Domains.Specifications.Expressions` | Entity-Model 간 Expression 자동 변환 프로퍼티 매핑 |
| `SpecificationExpressionResolver` | `Functorium.Domains.Specifications.Expressions` | Specification에서 Expression Tree를 추출/합성하는 유틸리티 |
| `EfCoreRepositoryBase<TAggregate, TId, TModel>` | `Functorium.Adapters.Repositories` | EF Core Repository 공통 베이스 클래스 |
| `InMemoryRepositoryBase<TAggregate, TId>` | `Functorium.Adapters.Repositories` | InMemory Repository 공통 베이스 클래스 |
| `DapperQueryBase<TEntity, TDto>` | `Functorium.Adapters.Repositories` | Dapper 기반 QueryAdapter 공통 베이스 클래스 |
| `InMemoryQueryBase<TEntity, TDto>` | `Functorium.Adapters.Repositories` | InMemory QueryAdapter 공통 베이스 클래스 |
| `DapperSpecTranslator<TEntity>` | `Functorium.Adapters.Repositories` | Specification → SQL WHERE 절 번역 레지스트리 |
| `IHasStringId` | `Functorium.Adapters.Repositories` | EF Core 모델의 string Id 공통 인터페이스 |
| `ObservablePortRegistration` | `Functorium.Abstractions.Registrations` | Observable Port DI 등록 확장 메서드 |
| `OptionsConfigurator` | `Functorium.Adapters.Abstractions.Options` | FluentValidation 기반 옵션 유효성 검사 등록 |
| `GenerateObservablePortAttribute` | `Functorium.Adapters.SourceGenerators` | Observable 래퍼 자동 생성 어트리뷰트 |
| `ObservablePortIgnoreAttribute` | `Functorium.Adapters.SourceGenerators` | Observable 래퍼 생성 제외 어트리뷰트 |

---

## IObservablePort 인터페이스

모든 Port와 Adapter가 구현하는 기반 인터페이스입니다. `RequestCategory` 속성을 통해 관측성(Observability) 레이어에서 요청 카테고리를 식별합니다.

```csharp
namespace Functorium.Abstractions.Observabilities;

public interface IObservablePort
{
    string RequestCategory { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `RequestCategory` | `string` | 관측성 로그/메트릭에서 사용할 카테고리 (예: `"Repository"`, `"ExternalApi"`, `"Messaging"`) |

### 인터페이스 계층

```
IObservablePort
├── IRepository<TAggregate, TId>    — Aggregate Root CRUD (Domain Layer)
├── IQueryPort                      — 비제네릭 마커 (Application Layer)
│   └── IQueryPort<TEntity, TDto>   — Specification 기반 조회 (Application Layer)
└── (사용자 정의 Port)               — External API, Messaging 등
```

`IObservablePort`를 상속하면 `[GenerateObservablePort]` 소스 생성기가 Tracing, Logging, Metrics를 자동 생성할 수 있습니다.

---

## Repository 계약 (IRepository\<TAggregate, TId\>)

Aggregate Root 단위의 영속화 계약입니다. 제네릭 제약을 통해 Aggregate Root 단위 영속화를 컴파일 타임에 강제합니다.

```csharp
namespace Functorium.Domains.Repositories;

public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

### 제네릭 제약

| 타입 파라미터 | 제약 | 설명 |
|---------------|------|------|
| `TAggregate` | `AggregateRoot<TId>` | Aggregate Root만 Repository 대상 (Entity 직접 영속화 방지) |
| `TId` | `struct, IEntityId<TId>` | Ulid 기반 EntityId 구현 타입 |

### 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `Create(aggregate)` | `FinT<IO, TAggregate>` | 단일 Aggregate 생성 |
| `GetById(id)` | `FinT<IO, TAggregate>` | ID로 Aggregate 조회 (없으면 `NotFound` 에러) |
| `Update(aggregate)` | `FinT<IO, TAggregate>` | 단일 Aggregate 업데이트 |
| `Delete(id)` | `FinT<IO, int>` | ID로 Aggregate 삭제 (삭제된 건수 반환) |
| `CreateRange(aggregates)` | `FinT<IO, Seq<TAggregate>>` | 일괄 생성 |
| `GetByIds(ids)` | `FinT<IO, Seq<TAggregate>>` | 일괄 조회 (일부 누락 시 `PartialNotFound` 에러) |
| `UpdateRange(aggregates)` | `FinT<IO, Seq<TAggregate>>` | 일괄 업데이트 |
| `DeleteRange(ids)` | `FinT<IO, int>` | 일괄 삭제 (삭제된 건수 반환) |

> 모든 메서드는 `FinT<IO, T>`를 반환합니다. 성공은 `Fin.Succ(value)`, 실패는 도메인/어댑터 에러로 표현됩니다.

---

## QueryPort 계약 (IQueryPort\<TEntity, TDto\>)

Specification 기반 조회와 DTO 직접 반환을 위한 읽기 전용 포트입니다. CQRS의 Read 모델에 해당합니다.

### IQueryPort (비제네릭 마커)

```csharp
namespace Functorium.Applications.Queries;

public interface IQueryPort : IObservablePort { }
```

런타임 타입 체크, DI 스캐닝, 제네릭 제약에 활용하는 비제네릭 마커 인터페이스입니다.

### IQueryPort\<TEntity, TDto\>

```csharp
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);

    FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec,
        CursorPageRequest cursor,
        SortExpression sort);

    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        CancellationToken cancellationToken = default);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset 기반 페이지네이션 검색 |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset(Cursor) 기반 페이지네이션 검색. deep page에서 O(1) 성능 |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | 대량 데이터 스트리밍 조회. 메모리에 전체 적재하지 않고 yield |

### 제네릭 파라미터

| 타입 파라미터 | 설명 |
|---------------|------|
| `TEntity` | 도메인 엔터티 타입 (Specification 대상) |
| `TDto` | 반환 DTO 타입 (프레젠테이션 계층에 직접 반환) |

---

## 페이지네이션 타입

### PageRequest

Offset 기반 페이지네이션 요청입니다. Application 레벨 쿼리 관심사로 도메인 불변식이 아닙니다.

```csharp
namespace Functorium.Applications.Queries;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 10_000;

    public int Page { get; }
    public int PageSize { get; }
    public int Skip => (Page - 1) * PageSize;

    public PageRequest(int page = 1, int pageSize = DefaultPageSize);
}
```

| 속성/상수 | 타입 | 설명 |
|-----------|------|------|
| `DefaultPageSize` | `int` | 기본 페이지 크기 (`20`) |
| `MaxPageSize` | `int` | 최대 페이지 크기 (`10,000`) |
| `Page` | `int` | 현재 페이지 번호 (1 미만이면 `1`로 보정) |
| `PageSize` | `int` | 페이지 크기 (1 미만이면 `DefaultPageSize`, 초과 시 `MaxPageSize`로 보정) |
| `Skip` | `int` | 건너뛸 항목 수 (계산 속성: `(Page - 1) * PageSize`) |

### PagedResult\<T\>

Offset 기반 페이지네이션 결과 컨테이너입니다.

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `Items` | `IReadOnlyList<T>` | 현재 페이지의 항목 목록 |
| `TotalCount` | `int` | 전체 항목 수 |
| `Page` | `int` | 현재 페이지 번호 |
| `PageSize` | `int` | 페이지 크기 |
| `TotalPages` | `int` | 전체 페이지 수 (계산 속성) |
| `HasPreviousPage` | `bool` | 이전 페이지 존재 여부 |
| `HasNextPage` | `bool` | 다음 페이지 존재 여부 |

### CursorPageRequest

Keyset(Cursor) 기반 페이지네이션 요청입니다. Offset 기반 대비 deep page에서 O(1) 성능을 제공합니다.

```csharp
public sealed record CursorPageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 10_000;

    public string? After { get; }
    public string? Before { get; }
    public int PageSize { get; }

    public CursorPageRequest(
        string? after = null,
        string? before = null,
        int pageSize = DefaultPageSize);
}
```

| 속성/상수 | 타입 | 설명 |
|-----------|------|------|
| `DefaultPageSize` | `int` | 기본 페이지 크기 (`20`) |
| `MaxPageSize` | `int` | 최대 페이지 크기 (`10,000`) |
| `After` | `string?` | 이 커서 이후의 항목을 조회 (forward pagination) |
| `Before` | `string?` | 이 커서 이전의 항목을 조회 (backward pagination) |
| `PageSize` | `int` | 페이지 크기 (1 미만이면 `DefaultPageSize`, 초과 시 `MaxPageSize`로 보정) |

### CursorPagedResult\<T\>

Keyset(Cursor) 기반 페이지네이션 결과 컨테이너입니다.

```csharp
public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    string? PrevCursor,
    bool HasMore);
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `Items` | `IReadOnlyList<T>` | 현재 페이지의 항목 목록 |
| `NextCursor` | `string?` | 다음 페이지 커서 (더 이상 항목이 없으면 `null`) |
| `PrevCursor` | `string?` | 이전 페이지 커서 |
| `HasMore` | `bool` | 다음 페이지 존재 여부 |

### SortExpression

다중 필드 정렬 표현입니다. Fluent API로 정렬 조건을 조합합니다.

```csharp
public sealed class SortExpression
{
    public Seq<SortField> Fields { get; }
    public bool IsEmpty { get; }

    public static SortExpression Empty { get; }
    public static SortExpression By(string fieldName);
    public static SortExpression By(string fieldName, SortDirection direction);
    public SortExpression ThenBy(string fieldName);
    public SortExpression ThenBy(string fieldName, SortDirection direction);
}
```

| 멤버 | 타입 | 설명 |
|------|------|------|
| `Fields` | `Seq<SortField>` | 정렬 필드 목록 (순서대로 적용) |
| `IsEmpty` | `bool` | 정렬 조건이 비어있는지 여부 |
| `Empty` | `SortExpression` | 정렬 없음 (정적 속성) |
| `By(fieldName)` | `SortExpression` | 단일 필드 오름차순 정렬 생성 (정적 팩토리) |
| `By(fieldName, direction)` | `SortExpression` | 단일 필드 + 방향 정렬 생성 (정적 팩토리) |
| `ThenBy(fieldName)` | `SortExpression` | 추가 정렬 필드 연결 (오름차순) |
| `ThenBy(fieldName, direction)` | `SortExpression` | 추가 정렬 필드 + 방향 연결 |

### SortField

정렬 필드와 방향의 쌍입니다.

```csharp
public sealed record SortField(string FieldName, SortDirection Direction);
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `FieldName` | `string` | 정렬 대상 필드명 |
| `Direction` | `SortDirection` | 정렬 방향 |

### SortDirection

정렬 방향을 나타내는 SmartEnum입니다.

```csharp
public sealed class SortDirection : SmartEnum<SortDirection, string>
{
    public static readonly SortDirection Ascending;   // Value: "asc"
    public static readonly SortDirection Descending;  // Value: "desc"

    public static SortDirection Parse(string? value);
}
```

| 멤버 | Value | 설명 |
|------|-------|------|
| `Ascending` | `"asc"` | 오름차순 |
| `Descending` | `"desc"` | 내림차순 |
| `Parse(value)` | - | 대소문자 무시하여 `"asc"`/`"desc"` 파싱. `null`/빈 문자열이면 `Ascending` 반환 |

---

## Specification 패턴

### Specification\<T\> (추상 기반 클래스)

도메인 조건을 캡슐화하고 And/Or/Not 조합을 지원하는 추상 기반 클래스입니다.

```csharp
namespace Functorium.Domains.Specifications;

public abstract class Specification<T>
{
    public static Specification<T> All { get; }
    public virtual bool IsAll => false;

    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other);
    public Specification<T> Or(Specification<T> other);
    public Specification<T> Not();

    public static Specification<T> operator &(Specification<T> left, Specification<T> right);
    public static Specification<T> operator |(Specification<T> left, Specification<T> right);
    public static Specification<T> operator !(Specification<T> spec);
}
```

| 멤버 | 타입 | 설명 |
|------|------|------|
| `All` | `Specification<T>` | 모든 엔터티를 만족하는 Specification (Null Object). `All & X = X` |
| `IsAll` | `bool` | 이 Specification이 항등원(`All`)인지 여부 |
| `IsSatisfiedBy(entity)` | `bool` | 엔터티가 조건을 만족하는지 확인 |
| `And(other)` | `Specification<T>` | AND 조합 |
| `Or(other)` | `Specification<T>` | OR 조합 |
| `Not()` | `Specification<T>` | NOT 부정 |

**연산자 오버로드:**

| 연산자 | 동등 메서드 | 설명 |
|--------|-----------|------|
| `&` | `And()` | AND 조합. `All` 항등원 최적화 포함 (`All & X = X`) |
| `\|` | `Or()` | OR 조합 |
| `!` | `Not()` | NOT 부정 |

### ExpressionSpecification\<T\>

Expression Tree 기반 Specification 추상 클래스입니다. `ToExpression()`을 구현하면 `IsSatisfiedBy()`가 자동으로 제공됩니다.

```csharp
public abstract class ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    public sealed override bool IsSatisfiedBy(T entity);  // Expression 컴파일 + 캐싱
}
```

| 멤버 | 설명 |
|------|------|
| `ToExpression()` | 조건을 `Expression<Func<T, bool>>`로 반환 (서브클래스 필수 구현) |
| `IsSatisfiedBy(entity)` | Expression을 컴파일하여 평가. 컴파일된 delegate는 캐싱됨 (`sealed`) |

### IExpressionSpec\<T\>

Specification이 Expression Tree를 제공할 수 있음을 나타내는 인터페이스입니다. EF Core 등의 LINQ 프로바이더에서 자동 SQL 번역에 사용됩니다.

```csharp
namespace Functorium.Domains.Specifications;

public interface IExpressionSpec<T>
{
    Expression<Func<T, bool>> ToExpression();
}
```

### PropertyMap\<TEntity, TModel\>

Entity Expression을 Model Expression으로 자동 변환하기 위한 프로퍼티 매핑입니다. `EfCoreRepositoryBase`의 `BuildQuery()` 메서드에서 Specification → SQL 변환에 사용됩니다.

```csharp
namespace Functorium.Domains.Specifications.Expressions;

public sealed class PropertyMap<TEntity, TModel>
{
    public PropertyMap<TEntity, TModel> Map<TValue, TModelValue>(
        Expression<Func<TEntity, TValue>> entityProp,
        Expression<Func<TModel, TModelValue>> modelProp);

    public string? TranslateFieldName(string entityFieldName);
    public Expression<Func<TModel, bool>> Translate(Expression<Func<TEntity, bool>> expression);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `Map(entityProp, modelProp)` | `PropertyMap<TEntity, TModel>` | Entity-Model 프로퍼티 매핑 등록. Fluent API |
| `TranslateFieldName(name)` | `string?` | Entity 필드명을 Model 필드명으로 번역 (매핑 없으면 `null`) |
| `Translate(expression)` | `Expression<Func<TModel, bool>>` | Entity Expression을 Model Expression으로 변환 |

**지원하는 Entity 프로퍼티 표현식:**

| 형태 | 예시 |
|------|------|
| 직접 멤버 접근 | `p => p.Name` |
| 타입 변환 | `p => (decimal)p.Price` |
| `ToString()` 호출 | `p => p.Id.ToString()` |

### SpecificationExpressionResolver

Specification에서 Expression Tree를 추출하고 And/Or/Not 조합도 재귀적으로 합성하는 유틸리티입니다.

```csharp
namespace Functorium.Domains.Specifications.Expressions;

public static class SpecificationExpressionResolver
{
    public static Expression<Func<T, bool>>? TryResolve<T>(Specification<T> spec);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `TryResolve(spec)` | `Expression<Func<T, bool>>?` | Specification에서 Expression 추출. `IExpressionSpec` 구현 시 직접 추출, And/Or/Not 조합 시 재귀 합성. 지원 불가 시 `null` 반환 |

---

## 구현 베이스 클래스

### EfCoreRepositoryBase\<TAggregate, TId, TModel\>

EF Core Repository의 공통 베이스 클래스입니다. 생성자에서 선언한 Include가 `ReadQuery()`를 통해 모든 읽기 쿼리에 자동 적용되어 N+1 문제를 구조적으로 방지합니다.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class EfCoreRepositoryBase<TAggregate, TId, TModel>
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
    where TModel : class, IHasStringId
{
    protected EfCoreRepositoryBase(
        IDomainEventCollector eventCollector,
        Func<IQueryable<TModel>, IQueryable<TModel>>? applyIncludes = null,
        PropertyMap<TAggregate, TModel>? propertyMap = null);
}
```

#### 제네릭 제약

| 타입 파라미터 | 제약 | 설명 |
|---------------|------|------|
| `TAggregate` | `AggregateRoot<TId>` | Aggregate Root 타입 |
| `TId` | `struct, IEntityId<TId>` | Ulid 기반 EntityId 타입 |
| `TModel` | `class, IHasStringId` | EF Core 엔티티 모델 (string Id 필수) |

#### 생성자 파라미터

| 파라미터 | 타입 | 설명 |
|----------|------|------|
| `eventCollector` | `IDomainEventCollector` | 도메인 이벤트 수집기 |
| `applyIncludes` | `Func<IQueryable<TModel>, IQueryable<TModel>>?` | Navigation Property Include 선언 (N+1 방지). `null`이면 Include 없음 |
| `propertyMap` | `PropertyMap<TAggregate, TModel>?` | Specification → Model Expression 변환용 매핑. `BuildQuery`/`ExistsBySpec` 사용 시 필수 |

#### 서브클래스 필수 구현

| 멤버 | 타입 | 설명 |
|------|------|------|
| `DbContext` | `DbContext` | EF Core DbContext (abstract property) |
| `DbSet` | `DbSet<TModel>` | 엔티티 모델의 DbSet (abstract property) |
| `ToDomain(model)` | `TAggregate` | Model → Domain 매핑 (abstract method) |
| `ToModel(aggregate)` | `TModel` | Domain → Model 매핑 (abstract method) |

#### 보호된(protected) 인프라 멤버

| 멤버 | 타입 | 설명 |
|------|------|------|
| `EventCollector` | `IDomainEventCollector` | 도메인 이벤트 수집기 |
| `PropertyMap` | `PropertyMap<TAggregate, TModel>?` | Specification → Model 프로퍼티 매핑 |
| `IdBatchSize` | `int` (virtual, 기본 `500`) | SQL IN 절 파라미터 한계 방지를 위한 배치 크기 |
| `ReadQuery()` | `IQueryable<TModel>` | Include가 자동 적용된 읽기 전용 쿼리 (`AsNoTracking`) |
| `ReadQueryIgnoringFilters()` | `IQueryable<TModel>` | Include + 글로벌 필터 무시 읽기 쿼리 (Soft Delete 조회용) |
| `BuildQuery(spec)` | `Fin<IQueryable<TModel>>` | Specification → Model Expression 쿼리 빌더 (PropertyMap 필수) |
| `ExistsBySpec(spec)` | `FinT<IO, bool>` | Specification 기반 존재 여부 확인 (PropertyMap 필수) |
| `ByIdPredicate(id)` | `Expression<Func<TModel, bool>>` | 단일 ID 매칭 Expression (virtual, `IHasStringId` 기반 기본 구현) |
| `ByIdsPredicate(ids)` | `Expression<Func<TModel, bool>>` | 복수 ID 매칭 Expression (virtual, `IHasStringId` 기반 기본 구현) |

#### 에러 헬퍼

| 메서드 | 설명 |
|--------|------|
| `NotFoundError(id)` | `AdapterErrorType.NotFound` 에러 생성. 실제 서브클래스 이름이 에러 코드에 포함 |
| `PartialNotFoundError(requestedIds, foundAggregates)` | `AdapterErrorType.PartialNotFound` 에러 생성. 누락 ID 목록 포함 |
| `NotConfiguredError(message)` | `AdapterErrorType.NotConfigured` 에러 생성 |
| `NotSupportedError(currentValue, message)` | `AdapterErrorType.NotSupported` 에러 생성 |

#### IHasStringId

EF Core 모델이 구현해야 하는 string Id 인터페이스입니다. `EfCoreRepositoryBase`의 `ByIdPredicate`/`ByIdsPredicate` 기본 구현을 제공합니다.

```csharp
namespace Functorium.Adapters.Repositories;

public interface IHasStringId
{
    string Id { get; set; }
}
```

### InMemoryRepositoryBase\<TAggregate, TId\>

InMemory Repository의 공통 베이스 클래스입니다. `ConcurrentDictionary` 기반으로 `IRepository` 전체 CRUD를 기본 구현합니다.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class InMemoryRepositoryBase<TAggregate, TId>
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    protected InMemoryRepositoryBase(IDomainEventCollector eventCollector);
}
```

#### 서브클래스 필수 구현

| 멤버 | 타입 | 설명 |
|------|------|------|
| `Store` | `ConcurrentDictionary<TId, TAggregate>` | 인메모리 저장소 (abstract property). 서브클래스에서 static 인스턴스 제공 |

#### 보호된(protected) 멤버

| 멤버 | 타입 | 설명 |
|------|------|------|
| `EventCollector` | `IDomainEventCollector` | 도메인 이벤트 수집기 |
| `RequestCategory` | `string` (virtual, 기본 `"Repository"`) | 관측성 카테고리 |

### DapperQueryBase\<TEntity, TDto\>

Dapper 기반 QueryAdapter의 공통 인프라입니다. 서브클래스는 SQL 선언과 WHERE 빌드만 담당합니다.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class DapperQueryBase<TEntity, TDto>
{
    protected DapperQueryBase(IDbConnection connection);
    protected DapperQueryBase(
        IDbConnection connection,
        DapperSpecTranslator<TEntity> translator,
        string tableAlias = "");
}
```

#### 서브클래스 필수 구현

| 멤버 | 타입 | 설명 |
|------|------|------|
| `SelectSql` | `string` | SELECT 쿼리 (FROM + JOIN까지, WHERE 제외) |
| `CountSql` | `string` | COUNT 쿼리 (FROM + JOIN까지, WHERE 제외) |
| `DefaultOrderBy` | `string` | 기본 정렬 절 (예: `"p.created_at DESC"`) |
| `AllowedSortColumns` | `Dictionary<string, string>` | 허용된 정렬 컬럼 매핑 (DTO 필드명 → DB 컬럼명) |

#### 보호된(protected) 메서드

| 메서드 | 설명 |
|--------|------|
| `BuildWhereClause(spec)` | Specification → SQL WHERE 절 변환. `DapperSpecTranslator` 주입 시 기본 구현 제공. 아니면 서브클래스에서 오버라이드 필수 |
| `PaginationClause` | DB 방언별 Offset 페이지네이션 절 (virtual, 기본 `"LIMIT @PageSize OFFSET @Skip"`) |
| `CursorPaginationClause` | DB 방언별 Keyset 페이지네이션 절 (virtual, 기본 `"LIMIT @PageSize"`) |
| `GetCursorValue(item, fieldName)` | DTO에서 커서 값 추출 (virtual, Reflection 기반 기본 구현 + 캐싱) |
| `Params(values)` | `DynamicParameters` 생성 헬퍼 (정적 메서드) |

#### 공개 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset 기반 페이지네이션 검색 (COUNT + SELECT 멀티 쿼리) |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset 기반 검색 (`PageSize + 1` 전략으로 HasMore 판단) |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | `QueryUnbufferedAsync`로 스트리밍 조회 (`DbConnection` 필수) |

### InMemoryQueryBase\<TEntity, TDto\>

InMemory 기반 QueryAdapter의 공통 인프라입니다. `DapperQueryBase`의 InMemory 대응 베이스 클래스입니다.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class InMemoryQueryBase<TEntity, TDto>
{
    // (생성자 파라미터 없음)
}
```

#### 서브클래스 필수 구현

| 멤버 | 타입 | 설명 |
|------|------|------|
| `DefaultSortField` | `string` | 기본 정렬 필드명 |
| `GetProjectedItems(spec)` | `IEnumerable<TDto>` | 필터링 + DTO 프로젝션 (JOIN 로직 포함) |
| `SortSelector(fieldName)` | `Func<TDto, object>` | 정렬 키 셀렉터 (필드명 → 셀렉터 함수) |

#### 공개 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset 기반 페이지네이션 검색 |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset 기반 검색 |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | 메모리 내 스트리밍 조회 |

### DapperSpecTranslator\<TEntity\>

Specification을 SQL WHERE 절로 번역하는 레지스트리입니다. 엔티티 타입별로 한 번 구성하면 여러 Dapper 어댑터가 테이블 별칭만 달리하여 공유할 수 있습니다.

```csharp
namespace Functorium.Adapters.Repositories;

public sealed class DapperSpecTranslator<TEntity>
{
    public DapperSpecTranslator<TEntity> WhenAll(
        Func<string, (string Where, DynamicParameters Params)> handler);

    public DapperSpecTranslator<TEntity> When<TSpec>(
        Func<TSpec, string, (string Where, DynamicParameters Params)> handler)
        where TSpec : Specification<TEntity>;

    public (string Where, DynamicParameters Params) Translate(
        Specification<TEntity> spec, string tableAlias = "");

    public static DynamicParameters Params(params (string Name, object Value)[] values);
    public static string Prefix(string tableAlias);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `WhenAll(handler)` | `DapperSpecTranslator<TEntity>` | `IsAll` (항등원) Specification 핸들러 등록 (Fluent API) |
| `When<TSpec>(handler)` | `DapperSpecTranslator<TEntity>` | 특정 Specification 타입의 SQL 번역 핸들러 등록 (Fluent API) |
| `Translate(spec, alias)` | `(string Where, DynamicParameters Params)` | Specification을 SQL WHERE 절로 번역 |
| `Params(values)` | `DynamicParameters` | `DynamicParameters` 생성 헬퍼 (정적) |
| `Prefix(tableAlias)` | `string` | 테이블 별칭 접두사 반환 (예: `"p"` → `"p."`, `""` → `""`) |

---

## DI 등록 (ObservablePortRegistration)

`IObservablePort` 구현체를 DI 컨테이너에 등록하는 `IServiceCollection` 확장 메서드 모음입니다. `ActivatorUtilities.CreateInstance`를 사용하여 구현 타입의 생성자에 `ActivitySource`, `ILogger`, `IMeterFactory`를 자동 주입합니다.

```csharp
namespace Functorium.Abstractions.Registrations;

public static class ObservablePortRegistration
{
    // 단일 인터페이스 등록
    public static IServiceCollection RegisterScopedObservablePort<TService, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePort<TService, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePort<TService, TImpl>(...);

    // 복수 인터페이스 → 단일 구현체 등록 (For 접미사)
    public static IServiceCollection RegisterScopedObservablePortFor<T1, T2, TImpl>(...);
    public static IServiceCollection RegisterScopedObservablePortFor<T1, T2, T3, TImpl>(...);
    public static IServiceCollection RegisterScopedObservablePortFor<TImpl>(
        ..., params Type[] serviceTypes);

    public static IServiceCollection RegisterTransientObservablePortFor<T1, T2, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePortFor<T1, T2, T3, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePortFor<TImpl>(
        ..., params Type[] serviceTypes);

    public static IServiceCollection RegisterSingletonObservablePortFor<T1, T2, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePortFor<T1, T2, T3, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePortFor<TImpl>(
        ..., params Type[] serviceTypes);
}
```

### 명명 규칙

| 패턴 | 설명 |
|------|------|
| `Register{Lifetime}ObservablePort<TService, TImpl>` | 단일 인터페이스를 하나의 구현체로 등록 |
| `Register{Lifetime}ObservablePortFor<T1, T2, TImpl>` | 2개 인터페이스를 하나의 구현체로 등록 |
| `Register{Lifetime}ObservablePortFor<T1, T2, T3, TImpl>` | 3개 인터페이스를 하나의 구현체로 등록 |
| `Register{Lifetime}ObservablePortFor<TImpl>(params Type[])` | N개 인터페이스를 하나의 구현체로 등록 (4개 이상) |

### 지원 Lifetime

| Lifetime | 인스턴스 공유 범위 |
|----------|-------------------|
| `Scoped` | HTTP 요청당 1개 인스턴스 |
| `Transient` | 요청될 때마다 새 인스턴스 |
| `Singleton` | 애플리케이션 전체에서 1개 인스턴스 |

### 제네릭 제약

모든 서비스 인터페이스 타입 파라미터에는 `class, IObservablePort` 제약이 적용됩니다. `params Type[]` 오버로드는 런타임에 `IObservablePort` 구현 여부와 구현 클래스의 인터페이스 구현 여부를 검증합니다.

### For 접미사 동작

`For` 접미사가 붙은 메서드는 구현체를 먼저 등록한 뒤, 각 서비스 인터페이스가 `GetRequiredService<TImplementation>()`으로 동일한 인스턴스를 참조하도록 등록합니다. 이를 통해 하나의 구현체를 여러 인터페이스로 resolve할 수 있습니다.

```csharp
// 사용 예시: IProductRepository와 IProductQuery를 동일한 Observable 구현체로 등록
services.RegisterScopedObservablePortFor<IProductRepository, IProductQuery, ProductObservable>();
```

---

## 옵션 설정 (OptionsConfigurator)

FluentValidation 기반 옵션 유효성 검사를 DI에 등록하는 유틸리티입니다.

```csharp
namespace Functorium.Adapters.Abstractions.Options;

public static class OptionsConfigurator
{
    public static OptionsBuilder<TOptions> RegisterConfigureOptions<TOptions, TValidator>(
        this IServiceCollection services,
        string configurationSectionName)
        where TOptions : class
        where TValidator : class, IValidator<TOptions>;
}
```

### RegisterConfigureOptions 동작

| 순서 | 동작 | 설명 |
|------|------|------|
| 1 | `IValidator<TOptions>` 등록 | `TValidator`를 Scoped로 DI 등록 |
| 2 | `BindConfiguration` | `appsettings.json`의 `configurationSectionName` 섹션을 `TOptions`에 바인딩 |
| 3 | FluentValidation 연결 | `IValidateOptions<TOptions>` 구현체를 통해 FluentValidation 검증 연결 |
| 4 | `ValidateOnStart` | 프로그램 시작 시 옵션 유효성 검사 실행 |
| 5 | `IStartupOptionsLogger` 자동 등록 | `TOptions`가 `IStartupOptionsLogger`를 구현하면 시작 시 옵션 값 로깅에 자동 등록 |

```csharp
// 사용 예시
services.RegisterConfigureOptions<DatabaseOptions, DatabaseOptionsValidator>("Database");
```

---

## 소스 생성기 어트리뷰트

### \[GenerateObservablePort\]

Adapter 클래스에 이 어트리뷰트를 적용하면 Observable 래퍼 클래스가 소스 생성기에 의해 자동으로 생성됩니다. 생성되는 Observable은 OpenTelemetry 기반의 Tracing, Logging, Metrics를 제공합니다.

```csharp
namespace Functorium.Adapters.SourceGenerators;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateObservablePortAttribute : Attribute;
```

| 속성 | 값 | 설명 |
|------|----|------|
| `AttributeTargets` | `Class` | 클래스에만 적용 가능 |
| `AllowMultiple` | `false` | 한 클래스에 한 번만 적용 |
| `Inherited` | `false` | 파생 클래스에 상속되지 않음 |

**사전 조건:**
- 프로젝트에서 `Functorium.SourceGenerators` 패키지를 참조해야 합니다
- Adapter 클래스의 인터페이스 메서드에 `virtual` 키워드가 필요합니다 (Pipeline이 override)

```csharp
// 사용 예시
[GenerateObservablePort]
public class ProductRepositoryInMemory
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    // virtual 메서드들...
}
// → ProductRepositoryInMemoryObservable 클래스가 자동 생성됨
```

### \[ObservablePortIgnore\]

특정 메서드를 Observable 래퍼 생성에서 제외하는 어트리뷰트입니다. 관측성이 불필요한 헬퍼 메서드나 내부 메서드에 사용합니다.

```csharp
namespace Functorium.Adapters.SourceGenerators;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePortIgnoreAttribute : Attribute;
```

| 속성 | 값 | 설명 |
|------|----|------|
| `AttributeTargets` | `Method` | 메서드에만 적용 가능 |
| `AllowMultiple` | `false` | 한 메서드에 한 번만 적용 |
| `Inherited` | `false` | 파생 클래스에 상속되지 않음 |

```csharp
// 사용 예시
[GenerateObservablePort]
public class ProductRepository : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    [ObservablePortIgnore]
    public virtual FinT<IO, int> GetCount() => ...;  // Observable 래퍼에서 제외
}
```

---

## 관련 문서

| 문서 | 설명 |
|------|------|
| [Port 아키텍처와 정의](../guides/adapter/12-ports) | Port 설계 원칙, 유형별 인터페이스 정의 패턴 가이드 |
| [Adapter 구현](../guides/adapter/13-adapters) | Repository, External API, Query Adapter 유형별 구현 가이드 |
| [Adapter Pipeline과 DI 등록](../guides/adapter/14a-adapter-pipeline-di) | Observable Pipeline 생성과 DI 등록 가이드 |
| [Adapter 테스트](../guides/adapter/14b-adapter-testing) | Adapter 단위/통합 테스트 가이드 |
| [엔티티와 애그리거트 사양](./01-entity-aggregate) | `AggregateRoot<TId>`, `IEntityId<TId>` API 사양 |
| [에러 시스템 사양](./04-error-system) | `AdapterErrorType` (NotFound, PartialNotFound 등) API 사양 |
| [관측 가능성 사양](./08-observability) | 3-Pillar 필드/태그 사양, Meter 정의 규칙 |
| [소스 생성기 사양](./10-source-generators) | `ObservablePortGenerator` 소스 생성기 상세 사양 |
