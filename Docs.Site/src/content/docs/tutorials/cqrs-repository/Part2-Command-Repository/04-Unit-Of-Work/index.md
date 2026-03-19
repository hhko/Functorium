---
title: "작업 단위"
---
## 개요

주문은 생성됐는데 재고 차감이 실패하면 어떻게 될까요?
Repository가 개별 Aggregate의 CRUD를 담당하다 보니, 여러 Repository에 걸친 변경이 부분적으로만 반영될 위험이 있습니다.
Unit of Work 패턴은 하나의 비즈니스 트랜잭션에서 발생하는 모든 변경을 추적하고, 한 번에 커밋하여 이 문제를 해결합니다.
이 장에서는 `IUnitOfWork` 인터페이스와 InMemory 구현체를 통해 핵심 개념을 실습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `IUnitOfWork`의 `SaveChanges()`와 `BeginTransactionAsync()`의 역할을 설명할 수 있습니다.
2. Pending Actions 패턴으로 변경 사항을 지연 실행하는 메커니즘을 구현할 수 있습니다.
3. `IUnitOfWorkTransaction`을 사용한 명시적 트랜잭션 스코프를 작성할 수 있습니다.

---

## 핵심 개념

### IUnitOfWork 인터페이스

`IUnitOfWork`는 두 가지 메서드를 제공합니다.

```csharp
public interface IUnitOfWork
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

- **SaveChanges()는** 추적된 모든 변경 사항을 한 번에 영속화합니다. `FinT<IO, Unit>`을 반환하여 성공/실패를 타입 안전하게 처리합니다.
- **BeginTransactionAsync()는** 명시적 트랜잭션을 시작합니다. `ExecuteDeleteAsync` 같은 즉시 실행 SQL과 `SaveChanges`를 동일 트랜잭션으로 묶을 때 사용합니다.

### 지연 실행 패턴

Repository가 CRUD를 수행해도 즉시 저장소에 반영하지 않습니다. 대신 `AddPendingAction()`으로 작업을 등록해 두고, `SaveChanges()` 호출 시점에 일괄 실행합니다.

```csharp
uow.AddPendingAction(() => store[product.Id] = product);
// 아직 store에 반영되지 않음

await uow.SaveChanges().Run().RunAsync();
// 이제 store에 반영됨
```

이 패턴 덕분에 여러 Repository의 변경이 `SaveChanges()` 한 번으로 원자적으로 반영됩니다. 실제 EF Core에서는 Change Tracker가 이 역할을 대신합니다.

### IUnitOfWorkTransaction

일반적인 CRUD에서는 `SaveChanges()`만으로 충분하지만, 즉시 실행 쿼리와 Change Tracker 기반 변경을 하나로 묶어야 할 때는 명시적 트랜잭션이 필요합니다.

```csharp
await using var tx = await uow.BeginTransactionAsync();
// 여러 작업 수행
await tx.CommitAsync();
// Dispose 시 미커밋 트랜잭션은 자동 롤백
```

`CommitAsync()`를 호출하지 않고 블록을 벗어나면 트랜잭션이 자동 롤백되므로, 실패 시 부분 커밋이 발생하지 않습니다.

### 다중 Aggregate 트랜잭션

개요에서 던진 질문으로 돌아갑시다. "주문은 생성됐는데 재고 차감이 실패하면?" — 각 Repository가 개별적으로 저장하면 이런 불일치가 발생합니다.

```csharp
var productStore = new Dictionary<ProductId, Product>();
var orderStore = new Dictionary<OrderId, Order>();

var laptop = Product.Create("노트북", 1_500_000m, stock: 10);
productStore[laptop.Id] = laptop;

var uow = new InMemoryUnitOfWork();

// 두 Aggregate의 변경을 하나의 UoW에 등록
var order = Order.Create(laptop.Id, quantity: 2, unitPrice: laptop.Price);
uow.AddPendingAction(() => orderStore[order.Id] = order);
uow.AddPendingAction(() => laptop.DeductStock(2));

