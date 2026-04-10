---
title: "트랜잭션 파이프라인"
---
## 개요

모든 Command Usecase에서 SaveChanges와 이벤트 발행을 반복해야 한다면 어떻게 될까요? Usecase마다 동일한 트랜잭션 관리 코드를 작성하면, 비즈니스 로직이 인프라 보일러플레이트에 묻힙니다. 이 장에서는 Mediator Pipeline으로 트랜잭션 시작, SaveChanges, 커밋, 이벤트 발행을 자동화하여 Usecase가 비즈니스 로직에만 집중할 수 있게 만들어봅시다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **Command Pipeline 실행 흐름** (Request → Handler → SaveChanges → Commit → Event Publishing)을 설명할 수 있습니다
2. **IUnitOfWork와 IUnitOfWorkTransaction으로** 자동 트랜잭션 관리를 구현할 수 있습니다
3. **Handler 실패 / SaveChanges 실패 시** 미커밋으로 자동 롤백되는 원리를 설명할 수 있습니다
4. **트랜잭션 커밋 이후에** 이벤트가 발행되는 이유를 설명할 수 있습니다

---

## "왜 필요한가?"

Pipeline 없이 매 Usecase마다 트랜잭션을 직접 관리하면 이렇게 됩니다.

```csharp
// Pipeline 없이 매번 반복되는 보일러플레이트
public async ValueTask<FinResponse<Response>> Handle(
    CreateProductCommand.Request request, CancellationToken ct)
{
    await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
    try
    {
        // --- 비즈니스 로직 ---
        var product = Product.Create(request.Name, request.Price);
        var fin = await repository.Create(product).RunAsync();
        if (fin.IsFail) return fin.ToFinResponse<Response>();
        // --- 여기까지가 진짜 로직 ---

        await unitOfWork.SaveChanges(ct).RunAsync();
        await transaction.CommitAsync(ct);
        await eventPublisher.PublishTrackedEvents(ct);

        return fin.ToFinResponse(p => new Response(p.Id.ToString()));
    }
    catch
    {
        // Dispose 시 미커밋 트랜잭션 자동 롤백
        throw;
    }
}
```

비즈니스 로직은 3줄인데, 트랜잭션 관리 코드가 나머지를 차지합니다. Usecase가 10개, 20개로 늘어나면 이 보일러플레이트가 모두 복사됩니다. Transaction Pipeline은 이 횡단 관심사를 한 곳에서 처리합니다.

---

## 핵심 개념

### Command Pipeline 실행 순서

Pipeline은 아래 순서대로 실행됩니다. Handler(비즈니스 로직)는 2번에서만 동작하고, 나머지는 Pipeline이 자동으로 처리합니다.

```
1. BeginTransactionAsync()       ← 트랜잭션 시작
2. Handler 실행 (next)           ← 비즈니스 로직
3. 실패 시 → return              ← 미커밋 → Dispose 시 롤백
4. UoW.SaveChanges()             ← DB 저장
5. transaction.CommitAsync()     ← 트랜잭션 커밋
6. PublishTrackedEvents()        ← 도메인 이벤트 발행
7. return response               ← 성공 응답 반환
```

3번에서 Handler가 실패를 반환하면 SaveChanges 이하를 건너뛰고 즉시 반환합니다. 트랜잭션은 커밋되지 않았으므로 Dispose 시 자동 롤백됩니다.

### IUnitOfWork

SaveChanges와 트랜잭션 시작을 추상화합니다.

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

`IAsyncDisposable`을 구현하므로 `await using`으로 사용합니다. Dispose 시 미커밋 트랜잭션은 자동 롤백됩니다.

```csharp
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
```

### Query는 바이패스

Pipeline은 Command만 처리합니다. Query 요청이 들어오면 Handler만 실행하고 SaveChanges/트랜잭션을 건너뜁니다.

---

## 프로젝트 설명

아래 파일들에서 Pipeline의 동작을 직접 확인할 수 있습니다.

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

Transaction Pipeline의 핵심 구성 요소를 정리합니다.

| 개념 | 설명 |
|------|------|
| `IUnitOfWork` | SaveChanges + 트랜잭션 시작을 추상화 |
| `IUnitOfWorkTransaction` | 명시적 트랜잭션 스코프 (IAsyncDisposable) |
| `UsecaseTransactionPipeline` | Mediator Pipeline으로 자동 트랜잭션 관리 |
| 롤백 | Dispose 시 미커밋 트랜잭션 자동 롤백 |
| 이벤트 발행 | 트랜잭션 커밋 후에만 이벤트 발행 |

---

## FAQ

### Q1: 왜 이벤트 발행이 트랜잭션 커밋 이후인가요?
**A**: 커밋 전에 이벤트를 발행하면, 이벤트 핸들러가 아직 커밋되지 않은 데이터를 조회할 수 있습니다. 또한 커밋이 실패하면 이미 발행된 이벤트를 취소할 수 없습니다.

### Q2: Usecase에서 직접 SaveChanges를 호출해야 하나요?
**A**: 아닙니다. Pipeline이 자동으로 호출합니다. Usecase는 Repository의 Create/Update만 호출하면 됩니다.

### Q3: 명시적 트랜잭션이 필요한 경우는?
**A**: Pipeline이 자동으로 트랜잭션을 관리하므로, 일반적으로 Usecase에서 직접 트랜잭션을 다룰 필요가 없습니다. Pipeline의 `BeginTransactionAsync`가 ExecuteDeleteAsync 등 즉시 실행 SQL도 동일 트랜잭션에 포함시킵니다.

---

CQRS 아키텍처의 모든 계층을 완성했습니다. 이제 실제 도메인에서 이 패턴들을 통합하는 모습을 확인해봅시다. Part 5에서는 주문, 고객, 재고, 카탈로그 네 가지 도메인 예제를 통해 전체 CQRS 흐름을 실전에 적용합니다.

→ [1장: 주문 관리](../../Part5-Domain-Examples/01-Ecommerce-Order-Management/)
