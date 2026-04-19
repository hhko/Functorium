# Functorium Release v1.0.0-alpha.3

**[English](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.3/.release-notes/v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3.md)** | **한국어**

**발표 자료**: [PDF](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.3/.release-notes/v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3-KR.pdf) | [PPTX](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.3/.release-notes/v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3-KR.pptx) | [MP4](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.3/.release-notes/v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3-KR.mp4) | [M4A](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.3/.release-notes/v1/v1.0.0-alpha.3/RELEASE-v1.0.0-alpha.3-KR.m4a)

## 개요

Functorium v1.0.0-alpha.3은 **IRepository 재설계**와 **EF Core 성능 최적화**에 집중합니다. Repository 인터페이스가 Write Single, Write Batch, Read, Specification 4개 책임 그룹으로 재구성되었으며, 배치 연산은 Change Tracker를 우회하여 처리량이 크게 향상됩니다. 새로운 `[GenerateSetters]` Source Generator는 EF Core `ExecuteUpdateAsync` 도입에 필요한 보일러플레이트를 제거합니다.

**주요 기능**:

- **IRepository 재설계**: Write Single / Write Batch / Read / Specification 4그룹 분리, Specification 기반 `Exists`, `Count`, `DeleteBy` 연산 추가
- **EF Core 성능 최적화**: `Update`/`UpdateRange`가 `ExecuteUpdateAsync`로 Change Tracker 우회, `CreateRange`가 대량 데이터 청크 삽입 지원
- **[GenerateSetters] Source Generator**: EF Core 모델의 `ApplySetters` 자동 생성, 수동 `SetProperty` 보일러플레이트 제거

## Breaking Changes

### 1. CreateRange/UpdateRange 반환 타입 변경

배치 쓰기 연산이 Aggregate 컬렉션(`Seq<TAggregate>`) 대신 영향 받은 행 수(`int`)를 반환합니다. 배치 호출자가 이미 Aggregate 참조를 보유하고 있으므로 성공/건수 신호만 필요하다는 원칙에 따릅니다.

**이전 (v1.0.0-alpha.2)**:
```csharp
// 배치 연산이 Aggregate 컬렉션을 반환
FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);

// 호출자 사용 방식
var created = await repository.CreateRange(orders).RunAsync();
Seq<Order> orders = created.ThrowIfFail();  // Seq<Order>
```

**이후 (v1.0.0-alpha.3)**:
```csharp
// 배치 연산이 영향 받은 건수를 반환
FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates);
FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates);

// 호출자 사용 방식
var created = await repository.CreateRange(orders).RunAsync();
int count = created.ThrowIfFail();  // int (생성/업데이트된 행 수)
```

**마이그레이션 가이드**:
1. `CreateRange` / `UpdateRange` 반환값을 `Seq<TAggregate>`로 사용하는 모든 호출부를 `int`로 변경
2. 배치 연산 후 Aggregate 컬렉션이 필요하면 입력 리스트를 직접 유지
3. `CreateRange` 또는 `UpdateRange`를 override하는 커스텀 Repository 서브클래스의 반환 타입 업데이트

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

---

### 2. EfCoreRepositoryBase 서브클래스에 새 추상 메서드 `BuildSetters` 필수

`EfCoreRepositoryBase`의 모든 서브클래스에서 `BuildSetters` 추상 메서드를 구현해야 합니다. 이 메서드는 `Update`/`UpdateRange` 연산에서 `ExecuteUpdateAsync`가 사용하는 `SetProperty` 매핑을 제공합니다.

**이전 (v1.0.0-alpha.2)**:
```csharp
public class OrderRepository : EfCoreRepositoryBase<Order, OrderId, OrderModel>
{
    // ToDomain과 ToModel만 필수
    protected override Order ToDomain(OrderModel model) => ...;
    protected override OrderModel ToModel(Order aggregate) => ...;
}
```

**이후 (v1.0.0-alpha.3)**:
```csharp
public class OrderRepository : EfCoreRepositoryBase<Order, OrderId, OrderModel>
{
    protected override Order ToDomain(OrderModel model) => ...;
    protected override OrderModel ToModel(Order aggregate) => ...;

    // 신규: ExecuteUpdateAsync에 필수
    protected override void BuildSetters(
        UpdateSettersBuilder<OrderModel> setters, OrderModel model)
        => OrderModel.ApplySetters(setters, model);  // [GenerateSetters]가 생성
}
```

