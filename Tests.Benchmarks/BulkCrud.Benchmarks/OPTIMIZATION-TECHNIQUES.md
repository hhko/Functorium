# 대량 CRUD 성능 최적화 기법

## 개요

10만건 규모의 대량 데이터 처리 시 발생하는 7가지 핵심 병목을 식별하고, 각각에 대한 최적화를 적용했습니다.

| # | 병목 | 원인 | 최적화 기법 | 효과 |
|---|------|------|------------|------|
| 1 | DomainEventCollector 중복 검사 | `List + Any(ReferenceEquals)` O(n²) | HashSet + ReferenceEqualityComparer O(n) | 523x |
| 2 | Repository 단건 처리 | Create/Update/Delete 건건 호출 | CreateRange/UpdateRange/DeleteRange 벌크 메서드 | 3~9x |
| 3 | EF Core 건건 SaveChanges | `Add() + SaveChanges()` 루프 | `AddRange() + SaveChanges()` 단일 호출 | 233x |
| 4 | EF Core 건건 삭제 | Load → Modify → SaveChanges 루프 | `ExecuteUpdateAsync` 서버사이드 SQL | N/A |
| 5 | EF Core 건건 조회 | N개의 `GetById` 개별 쿼리 | `WHERE IN` 단일 쿼리 | N/A |
| 6 | PageRequest 최대 페이지 크기 | MaxPageSize = 100 | MaxPageSize = 10,000 | 100x 쿼리 감소 |
| 7 | 전체 메모리 적재 | 결과셋 전체를 List로 버퍼링 | `IAsyncEnumerable` + `QueryUnbufferedAsync` | 상수 메모리 |

---

## 1. DomainEventCollector: O(n²) → O(n)

### 병목 원인

`DomainEventCollector`는 Repository의 CUD 작업마다 Aggregate를 추적하여, 트랜잭션 완료 시 도메인 이벤트를 수집합니다. 기존 구현은 `List<T>`에 중복 검사로 `Any(ReferenceEquals)`를 사용했습니다.

```csharp
// Before: O(n²) — Track()를 N번 호출하면, 매번 리스트 전체를 순회
private readonly List<IHasDomainEvents> _tracked = [];

public void Track(IHasDomainEvents aggregate)
{
    if (!_tracked.Any(a => ReferenceEquals(a, aggregate)))  // O(n) 탐색
        _tracked.Add(aggregate);
}
```

10만건 Track 시: `1 + 2 + 3 + ... + 100,000 = ~50억 비교` → **5.8초**

### 최적화 기법

`HashSet<T>`에 `ReferenceEqualityComparer`를 적용하여 O(1) 중복 검사를 달성합니다.

```csharp
// After: O(n) — HashSet.Add()는 O(1)
private readonly HashSet<IHasDomainEvents> _tracked = new(ReferenceEqualityComparer.Instance);

public void Track(IHasDomainEvents aggregate) => _tracked.Add(aggregate);

public void TrackRange(IEnumerable<IHasDomainEvents> aggregates)
{
    foreach (var aggregate in aggregates)
        _tracked.Add(aggregate);
}
```

**핵심 포인트:**

- `ReferenceEqualityComparer.Instance`: 참조 동등성(ReferenceEquals) 기반 해시 비교. Aggregate의 `Equals`/`GetHashCode` 오버라이드와 무관하게 인스턴스 동일성을 보장
- `HashSet.Add()`: 이미 존재하면 `false` 반환, 예외 없음. 별도 존재 여부 검사 불필요
- `TrackRange()`: 벌크 메서드에서 한 번에 여러 Aggregate를 추적

### 성능 결과

| Count | Before (List+Any) | After (HashSet) | 속도 향상 | 메모리 절감 |
|------:|---------:|---------:|------:|------:|
| 1K | 570 us | 43 us | 13x | 30% |
| 10K | 220 ms | 976 us | 226x | 41% |
| 100K | 5,848 ms | 11 ms | **523x** | **45%** |

---

## 2. IRepository 벌크 인터페이스

### 병목 원인

기존 `IRepository`는 단건 메서드만 제공합니다. 10만건 처리 시 `Create`를 10만 번 호출하면, 매번 IO 모나드 래핑/언래핑 오버헤드가 발생합니다.

