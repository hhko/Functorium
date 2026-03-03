---
title: "Part 2 - Chapter 8: Unit of Work"
---

> **Part 2: Command 측 -- Repository 패턴** | [← 이전: 7장 EF Core Repository](../03-EfCore-Repository/) | [다음: 9장 QueryPort Interface →](../../Part3-Query-Patterns/01-QueryPort-Interface/)

---

## 개요

Unit of Work 패턴은 **하나의 비즈니스 트랜잭션에서 발생하는 모든 변경을 추적하고, 한 번에 커밋하는 역할**을 합니다. Repository가 개별 Aggregate의 CRUD를 담당한다면, Unit of Work는 여러 Repository의 변경을 하나의 트랜잭션으로 묶습니다. 이 장에서는 `IUnitOfWork` 인터페이스와 InMemory 구현체를 통해 핵심 개념을 실습합니다.

---

## 학습 목표

### 핵심 학습 목표
1. **IUnitOfWork 인터페이스** - `SaveChanges()`와 `BeginTransactionAsync()`의 역할
2. **Pending Actions 패턴** - 변경 사항을 지연 실행하는 메커니즘
3. **IUnitOfWorkTransaction** - 명시적 트랜잭션 스코프의 사용법

### 실습을 통해 확인할 내용
- **InMemoryUnitOfWork**: `IUnitOfWork`의 InMemory 구현
- **SaveChanges()**: 대기 중인 작업을 일괄 실행
- **BeginTransactionAsync()**: 명시적 트랜잭션 생성

---

## 핵심 개념

### IUnitOfWork 인터페이스

```csharp
public interface IUnitOfWork
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

- **SaveChanges()**: 추적된 모든 변경 사항을 영속화합니다. `FinT<IO, Unit>`을 반환하여 성공/실패를 타입 안전하게 처리합니다.
- **BeginTransactionAsync()**: 명시적 트랜잭션을 시작합니다. `ExecuteDeleteAsync` 같은 즉시 실행 SQL과 `SaveChanges`를 동일 트랜잭션으로 묶을 때 사용합니다.

### 지연 실행 패턴

InMemory 구현에서는 `AddPendingAction()`으로 작업을 등록하고, `SaveChanges()` 호출 시 일괄 실행합니다:

```csharp
uow.AddPendingAction(() => store[product.Id] = product);
// 아직 store에 반영되지 않음

await uow.SaveChanges().Run().RunAsync();
// 이제 store에 반영됨
```

실제 EF Core에서는 Change Tracker가 이 역할을 대신합니다.

### IUnitOfWorkTransaction

```csharp
await using var tx = await uow.BeginTransactionAsync();
// 여러 작업 수행
await tx.CommitAsync();
// Dispose 시 미커밋 트랜잭션은 자동 롤백
```

---

## 프로젝트 설명

### 프로젝트 구조

```
04-Unit-Of-Work/
├── UnitOfWork/
│   ├── UnitOfWork.csproj
│   ├── Program.cs                 # SaveChanges, 트랜잭션 데모
│   ├── ProductId.cs               # Ulid 기반 식별자
│   ├── Product.cs                 # 데모용 Aggregate
│   └── InMemoryUnitOfWork.cs      # IUnitOfWork InMemory 구현
├── UnitOfWork.Tests.Unit/
│   ├── UnitOfWork.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── InMemoryUnitOfWorkTests.cs
└── README.md
```

### 핵심 코드

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

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| 인터페이스 | `IUnitOfWork` |
| 핵심 메서드 | `SaveChanges()`, `BeginTransactionAsync()` |
| 반환 타입 | `FinT<IO, Unit>` (SaveChanges) |
| 트랜잭션 | `IUnitOfWorkTransaction` (CommitAsync + DisposeAsync) |
| InMemory 구현 | `AddPendingAction()` → `SaveChanges()` |
| 실제 구현 | EF Core의 `DbContext.SaveChangesAsync()` 래핑 |

### Repository vs Unit of Work
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