**마이그레이션 가이드**:
1. EF Core 모델 클래스에 `[GenerateSetters]` 어트리뷰트를 적용하고 `partial`로 선언
2. Repository 서브클래스에서 생성된 `ApplySetters`에 위임하는 `BuildSetters` 메서드 구현
3. 프로젝트에 `Functorium.SourceGenerators` 참조 추가

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

---

### 3. ExistsBySpec 이름 변경 및 public 승격

`EfCoreRepositoryBase`의 `protected ExistsBySpec` 메서드가 `Exists`로 이름이 변경되고 `public virtual`로 승격되어, 새로운 `IRepository.Exists` 계약을 구현합니다.

**이전 (v1.0.0-alpha.2)**:
```csharp
// protected 메서드 — Repository 서브클래스 내부에서만 사용 가능
protected FinT<IO, bool> ExistsBySpec(Specification<TAggregate> spec)
```

**이후 (v1.0.0-alpha.3)**:
```csharp
// IRepository 인터페이스의 public 메서드
public virtual FinT<IO, bool> Exists(Specification<TAggregate> spec)
```

**마이그레이션 가이드**:
1. 모든 `ExistsBySpec` 호출을 `Exists`로 이름 변경
2. Repository 서브클래스에서 `ExistsBySpec`을 호출하고 있었다면, 이제 `IRepository` 인터페이스에서 직접 사용 가능

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

## 새로운 기능

### Functorium 라이브러리

#### 1. IRepository 인터페이스 재구성

`IRepository<TAggregate, TId>` 인터페이스가 4개의 명확한 책임 그룹으로 재설계되었습니다. 각 그룹은 일관된 반환 타입 규칙을 따릅니다: 단건 쓰기 연산은 LINQ 모나드 합성을 위해 Aggregate를 반환하고, 배치 쓰기 연산은 영향 받은 행 수를 반환하며, Specification 연산은 단일 SQL 문으로 실행됩니다.

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // -- Write: Single --
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // -- Write: Batch --
    FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);

    // -- Read --
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);

    // -- Specification --
    FinT<IO, bool> Exists(Specification<TAggregate> spec);
    FinT<IO, int> Count(Specification<TAggregate> spec);
    FinT<IO, int> DeleteBy(Specification<TAggregate> spec);
}
```

**왜 중요한가:**
- 단건/배치/읽기/Specification 책임을 명확히 분리하여 인터페이스 탐색 시 인지 부하 감소
- 배치 연산이 `Seq<TAggregate>` 대신 `int`를 반환하여 호출자가 이미 보유한 대규모 Aggregate 컬렉션의 불필요한 실체화 방지
- Specification 기반 `Exists`, `Count`, `DeleteBy`는 각각 단일 SQL 문으로 실행(클라이언트 측 필터링 없음)되어 Repository를 통한 효율적인 Aggregate 수준 쿼리 가능
- `UpdateBy(Specification, SetPropertyCalls)`는 의존성 방향 규칙(EF Core 타입은 Adapter 계층)을 준수하기 위해 인터페이스에서 의도적으로 제외

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

---

#### 2. Specification 기반 Repository 연산: Exists, Count, DeleteBy

3개의 새로운 Specification 기반 메서드가 `IRepository`에 추가되어, 단일 SQL 문으로 변환되는 Aggregate 수준 조건부 쿼리가 가능합니다. `EfCoreRepositoryBase`와 `InMemoryRepositoryBase` 모두 구현을 제공합니다.

```csharp
// 고객의 활성 주문 존재 여부 확인 (1 SQL: SELECT EXISTS)
var spec = new ActiveOrdersByCustomerSpec(customerId);
FinT<IO, bool> exists = repository.Exists(spec);

// 매칭되는 Aggregate 건수 (1 SQL: SELECT COUNT)
FinT<IO, int> count = repository.Count(spec);

// Specification 기반 대량 삭제 (1 SQL: DELETE WHERE)
var expiredSpec = new ExpiredOrdersSpec(cutoffDate);
FinT<IO, int> deletedCount = repository.DeleteBy(expiredSpec);
```

**왜 중요한가:**
- 이 메서드들 없이는 존재 확인이나 건수 조회를 위해 매칭되는 모든 Aggregate를 메모리에 로드한 후 클라이언트 측에서 필터링해야 했음
- 각 연산은 EF Core의 Expression 변환을 통해 단일 SQL 문으로 컴파일되어 데이터베이스 라운드트립 최소화
- `DeleteBy`는 `ExecuteDeleteAsync`로 Change Tracker를 완전히 우회하여 엔티티 로드 없이 대량 삭제 처리
- Specification 재사용: 동일한 `Specification<TAggregate>` 클래스가 `Exists`, `Count`, `DeleteBy` 및 기존 쿼리 인프라에서 모두 동작

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

---

### Functorium.Adapters 라이브러리

#### 3. EF Core Update 성능: ExecuteUpdateAsync 도입

`EfCoreRepositoryBase`의 `Update`와 `UpdateRange`가 기존 `FindAsync` + `SetValues` 패턴 대신 EF Core의 `ExecuteUpdateAsync`를 사용합니다. Change Tracker 기반 업데이트에 필요했던 SELECT 라운드트립이 제거됩니다.

```csharp
// 이전: 2 라운드트립 (SELECT + UPDATE)
// var existing = await DbSet.FindAsync(id);
// DbContext.Entry(existing).CurrentValues.SetValues(updated);