```csharp
// Before: 10만 번의 IO.lift() → Run() → RunAsync() 사이클
foreach (var product in products)
    await repo.Create(product).Run().RunAsync();
```

### 최적화 기법

벌크 메서드를 인터페이스에 추가하여 단일 IO 모나드 내에서 전체 컬렉션을 처리합니다.

```csharp
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // 기존 단건 메서드
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, Unit> Delete(TId id);

    // 벌크 메서드
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Unit> DeleteRange(IReadOnlyList<TId> ids);
}
```

**설계 결정:**

| 항목 | 선택 | 이유 |
|------|------|------|
| 입력 타입 | `IReadOnlyList<T>` | `List`, `Array`, `Seq` 모두 무변환 전달 가능 |
| 반환 타입 | `Seq<T>` | 기존 함수형 컨벤션(LanguageExt) 유지 |
| IO 래핑 | 단일 `IO.lift()` | N번의 모나드 사이클 → 1번으로 감소 |

### InMemory 구현

```csharp
// After: 단일 IO.lift() 내에서 전체 컬렉션 처리
public virtual FinT<IO, Seq<Product>> CreateRange(IReadOnlyList<Product> products)
{
    return IO.lift(() =>
    {
        foreach (var product in products)
            Products[product.Id] = product;       // ConcurrentDictionary O(1) 삽입
        _eventCollector.TrackRange(products);      // 벌크 이벤트 추적
        return Fin.Succ(toSeq(products));
    });
}
```

### 성능 결과 (InMemory, 100K)

| Operation | 단건 루프 | 벌크 | 속도 향상 | 메모리 절감 |
|-----------|-------:|------:|------:|------:|
| Create | 403 ms | 123 ms | 3.3x | 85% |
| Read | 274 ms | 40 ms | 7x | 98% |
| Update | 121 ms | 38 ms | 3x | 94% |
| Delete | 92 ms | 11 ms | 9x | 99.9% |

---

## 3. EF Core AddRange 벌크 생성

### 병목 원인

EF Core의 `Add()` + `SaveChangesAsync()`를 건건 호출하면:

1. 매번 Change Tracker에 엔티티 등록 (O(n) 상태 확인)
2. 매번 SQL INSERT 생성 및 실행
3. 매번 트랜잭션 커밋 (SQLite: 파일 I/O fsync)

```csharp
// Before: N개의 Add + N개의 SaveChanges = N개의 DB 트랜잭션
foreach (var product in products)
{
    dbContext.Products.Add(product.ToModel());
    await dbContext.SaveChangesAsync();   // 매번 트랜잭션 커밋
}
```

### 최적화 기법

`AddRange()`로 모든 엔티티를 Change Tracker에 한 번에 등록하고, `SaveChangesAsync()`를 1회만 호출합니다.

```csharp
// After: 1회 AddRange + 1회 SaveChanges = 1회 DB 트랜잭션
public virtual FinT<IO, Seq<Product>> CreateRange(IReadOnlyList<Product> products)
{
    return IO.liftAsync(async () =>
    {
        _dbContext.Products.AddRange(products.Select(p => p.ToModel()));
        _eventCollector.TrackRange(products);
        return Fin.Succ(toSeq(products));
    });
}
// SaveChanges는 UnitOfWork/Pipeline에서 1회 호출
```

**핵심 포인트:**

- `AddRange()`: EF Core가 내부적으로 배치 INSERT SQL을 생성 (예: `INSERT INTO ... VALUES (...), (...), (...)`)
- Change Tracker 최적화: `Add()`를 N번 호출하면 매번 상태 그래프를 탐색하지만, `AddRange()`는 벌크 감지 후 최적화된 경로 사용
- 트랜잭션 비용: SQLite의 `fsync` 호출이 1회로 감소 (디스크 I/O 병목 제거)

### 성능 결과 (EF Core SQLite)

| Count | SingleAdd + SaveChanges | AddRange + SaveChanges | 속도 향상 | 메모리 절감 |
|------:|----------:|------:|------:|------:|
| 1K | 14.3 s | 326 ms | **44x** | 94% |
| 10K | 4.1 min | 1.1 s | **233x** | 99.4% |

`AddRange via Repository` (함수형 래핑 포함)도 `AddRange + SaveChanges` (직접 호출)와 거의 동일한 성능을 보여, Repository 추상화 계층의 오버헤드가 무시할 수준임을 확인했습니다.

