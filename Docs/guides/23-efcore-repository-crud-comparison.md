# EfCoreRepositoryBase 단건 vs 벌크 CRUD 비교

이 문서는 `EfCoreRepositoryBase`의 단건/벌크 CRUD 연산 간 아키텍처 경로 차이를 비교 분석합니다.

## 목차

- [요약](#요약)
- [전체 비교표](#전체-비교표)
- [Create: 단건과 벌크의 대칭 구조](#create-단건과-벌크의-대칭-구조)
- [Read: 단건과 벌크의 대칭 구조](#read-단건과-벌크의-대칭-구조)
- [Update: 단건과 벌크의 대칭 구조](#update-단건과-벌크의-대칭-구조)
- [Delete: 단건과 벌크의 대칭 구조](#delete-단건과-벌크의-대칭-구조)
- [Product Soft Delete 오버라이드](#product-soft-delete-오버라이드)
- [서브클래스 오버라이드 현황](#서브클래스-오버라이드-현황)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 개념

**1. CRUD 8개 연산 모두 대칭**

| 연산 | 단건 vs 벌크 | 이유 |
|------|:---:|------|
| Create | 대칭 | `DbSet.Add` vs `DbSet.AddRange` (API만 복수형) |
| Read | 대칭 | `FirstOrDefault` vs `Where().ToList()` (조건만 단수/복수) |
| Update | 대칭 | `DbSet.Update` vs `DbSet.UpdateRange` (API만 복수형) |
| Delete | 대칭 | `Where(pred).ExecuteDeleteAsync` (동일 경로, 조건만 단수/복수) |

**2. 비대칭은 Soft Delete 오버라이드에서만 발생**
- Product `Delete`(단건): 도메인 객체를 생성하고 `product.Delete()` 상태 전이 → `Attach + IsModified` → 이벤트 발행
- Product `DeleteRange`(벌크): `ExecuteUpdateAsync`로 `DeletedAt`/`DeletedBy`만 SQL로 직접 갱신 → 이벤트 미발행
- 벌크 SQL과 도메인 이벤트는 구조적으로 양립 불가능

---

## 전체 비교표

| 연산 | 구분 | Change Tracker | 도메인 변환 | 이벤트 추적 | ReadQuery | 실행 방식 |
|------|------|:-:|:-:|:-:|:-:|------|
| **Create** | 단건 | O | O (ToModel) | O (Track) | - | `DbSet.Add` |
| **CreateRange** | 벌크 | O | O (ToModel) | O (TrackRange) | - | `DbSet.AddRange` |
| **GetById** | 단건 | X | O (ToDomain) | - | O | `AsNoTracking` → `FirstOrDefault` |
| **GetByIds** | 벌크 | X | O (ToDomain) | - | O | `AsNoTracking` → `Where` → `ToList` |
| **Update** | 단건 | O | O (ToModel) | O (Track) | - | `DbSet.Update` |
| **UpdateRange** | 벌크 | O | O (ToModel) | O (TrackRange) | - | `DbSet.UpdateRange` |
| **Delete** | 단건 | **X** | **X** | **X** | - | `Where(pred).ExecuteDeleteAsync` |
| **DeleteRange** | 벌크 | **X** | **X** | **X** | - | `Where(pred).ExecuteDeleteAsync` |

> **참고**: ReadQuery 열의 O는 `ReadQuery()` (AsNoTracking + Include 자동 적용)를 사용한다는 의미입니다.
>
> **참고**: GetByIds는 요청 ID 수와 결과 수가 다르면 `PartialNotFoundError`를 반환합니다.

---

## Create: 단건과 벌크의 대칭 구조

단건과 벌크의 실행 경로가 동일합니다. API만 복수형으로 바뀝니다.

```
Create:      DbSet.Add(ToModel(agg))       → Track(agg)
CreateRange: DbSet.AddRange(Select ToModel) → TrackRange(aggs)
```

**소스 코드 비교:**

```csharp
// 단건 — EfCoreRepositoryBase.cs:121-129
public virtual FinT<IO, TAggregate> Create(TAggregate aggregate)
{
    return IO.lift(() =>
    {
        DbSet.Add(ToModel(aggregate));
        EventCollector.Track(aggregate);
        return Fin.Succ(aggregate);
    });
}

// 벌크 — EfCoreRepositoryBase.cs:167-175
public virtual FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates)
{
    return IO.lift(() =>
    {
        DbSet.AddRange(aggregates.Select(ToModel));
        EventCollector.TrackRange(aggregates);
        return Fin.Succ(toSeq(aggregates));
    });
}
```

| 항목 | Create | CreateRange |
|------|--------|-------------|
| IO 방식 | `IO.lift` (동기) | `IO.lift` (동기) |
| Change Tracker | O | O |
| 도메인 변환 | `ToModel` (1건) | `Select(ToModel)` (N건) |
| 이벤트 추적 | `Track` | `TrackRange` |

---

## Read: 단건과 벌크의 대칭 구조

둘 다 `ReadQuery()`를 사용하므로 AsNoTracking과 Include가 자동 적용됩니다. 조건만 단수/복수로 다릅니다.

```
GetById:  ReadQuery().FirstOrDefault(ByIdPredicate)
GetByIds: ReadQuery().Where(ByIdsPredicate).ToList()
```

**소스 코드 비교:**

```csharp
// 단건 — EfCoreRepositoryBase.cs:131-145
public virtual FinT<IO, TAggregate> GetById(TId id)
{
    return IO.liftAsync(async () =>
    {
        var model = await ReadQuery()
            .FirstOrDefaultAsync(ByIdPredicate(id));

        if (model is not null)
        {
            return Fin.Succ(ToDomain(model));
        }

        return NotFoundError(id);
    });
}

// 벌크 — EfCoreRepositoryBase.cs:177-193
public virtual FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids)
{
    return IO.liftAsync(async () =>
    {
        var models = await ReadQuery()
            .Where(ByIdsPredicate(ids))
            .ToListAsync();
        var aggregates = toSeq(models.Select(ToDomain));

        if (aggregates.Count != ids.Count)
        {
            return PartialNotFoundError(ids, aggregates);
        }

        return Fin.Succ(aggregates);
    });
}
```

| 항목 | GetById | GetByIds |
|------|---------|----------|
| 쿼리 소스 | `ReadQuery()` | `ReadQuery()` |
| 필터링 | `FirstOrDefaultAsync(pred)` | `Where(pred).ToListAsync()` |
| 도메인 변환 | `ToDomain` (1건) | `Select(ToDomain)` (N건) |
| NotFound 처리 | `NotFoundError` (null 체크) | `PartialNotFoundError` (건수 불일치) |

---

## Update: 단건과 벌크의 대칭 구조

Create와 동일한 대칭 패턴입니다.

```
Update:      DbSet.Update(ToModel(agg))       → Track(agg)
UpdateRange: DbSet.UpdateRange(Select ToModel) → TrackRange(aggs)
```

**소스 코드 비교:**

```csharp
// 단건 — EfCoreRepositoryBase.cs:147-155
public virtual FinT<IO, TAggregate> Update(TAggregate aggregate)
{
    return IO.lift(() =>
    {
        DbSet.Update(ToModel(aggregate));
        EventCollector.Track(aggregate);
        return Fin.Succ(aggregate);
    });
}

// 벌크 — EfCoreRepositoryBase.cs:195-203
public virtual FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates)
{
    return IO.lift(() =>
    {
        DbSet.UpdateRange(aggregates.Select(ToModel));
        EventCollector.TrackRange(aggregates);
        return Fin.Succ(toSeq(aggregates));
    });
}
```

| 항목 | Update | UpdateRange |
|------|--------|-------------|
| IO 방식 | `IO.lift` (동기) | `IO.lift` (동기) |
| Change Tracker | O | O |
| 도메인 변환 | `ToModel` (1건) | `Select(ToModel)` (N건) |
| 이벤트 추적 | `Track` | `TrackRange` |

---

## Delete: 단건과 벌크의 대칭 구조

Delete도 C/R/U와 동일한 대칭 구조입니다. 둘 다 `ExecuteDeleteAsync`로 SQL DELETE를 직접 실행합니다.

```
Delete:      Where(ByIdPredicate).ExecuteDeleteAsync()
DeleteRange: Where(ByIdsPredicate).ExecuteDeleteAsync()
```

**소스 코드 비교:**

```csharp
// 단건 — EfCoreRepositoryBase.cs:157-165
public virtual FinT<IO, int> Delete(TId id)
{
    return IO.liftAsync(async () =>
    {
        int affected = await DbSet.Where(ByIdPredicate(id))
            .ExecuteDeleteAsync();
        return Fin.Succ(affected);
    });
}

// 벌크 — EfCoreRepositoryBase.cs:205-213
public virtual FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids)
{
    return IO.liftAsync(async () =>
    {
        int affected = await DbSet.Where(ByIdsPredicate(ids))
            .ExecuteDeleteAsync();
        return Fin.Succ(affected);
    });
}
```

| 항목 | Delete (단건) | DeleteRange (벌크) |
|------|:---:|:---:|
| Change Tracker | X | X |
| 도메인 객체 생성 | X | X |
| 이벤트 추적 | X | X |
| 실행 방식 | `ExecuteDeleteAsync` | `ExecuteDeleteAsync` |
| DB 왕복 | 1회 (DELETE) | 1회 (DELETE) |
| 반환 타입 | `FinT<IO, int>` (affected rows) | `FinT<IO, int>` (affected rows) |

> **참고**: 이전 구현에서는 단건 Delete가 `FindAsync → Remove` (Change Tracker 경유, DB 2회 왕복)이었으나, 현재는 벌크와 동일하게 `ExecuteDeleteAsync`를 사용합니다.

---

## Product Soft Delete 오버라이드

Product는 Hard Delete 대신 Soft Delete를 구현합니다. **베이스 클래스의 Delete는 대칭이지만, Soft Delete 오버라이드에서 비대칭이 발생합니다.**

### 단건 Delete (도메인 주도)

```
ReadQueryIgnoringFilters → ToDomain → product.Delete("system") → ToModel → Attach + IsModified → Track
```

```csharp
// EfCoreProductRepository.cs:52-75
public override FinT<IO, int> Delete(ProductId id)
{
    return IO.liftAsync(async () =>
    {
        var model = await ReadQueryIgnoringFilters()
            .FirstOrDefaultAsync(ByIdPredicate(id));

        if (model is null)
        {
            return NotFoundError(id);
        }

        var product = ToDomain(model);
        product.Delete("system");           // 도메인 상태 전이 → 이벤트 발행

        var updatedModel = ToModel(product);
        DbSet.Attach(updatedModel);                                                  // Attach: Change Tracker에 등록
        _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;  // DeletedAt만 UPDATE
        _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;  // DeletedBy만 UPDATE

        EventCollector.Track(product);      // 이벤트 수집
        return Fin.Succ(1);
    });
}
```

`Attach + IsModified` 패턴: `DbSet.Update`가 모든 컬럼을 UPDATE하는 것과 달리, `Attach` 후 필요한 프로퍼티만 `IsModified = true`로 설정하면 해당 컬럼만 UPDATE SQL에 포함됩니다. Soft Delete는 `DeletedAt`/`DeletedBy` 두 컬럼만 변경하므로 불필요한 컬럼 갱신을 방지합니다.

### 벌크 DeleteRange (SQL 주도)

```
DbSet.Where(pred).ExecuteUpdateAsync(SetProperty DeletedAt, DeletedBy)
```

```csharp
// EfCoreProductRepository.cs:82-93
public override FinT<IO, int> DeleteRange(IReadOnlyList<ProductId> ids)
{
    return IO.liftAsync(async () =>
    {
        int affected = await DbSet
            .Where(ByIdsPredicate(ids))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
                .SetProperty(p => p.DeletedBy, "system"));
        return Fin.Succ(affected);
    });
}
```

### 비교

| 항목 | Delete (단건) | DeleteRange (벌크) |
|------|:---:|:---:|
| 실행 SQL | UPDATE (2 columns) | UPDATE (2 columns) |
| Change Tracker | O (`Attach + IsModified`) | X |
| 도메인 객체 생성 | O (`ToDomain`) | X |
| 도메인 상태 전이 | O (`product.Delete()`) | X |
| 도메인 이벤트 | O | **X** |
| DB 왕복 | 2회 (SELECT + UPDATE) | 1회 (UPDATE) |
| 반환 타입 | `FinT<IO, int>` | `FinT<IO, int>` |

### 비대칭 원인

벌크 SQL 연산(`ExecuteUpdateAsync`)과 도메인 이벤트 추적은 **구조적으로 양립 불가능**합니다.

1. 도메인 이벤트는 도메인 객체의 상태 전이에서 발생
2. 벌크 SQL은 도메인 객체를 생성하지 않음
3. N건을 개별 로드하면 벌크 연산의 성능 이점이 소멸

이것은 의도된 성능 트레이드오프입니다. 이벤트가 필요한 경우 단건 `Delete()`를 사용하세요.

---

## 서브클래스 오버라이드 현황

| 리포지토리 | CRUD 오버라이드 | 고유 메서드 |
|-----------|:-:|------|
| `EfCoreProductRepository` | `Delete`, `DeleteRange` | `GetByIdIncludingDeleted`, `Exists` |
| `EfCoreOrderRepository` | 없음 | 없음 |
| `EfCoreCustomerRepository` | 없음 | `Exists` |
| `EfCoreInventoryRepository` | 없음 | `GetByProductId`, `Exists` |
| `EfCoreTagRepository` | 없음 | 없음 |

Product만 유일하게 CRUD를 오버라이드합니다. 이유는 Soft Delete라는 도메인 요구사항 때문입니다.

### applyIncludes 선언 현황

| 리포지토리 | applyIncludes | Navigation Property |
|-----------|--------------|---------------------|
| `EfCoreProductRepository` | `q => q.Include(p => p.ProductTags)` | `ProductTags` |
| `EfCoreOrderRepository` | `q => q.Include(o => o.OrderLines)` | `OrderLines` |
| `EfCoreCustomerRepository` | `null` (기본값) | 없음 |
| `EfCoreInventoryRepository` | `null` (기본값) | 없음 |
| `EfCoreTagRepository` | `null` (기본값) | 없음 |

---

## 트러블슈팅

### 벌크 DeleteRange 후 Change Tracker 상태가 불일치할 때

**원인:** `ExecuteDeleteAsync`/`ExecuteUpdateAsync`는 Change Tracker를 우회하므로, 이미 추적 중인 엔티티의 상태가 DB와 달라질 수 있습니다.

**해결:** `ReadQuery()`는 `AsNoTracking()`을 사용하므로 읽기 시에는 문제가 없습니다. 동일 트랜잭션 내에서 벌크 삭제 후 해당 엔티티를 Change Tracker로 조작해야 한다면, `DbContext.ChangeTracker.Clear()`를 호출하세요.

### Product 벌크 DeleteRange에서 도메인 이벤트가 발행되지 않을 때

**원인:** 의도된 동작입니다. `ExecuteUpdateAsync`는 도메인 객체를 생성하지 않으므로 이벤트가 발행되지 않습니다.

**해결:** 이벤트가 반드시 필요하다면 단건 `Delete()`를 개별 호출하세요. 성능이 중요하다면 벌크 `DeleteRange()`를 사용하고, 필요한 후처리를 별도로 수행하세요.

---

## FAQ

### Q1. 왜 CRUD 8개 연산이 모두 대칭인가요?

Create/Update는 호출자가 이미 도메인 객체를 가지고 있으므로, 단건이든 벌크든 `ToModel` → `DbSet.Add/Update`로 동일한 경로를 탑니다. Delete는 둘 다 ID를 받아 `ExecuteDeleteAsync`로 SQL DELETE를 직접 실행합니다. Read는 둘 다 `ReadQuery()`를 사용하고 조건만 단수/복수로 다릅니다.

### Q2. 벌크 연산에서 도메인 이벤트를 발행할 수 있나요?

구조적으로 불가능합니다. 도메인 이벤트는 도메인 객체의 상태 전이에서 발생하는데, `ExecuteDeleteAsync`/`ExecuteUpdateAsync`는 도메인 객체를 생성하지 않습니다. N건을 개별 로드하면 벌크 연산의 성능 이점이 소멸합니다.

### Q3. ReadQuery()와 ReadQueryIgnoringFilters()의 차이는 무엇인가요?

| 메서드 | 글로벌 필터 | 용도 |
|--------|:-:|------|
| `ReadQuery()` | 적용 | 일반 조회 (Soft Delete된 엔티티 제외) |
| `ReadQueryIgnoringFilters()` | 무시 | Soft Delete된 엔티티 포함 조회 |

둘 다 `AsNoTracking()`과 `applyIncludes`가 적용됩니다.

### Q4. 벌크 DeleteRange에 단일 ID를 넘기면 어떻게 되나요?

정상 동작합니다. `DeleteRange(new[] { id })`는 단건 `Delete(id)`와 동일하게 1회 DB 왕복으로 삭제를 수행합니다. 둘 다 `ExecuteDeleteAsync`를 사용하므로 성능 차이가 없습니다. 다만 Delete는 `affected` 값으로 삭제 건수를 확인할 수 있을 뿐이며, 두 메서드 모두 도메인 이벤트를 발행하지 않습니다.

### Q5. Product 외에 Soft Delete가 필요한 엔티티가 추가되면 어떻게 하나요?

`EfCoreProductRepository`의 패턴을 따르세요:
1. `Delete()`를 오버라이드하여 `ReadQueryIgnoringFilters` → `ToDomain` → 상태 전이 → `Attach + IsModified` 경로를 구현
2. `DeleteRange()`를 오버라이드하여 `ExecuteUpdateAsync`로 `DeletedAt`/`DeletedBy`를 직접 갱신
3. 글로벌 쿼리 필터를 `DbContext.OnModelCreating`에서 설정

---

## 참고 문서

- [13-adapters.md](./13-adapters.md) — Adapter 구현 가이드
- [Src/Functorium/Adapters/Repositories/EfCoreRepositoryBase.cs](../../Src/Functorium/Adapters/Repositories/EfCoreRepositoryBase.cs) — 베이스 클래스 소스
- [Src.Benchmarks/BulkCrud.Benchmarks/OPTIMIZATION-TECHNIQUES.md](../../Src.Benchmarks/BulkCrud.Benchmarks/OPTIMIZATION-TECHNIQUES.md) — 대량 CRUD 성능 최적화 기법