// 이후: 1 라운드트립 (UPDATE WHERE Id = @id)
var model = ToModel(aggregate);
int affected = await DbSet
    .Where(ByIdPredicate(aggregate.Id))
    .ExecuteUpdateAsync(s => BuildSetters(s, model));
```

**왜 중요한가:**
- `Update`가 2 데이터베이스 라운드트립에서 1로 감소 (사전 SELECT 불필요), 단건 업데이트 지연 시간 절반으로 단축
- `UpdateRange`가 Change Tracker 대신 Aggregate별 `ExecuteUpdateAsync`를 실행하여 N+1 SELECT 문제 방지
- Change Tracker 우회로 대규모 배치 시나리오에서 수정된 엔티티 추적에 의한 메모리 압력 제거
- `protected UpdateBy(Specification, Action<UpdateSettersBuilder>)` 메서드를 통해 서브클래스에서 도메인 특화 조건부 대량 업데이트 구현 가능 (예: "만료된 구독 일괄 비활성화"를 단일 SQL UPDATE로 처리)

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

---

#### 4. CreateRange 대량 데이터 청크 삽입

`CreateRange`가 대규모 입력을 감지하여 자동으로 청크로 분할하고, 청크별로 `SaveChangesAsync` + `ChangeTracker.Clear()`를 호출하여 메모리 부족을 방지합니다.

```csharp
// 소량 배치 (<= IdBatchSize): 단일 AddRange + SaveChanges
// 대량 배치 (> IdBatchSize): 청크 처리

// EfCoreRepositoryBase가 자동으로 처리:
//   foreach (var chunk in aggregates.Chunk(IdBatchSize))
//   {
//       DbSet.AddRange(chunk.Select(ToModel));
//       await DbContext.SaveChangesAsync();
//       DbContext.ChangeTracker.Clear();
//   }

// 사용법은 변경 없음:
FinT<IO, int> created = repository.CreateRange(largeOrderList);
```

**왜 중요한가:**
- 단일 `AddRange` + `SaveChangesAsync` 호출로 수만 건의 엔티티를 삽입하면 Change Tracker 메모리가 무한히 증가하여 운영 환경에서 OOM 발생
- `ChangeTracker.Clear()`를 사용한 청크 처리로 배치 크기에 관계없이 일정한 메모리 사용량 유지
- 소량 배치(`IdBatchSize` 이하)는 청크 오버헤드 없이 빠른 동기 경로 사용
- 청크 간 원자성을 보장하려면 `UsecaseTransactionPipeline` 컨텍스트 내에서 사용해야 함; 래핑 트랜잭션 없이는 부분 삽입 롤백 불가

<!-- 관련 커밋: 5d843dab feat!(repository): IRepository 재설계 및 EF Core 대량 처리 성능 개선 -->

---

#### 5. [GenerateSetters] Source Generator

EF Core 모델 클래스에 새로운 `[GenerateSetters]` 어트리뷰트를 적용하면 Source Generator가 `ExecuteUpdateAsync`용 각 settable 프로퍼티를 `SetProperty` 호출로 매핑하는 `ApplySetters` 정적 메서드를 생성합니다.

```csharp
using Functorium.Adapters.SourceGenerators;

[GenerateSetters]
public partial class OrderModel : IHasStringId
{
    public string Id { get; set; } = string.Empty;     // 자동 제외 (Id)
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [SetterIgnore]
    public DateTime AuditTimestamp { get; set; }        // 수동 제외

    public ICollection<OrderItemModel> Items { get; set; } = [];  // 자동 제외 (네비게이션)
}

// 생성된 메서드 (Functorium.SourceGenerators에 의해):
// public static void ApplySetters(UpdateSettersBuilder<OrderModel> s, OrderModel m)
// {
//     s.SetProperty(x => x.Status, m.Status);
//     s.SetProperty(x => x.TotalAmount, m.TotalAmount);
// }