---

## 4. EF Core ExecuteUpdateAsync 벌크 삭제

### 병목 원인

Soft Delete 패턴에서 기존 방식은 엔티티를 로드 → 속성 변경 → SaveChanges로 3단계를 거칩니다.

```csharp
// Before: Load + Modify + SaveChanges (엔티티 전체를 메모리에 적재)
var models = await dbContext.Products
    .Where(p => ids.Contains(p.Id))
    .ToListAsync();                    // 전체 엔티티 메모리 적재
dbContext.Products.RemoveRange(models);
await dbContext.SaveChangesAsync();    // N개의 UPDATE SQL
```

### 최적화 기법

EF Core 7+의 `ExecuteUpdateAsync`를 사용하여 엔티티를 메모리에 로드하지 않고 서버사이드에서 직접 SQL UPDATE를 실행합니다.

```csharp
// After: 단일 SQL UPDATE (엔티티 로드 없음)
public virtual FinT<IO, Unit> DeleteRange(IReadOnlyList<ProductId> ids)
{
    return IO.liftAsync(async () =>
    {
        var idStrings = ids.Select(id => id.ToString()).ToList();
        await _dbContext.Products
            .Where(p => idStrings.Contains(p.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
                .SetProperty(p => p.DeletedBy, "system"));
        return Fin.Succ(unit);
    });
}
```

**생성되는 SQL:**

```sql
-- 단일 SQL 문 (엔티티 로드 없이 서버에서 직접 실행)
UPDATE Products
SET DeletedAt = @p0, DeletedBy = @p1
WHERE Id IN (@id0, @id1, @id2, ...)
```

**핵심 포인트:**

- 엔티티 메모리 적재 없음 → 메모리 사용량 대폭 감소
- Change Tracker 미사용 → 상태 추적 오버헤드 제거
- 단일 SQL 문 실행 → DB 라운드트립 1회
- **트레이드오프**: 도메인 이벤트 미발생 (엔티티 로드 없이 SQL만 실행하므로 도메인 로직 우회)

---

## 5. EF Core WHERE IN 벌크 조회

### 병목 원인

```csharp
// Before: N개의 개별 쿼리
foreach (var id in ids)
{
    var product = await repo.GetById(id).Run().RunAsync();
}
// → SELECT * FROM Products WHERE Id = @id (10만 번 실행)
```

### 최적화 기법

`Where(p => ids.Contains(p.Id))`로 단일 쿼리에 모든 ID를 포함합니다.

```csharp
// After: 단일 WHERE IN 쿼리
public virtual FinT<IO, Seq<Product>> GetByIds(IReadOnlyList<ProductId> ids)
{
    return IO.liftAsync(async () =>
    {
        var idStrings = ids.Select(id => id.ToString()).ToList();
        var models = await _dbContext.Products
            .AsNoTracking()                              // Change Tracker 비활성화
            .Include(p => p.ProductTags)
            .Where(p => idStrings.Contains(p.Id))        // WHERE IN 절
            .ToListAsync();
        return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
    });
}
```

**생성되는 SQL:**

```sql
-- 단일 쿼리
SELECT p.*, pt.*
FROM Products p
LEFT JOIN ProductTags pt ON p.Id = pt.ProductId
WHERE p.Id IN (@id0, @id1, @id2, ...)
```

**핵심 포인트:**

- `AsNoTracking()`: 읽기 전용 조회에서 Change Tracker 오버헤드 제거
- `WHERE IN`: N개의 개별 쿼리를 1개의 쿼리로 통합
- DB 라운드트립: N → 1

---

## 6. PageRequest MaxPageSize 확장

### 병목 원인

```csharp
// Before: MaxPageSize = 100
// 10만건 조회 시: 100,000 / 100 = 1,000회 페이징 쿼리 필요
```

### 최적화 기법

```csharp
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 10_000;  // 100 → 10,000

    public PageRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
```

**효과:**

| 항목 | Before | After |
|------|--------|-------|
| 10만건 조회 시 페이징 횟수 | 1,000회 | **10회** |
| DB 라운드트립 | 1,000회 | **10회** |

---

## 7. IAsyncEnumerable 스트리밍 조회

### 병목 원인

기존 `Search` 메서드는 결과셋 전체를 `List<T>`로 메모리에 적재합니다.

