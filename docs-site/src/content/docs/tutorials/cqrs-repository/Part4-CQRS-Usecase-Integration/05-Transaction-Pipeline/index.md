---
title: "Part 4 - Chapter 18: Transaction Pipeline"
---

> **Part 4: CQRS Usecase 통합** | [← 이전: 17장 Domain Event Flow](../04-Domain-Event-Flow/) | [다음: 19장 E-commerce Order Management →](../../Part5-Domain-Examples/01-Ecommerce-Order-Management/)

---

## 개요

Transaction Pipeline은 Command Usecase의 실행 흐름을 자동으로 관리하는 인프라 계층입니다. Handler 실행 후 SaveChanges, 트랜잭션 커밋, 도메인 이벤트 발행을 순차적으로 처리하며, 실패 시 자동으로 롤백합니다. Usecase 개발자는 비즈니스 로직에만 집중하면 됩니다.

---

## 학습 목표

- **Command Pipeline 실행 흐름**: Request -> Handler -> SaveChanges -> Commit -> Event Publishing
- **자동 트랜잭션 관리**: IUnitOfWork와 IUnitOfWorkTransaction의 역할
- **실패 시 롤백**: Handler 실패 / SaveChanges 실패 시 미커밋으로 자동 롤백
- **이벤트 발행 타이밍**: 트랜잭션 커밋 이후에 이벤트 발행

---

## 핵심 개념

### Command Pipeline 실행 순서

```
1. BeginTransactionAsync()       ← 트랜잭션 시작
2. Handler 실행 (next)           ← 비즈니스 로직
3. 실패 시 → return              ← 미커밋 → Dispose 시 롤백
4. UoW.SaveChanges()             ← DB 저장
5. transaction.CommitAsync()     ← 트랜잭션 커밋
6. PublishTrackedEvents()        ← 도메인 이벤트 발행
7. return response               ← 성공 응답 반환
```

### IUnitOfWork

```csharp
public interface IUnitOfWork
{
    FinT<IO, Unit> SaveChanges(CancellationToken ct = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
```

- `SaveChanges`: EF Core의 `SaveChangesAsync()`를 FinT로 감싸서 호출
- `BeginTransactionAsync`: 명시적 트랜잭션 스코프를 시작

### IUnitOfWorkTransaction

```csharp
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
```

`IAsyncDisposable`을 구현하므로 `await using`으로 사용합니다. Dispose 시 미커밋 트랜잭션은 자동 롤백됩니다.

### Query는 바이패스

Pipeline은 Command만 처리합니다. Query 요청이 들어오면 Handler만 실행하고 SaveChanges/트랜잭션을 건너뜁니다.

---

## 프로젝트 설명

| 파일 | 설명 |
|------|------|
| `ProductId.cs` | Ulid 기반 Product 식별자 |
| `Product.cs` | AggregateRoot + 이벤트를 발생시키는 상품 |
| `InMemoryUnitOfWork.cs` | IUnitOfWork / IUnitOfWorkTransaction InMemory 구현 |
| `SimpleDomainEventCollector.cs` | IDomainEventCollector 구현 |
| `TransactionDemo.cs` | Command Pipeline 흐름 시뮬레이션 |
| `Program.cs` | 성공/실패 시나리오 데모 |

---

## 한눈에 보는 정리

| 개념 | 설명 |
|------|------|
| `IUnitOfWork` | SaveChanges + 트랜잭션 시작을 추상화 |
| `IUnitOfWorkTransaction` | 명시적 트랜잭션 스코프 (IAsyncDisposable) |
| `UsecaseTransactionPipeline` | Mediator Pipeline으로 자동 트랜잭션 관리 |
| 롤백 | Dispose 시 미커밋 트랜잭션 자동 롤백 |
| 이벤트 발행 | 트랜잭션 커밋 후에만 이벤트 발행 |

---

## FAQ

**Q: 왜 이벤트 발행이 트랜잭션 커밋 이후인가요?**
A: 커밋 전에 이벤트를 발행하면, 이벤트 핸들러가 아직 커밋되지 않은 데이터를 조회할 수 있습니다. 또한 커밋이 실패하면 이미 발행된 이벤트를 취소할 수 없습니다.

**Q: Usecase에서 직접 SaveChanges를 호출해야 하나요?**
A: 아닙니다. Pipeline이 자동으로 호출합니다. Usecase는 Repository의 Create/Update만 호출하면 됩니다.

**Q: 명시적 트랜잭션이 필요한 경우는?**
A: Pipeline이 자동으로 트랜잭션을 관리하므로, 일반적으로 Usecase에서 직접 트랜잭션을 다룰 필요가 없습니다. Pipeline의 `BeginTransactionAsync`가 ExecuteDeleteAsync 등 즉시 실행 SQL도 동일 트랜잭션에 포함시킵니다.