// Repository에서 사용:
protected override void BuildSetters(
    UpdateSettersBuilder<OrderModel> setters, OrderModel model)
    => OrderModel.ApplySetters(setters, model);
```

**왜 중요한가:**
- 각 모델 프로퍼티에 대해 수동으로 `SetProperty` 호출을 작성하는 것은 번거롭고 실수하기 쉬움; 새 컬럼을 추가하면 모델과 Repository 양쪽을 업데이트해야 함
- Source Generator가 모델과 자동으로 `ApplySetters`를 동기화 — 프로퍼티를 추가하면 빌드 타임에 setter 매핑이 생성됨
- `Id` 프로퍼티와 네비게이션 프로퍼티(`ICollection<T>`, `IList<T>`)가 자동으로 제외되어 실수로 인한 기본 키 덮어쓰기나 네비게이션 에러 방지
- `[SetterIgnore]`로 감사 컬럼이나 계산 필드 등 업데이트에 포함되지 않아야 하는 프로퍼티를 세밀하게 제어

<!-- 관련 커밋: 606e610e feat(source-gen): [GenerateSetters] Source Generator 추가 -->

## 버그 수정

- IRepository 재설계 후 BulkCrud 벤치마크 네임스페이스 및 `ValidationRules<T>` 참조 수정 (`8d4e1b8a`)

## API 변경사항

### IRepository 인터페이스 (Functorium)

```
Functorium.Domains.Repositories
└── IRepository<TAggregate, TId>
    ├── Write: Single
    │   ├── Create(TAggregate) -> FinT<IO, TAggregate>
    │   ├── Update(TAggregate) -> FinT<IO, TAggregate>
    │   └── Delete(TId) -> FinT<IO, int>
    ├── Write: Batch
    │   ├── CreateRange(IReadOnlyList<TAggregate>) -> FinT<IO, int>     [변경: 기존 Seq<TAggregate>]
    │   ├── UpdateRange(IReadOnlyList<TAggregate>) -> FinT<IO, int>     [변경: 기존 Seq<TAggregate>]
    │   └── DeleteRange(IReadOnlyList<TId>) -> FinT<IO, int>
    ├── Read
    │   ├── GetById(TId) -> FinT<IO, TAggregate>
    │   └── GetByIds(IReadOnlyList<TId>) -> FinT<IO, Seq<TAggregate>>
    └── Specification                                                    [신규 그룹]
        ├── Exists(Specification<TAggregate>) -> FinT<IO, bool>          [신규]
        ├── Count(Specification<TAggregate>) -> FinT<IO, int>            [신규]
        └── DeleteBy(Specification<TAggregate>) -> FinT<IO, int>         [신규]
```

### EfCoreRepositoryBase 신규 멤버 (Functorium.Adapters)

```
Functorium.Adapters.Repositories
└── EfCoreRepositoryBase<TAggregate, TId, TModel>
    ├── abstract void BuildSetters(UpdateSettersBuilder<TModel>, TModel)  [신규: 필수]
    ├── virtual FinT<IO, bool> Exists(Specification<TAggregate>)          [신규: 기존 protected ExistsBySpec]
    ├── virtual FinT<IO, int> Count(Specification<TAggregate>)            [신규]
    ├── virtual FinT<IO, int> DeleteBy(Specification<TAggregate>)         [신규]
    └── protected FinT<IO, int> UpdateBy(Specification<TAggregate>, Action<UpdateSettersBuilder<TModel>>)  [신규]
```

### Source Generator 어트리뷰트 (Functorium.Adapters)

```
Functorium.Adapters.SourceGenerators
├── GenerateSettersAttribute      [신규]
└── SetterIgnoreAttribute         [신규]
```

## 설치

### NuGet 패키지 설치

```bash
# Functorium 핵심 라이브러리
dotnet add package Functorium --version 1.0.0-alpha.3

# Functorium.Adapters (Repository, Pipeline, Observability)
dotnet add package Functorium.Adapters --version 1.0.0-alpha.3

# Functorium.SourceGenerators (빌드 타임 코드 생성)
dotnet add package Functorium.SourceGenerators --version 1.0.0-alpha.3

# Functorium.Testing (테스트 유틸리티, 선택)
dotnet add package Functorium.Testing --version 1.0.0-alpha.3
```

### 필수 의존성

- .NET 10 이상
- LanguageExt.Core 5.x
- Microsoft.EntityFrameworkCore.Relational (`ExecuteUpdateAsync` 필수)