```csharp
// Before: 전체 결과를 메모리에 버퍼링
var result = await queryPort.Search(spec, page, sort).Run().RunAsync();
// result.Items → List<TDto> (전체 메모리 적재)
```

### 최적화 기법

`IAsyncEnumerable<T>`와 Dapper의 `QueryUnbufferedAsync`를 조합하여, DB 커서에서 한 행씩 yield합니다.

**인터페이스:**

```csharp
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    // 기존: 페이징된 결과를 메모리에 적재
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec, PageRequest page, SortExpression sort);

    // 신규: 스트리밍 (메모리에 전체 적재하지 않음)
    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        CancellationToken cancellationToken = default);
}
```

**Dapper 기반 구현:**

```csharp
public virtual async IAsyncEnumerable<TDto> Stream(
    Specification<TEntity> spec,
    SortExpression sort,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var (where, parameters) = BuildWhereClause(spec);
    var orderBy = BuildOrderByClause(sort);
    var sql = $"{SelectSql} {where} {orderBy}";

    var dbConnection = _connection as DbConnection
        ?? throw new InvalidOperationException(
            "Stream requires a DbConnection instance.");

    await foreach (var item in dbConnection
        .QueryUnbufferedAsync<TDto>(sql, parameters)
        .WithCancellation(cancellationToken))
    {
        yield return item;  // 한 행씩 반환
    }
}
```

**Search vs Stream 비교:**

| 항목 | Search (버퍼링) | Stream (스트리밍) |
|------|----------------|------------------|
| 메모리 사용 | 결과셋 크기에 비례 | **상수** (행 1개분) |
| 첫 결과 반환 시점 | 전체 쿼리 완료 후 | **첫 행 도착 즉시** |
| 페이징 | 지원 (OFFSET/LIMIT) | 불필요 |
| 취소 지원 | 쿼리 완료 전 불가 | `CancellationToken`으로 중간 취소 가능 |
| 적합한 용도 | UI 페이지네이션 | 대량 데이터 내보내기, ETL, 배치 처리 |

**핵심 포인트:**

- `QueryUnbufferedAsync`: Dapper가 ADO.NET `DbDataReader`를 서버사이드 커서로 유지하며, 한 행씩 읽어서 `IAsyncEnumerable<T>`로 반환
- `yield return`: 호출자가 `await foreach`로 소비할 때만 다음 행을 읽음 (lazy evaluation)
- `[EnumeratorCancellation]`: `WithCancellation()`으로 전달된 토큰이 자동으로 매핑됨

---

## 최적화 기법 분류

### 알고리즘 복잡도 개선

| 기법 | 적용 위치 | 개선 |
|------|----------|------|
| HashSet 중복 검사 | DomainEventCollector | O(n²) → O(n) |
| ConcurrentDictionary 조회 | InMemory Repository | O(n) 탐색 → O(1) 해시 조회 |

### I/O 라운드트립 감소

| 기법 | 적용 위치 | 개선 |
|------|----------|------|
| AddRange + 단일 SaveChanges | EF Core CreateRange | N 트랜잭션 → 1 트랜잭션 |
| WHERE IN 쿼리 | EF Core GetByIds | N 쿼리 → 1 쿼리 |
| ExecuteUpdateAsync | EF Core DeleteRange | Load+Save N건 → SQL 1문 |
| MaxPageSize 확장 | PageRequest | 1,000 쿼리 → 10 쿼리 |

### 메모리 효율화

| 기법 | 적용 위치 | 개선 |
|------|----------|------|
| IO 모나드 래핑 1회 | 벌크 Repository | N번 lift/run → 1번 lift/run |
| AsNoTracking | EF Core GetByIds | Change Tracker 메모리 제거 |
| ExecuteUpdateAsync | EF Core DeleteRange | 엔티티 메모리 적재 제거 |
| IAsyncEnumerable 스트리밍 | DapperQueryAdapterBase | O(n) 메모리 → O(1) 메모리 |

---

## 참고

- 벤치마크 결과: [`README.md`](README.md)
- 벤치마크 실행: `cd Src.Benchmarks/BulkCrud.Benchmarks.Runner && dotnet run -c Release -- --filter '*'`
- 성능 검증 테스트: `dotnet test --project Src.Benchmarks/BulkCrud.Benchmarks/BulkCrud.Benchmarks.csproj`