// SaveChanges 전: 주문 0건, 재고 10
await uow.SaveChanges().Run().RunAsync();
// SaveChanges 후: 주문 1건, 재고 8
```

두 Aggregate의 변경이 `SaveChanges()` 한 번으로 원자적으로 반영됩니다.
개별 Repository가 각자 SaveChanges를 호출했다면 주문은 생성되었지만 재고는 차감되지 않는 불일치가 발생할 수 있습니다.

> **참고**: InMemory 구현에서는 pending action이 순차 실행되므로, 중간에 예외가 발생하면 이미 실행된 action의 부수 효과가 남습니다. 실제 EF Core에서는 Change Tracker가 DB 레벨에서 all-or-nothing을 보장합니다.

---

## 프로젝트 설명

### 프로젝트 구조

```
04-Unit-Of-Work/
├── UnitOfWork/
│   ├── UnitOfWork.csproj
│   ├── Program.cs                 # SaveChanges, 트랜잭션, 다중 Aggregate 데모
│   ├── ProductId.cs               # Ulid 기반 식별자
│   ├── Product.cs                 # 데모용 Aggregate (재고 관리 포함)
│   ├── OrderId.cs                 # Ulid 기반 주문 식별자
│   ├── Order.cs                   # 주문 Aggregate (다중 Aggregate 데모용)
│   └── InMemoryUnitOfWork.cs      # IUnitOfWork InMemory 구현
├── UnitOfWork.Tests.Unit/
│   ├── UnitOfWork.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── InMemoryUnitOfWorkTests.cs
└── README.md
```

### 핵심 코드

`InMemoryUnitOfWork`의 구현을 살펴보세요. `_pendingActions` 리스트에 작업을 쌓아두고, `SaveChanges()`에서 한 번에 실행합니다.

**InMemoryUnitOfWork** -- 대기 작업 등록 및 일괄 실행:

```csharp
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _pendingActions = [];
    private bool _saved;
    public bool IsSaved => _saved;

    public void AddPendingAction(Action action) => _pendingActions.Add(action);

    public FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.lift(() =>
        {
            foreach (var action in _pendingActions) action();
            _pendingActions.Clear();
            _saved = true;
            return Fin.Succ(unit);
        });
    }
}
```

`IO.lift()`로 감싸서 실제 실행을 `Run()` 호출 시점까지 지연합니다. `_pendingActions`를 순회하며 모든 작업을 실행한 뒤 리스트를 비우는 것이 핵심입니다.

---

## 한눈에 보는 정리

다음 테이블은 Unit of Work의 핵심 구성 요소를 요약합니다.

| 항목 | 설명 |
|------|------|
| 인터페이스 | `IUnitOfWork` |
| 핵심 메서드 | `SaveChanges()`, `BeginTransactionAsync()` |
| 반환 타입 | `FinT<IO, Unit>` (SaveChanges) |
| 트랜잭션 | `IUnitOfWorkTransaction` (CommitAsync + DisposeAsync) |
| InMemory 구현 | `AddPendingAction()` → `SaveChanges()` |
| 실제 구현 | EF Core의 `DbContext.SaveChangesAsync()` 래핑 |

### Repository vs Unit of Work

Repository와 Unit of Work는 어떻게 다를까요? 다음 테이블에서 관심사의 차이를 비교해 보세요.

| 관심사 | Repository | Unit of Work |
|--------|------------|-------------|
| 범위 | 단일 Aggregate | 전체 트랜잭션 |
| 역할 | CRUD 오퍼레이션 | 변경 사항 일괄 커밋 |
| 의존성 | 특정 Aggregate 타입 | Aggregate 무관 |
| 호출 시점 | Usecase 내부 | Usecase 완료 시 |

---

## FAQ

### Q1: 왜 Repository에서 직접 SaveChanges를 호출하지 않나요?
**A**: 하나의 Usecase에서 여러 Repository를 사용할 때, 각 Repository가 개별적으로 SaveChanges를 호출하면 부분 커밋이 발생할 수 있습니다. Unit of Work가 모든 변경을 한 번에 커밋하여 원자성을 보장합니다.

### Q2: BeginTransactionAsync는 언제 사용하나요?
**A**: EF Core의 `ExecuteDeleteAsync`/`ExecuteUpdateAsync` 같은 즉시 실행 쿼리와 Change Tracker 기반 `SaveChanges`를 동일 트랜잭션으로 묶어야 할 때 사용합니다. 일반적인 CRUD에서는 `SaveChanges()`만으로 충분합니다.

### Q3: IsSaved 속성은 실제 운영에서도 사용하나요?
**A**: 아닙니다. `IsSaved`는 InMemory 구현에서 테스트 편의를 위해 추가한 속성입니다. 실제 EF Core 기반 Unit of Work에서는 `SaveChanges()`의 `Fin<Unit>` 결과로 성공/실패를 판단합니다.

---

Command 측 영속화를 완성했습니다. Repository로 개별 Aggregate를 저장하고, Unit of Work로 트랜잭션 원자성을 보장합니다. 그런데 Repository의 GetById로 주문 목록을 조회하면 어떤 한계가 있을까요? 전체 Aggregate를 로드한 뒤 필요한 필드만 추출해야 하므로 비효율적입니다. Part 3에서는 읽기에 최적화된 Query 패턴을 살펴봅니다.

→ [1장: IQueryPort 인터페이스](../../Part3-Query-Patterns/01-QueryPort-Interface/)
