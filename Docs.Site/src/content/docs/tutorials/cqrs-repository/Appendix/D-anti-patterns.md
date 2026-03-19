---
title: "CQRS 안티패턴"
---
## 개요

CQRS 패턴을 적용할 때 흔히 발생하는 안티패턴과 올바른 대안을 정리합니다. 이 패턴들을 인식하면 설계 실수를 사전에 방지할 수 있습니다.

---

## 1. Repository로 목록 조회하기

### 안티패턴

```csharp
// IRepository의 GetByIds로 목록 조회를 시도
public async ValueTask<FinResponse<List<OrderDto>>> Handle(
    ListOrdersQuery request, CancellationToken ct)
{
    var ids = await GetAllOrderIds();                    // 전체 ID 조회
    var fin = await repository.GetByIds(ids).RunAsync(); // Aggregate 전체 로드
    var dtos = fin.Map(orders => orders.Map(o => new OrderDto(...))); // 수동 변환
    return dtos.ToFinResponse();
}
```

**문제점:**
- Aggregate Root 전체를 메모리에 로드 (불필요한 도메인 로직 포함)
- 페이지네이션 없이 전체 데이터 로드
- DTO 변환을 수동으로 처리
- N+1 쿼리 발생 가능

### 올바른 방법

```csharp
// IQueryPort로 목록 조회
public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
    ListOrdersQuery request, CancellationToken ct)
{
    var spec = Specification<Order>.All;
    var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
    return fin.ToFinResponse();
}
```

---

## 2. Query Usecase에서 데이터 변경하기

### 안티패턴

```csharp
// Query Usecase에서 데이터를 변경
public class GetOrderUsecase
    : IQueryUsecase<GetOrderQuery, OrderDto>
{
    public async ValueTask<FinResponse<OrderDto>> Handle(
        GetOrderQuery request, CancellationToken ct)
    {
        // Query인데 조회수를 증가시킴
        await repository.Update(order.IncrementViewCount()).RunAsync();
        return fin.ToFinResponse();
    }
}
```

**문제점:**
- Query는 읽기 전용이어야 함 (CQS 원칙 위반)
- 트랜잭션 파이프라인이 Query에는 적용되지 않을 수 있음
- 읽기 복제본을 사용하는 경우 쓰기 불가

### 올바른 방법

데이터 변경이 필요하면 별도의 Command를 발행합니다.

---

## 3. Command DTO와 Query DTO 공유하기

### 안티패턴

```csharp
// Command와 Query에서 같은 DTO를 사용
public record OrderDto(
    string Id,
    string CustomerId,
    string CustomerName,       // Query에만 필요
    List<OrderItemDto> Items,  // Command에만 필요
    decimal TotalAmount,
    string StatusText,         // Query에만 필요
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**문제점:**
- Command에 불필요한 읽기 전용 필드 포함
- Query에 불필요한 쓰기 전용 필드 포함
- 한쪽의 변경이 다른 쪽에 영향

### 올바른 방법

```csharp
// Command DTO: 쓰기에 필요한 필드만
public record CreateOrderCommand(
    CustomerId CustomerId,
    List<CreateOrderItemDto> Items)
    : ICommandRequest<OrderId>;

// Query DTO: 읽기에 최적화된 필드
public record OrderDto(
    string Id,
    string CustomerName,
    decimal TotalAmount,
    string StatusText,
    int ItemCount,
    DateTime CreatedAt);
```

---

## 4. 페이지네이션 없이 전체 조회하기

### 안티패턴

```csharp
// 전체 데이터를 한 번에 조회
var allOrders = await query.Search(
    Specification<Order>.All,
    new PageRequest(1, int.MaxValue),  // 전체 조회
    SortExpression.Empty).RunAsync();
```

**문제점:**
- 메모리 부족 위험 (대량 데이터)
- 응답 시간 급증
- 데이터베이스 부하

### 올바른 방법

```csharp
// 적절한 페이지네이션 적용
var pagedOrders = await query.Search(
    spec,
    new PageRequest(page: 1, size: 20),
    SortExpression.By("CreatedAt", SortDirection.Descending)).RunAsync();

// 대량 데이터는 Stream 사용
await foreach (var dto in query.Stream(spec, sort, ct))
{
    // 건별 처리
}
```

---

## 5. 도메인 이벤트 무시하기

### 안티패턴

```csharp
// Aggregate의 상태 변경 후 이벤트를 발행하지 않음
public class CancelOrderUsecase(IRepository<Order, OrderId> repository)
    : ICommandUsecase<CancelOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CancelOrderCommand command, CancellationToken ct)
    {
        var fin = await repository.GetById(command.OrderId).RunAsync();
        var order = fin.ThrowIfFail();
        order.Cancel();  // 이벤트가 추가되지 않으면 다른 바운디드 컨텍스트에 통지 불가
        await repository.Update(order).RunAsync();
        // 재고 복원, 결제 취소 등은 어떻게?
    }
}
```

**문제점:**
- 관련 시스템에 상태 변경이 전파되지 않음
- 바운디드 컨텍스트 간 정합성 깨짐

### 올바른 방법

```csharp
// Aggregate 내부에서 도메인 이벤트 발행
public class Order : AggregateRoot<OrderId>
{
    public void Cancel()
    {
        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id));  // 이벤트 추가
    }
}
// 트랜잭션 파이프라인이 SaveChanges 후 도메인 이벤트를 자동 발행
```

---

## 6. Usecase에서 직접 DbContext 사용하기

### 안티패턴

```csharp
// Usecase에서 인프라 계층에 직접 의존
public class CreateOrderUsecase(AppDbContext dbContext)
    : ICommandUsecase<CreateOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CreateOrderCommand command, CancellationToken ct)
    {
        var entity = new OrderEntity { ... };
        dbContext.Orders.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return FinResponse.Succ(new OrderId(entity.Id));
    }
}
```

**문제점:**
- Application 계층이 Infrastructure 계층에 직접 의존
- 테스트 시 실제 DB 또는 복잡한 모킹 필요
- 도메인 로직과 영속화 로직이 혼합

### 올바른 방법

```csharp
// IRepository 추상화를 통해 영속화
public class CreateOrderUsecase(IRepository<Order, OrderId> repository)
    : ICommandUsecase<CreateOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(OrderId.New(), command.CustomerId);
        var fin = await repository.Create(order).RunAsync();
        return fin.ToFinResponse(o => o.Id);
    }
}
```

---

## 7. 모든 곳에 CQRS 적용하기

### 안티패턴

단순한 설정 관리, 코드 테이블 관리 등 CRUD만으로 충분한 기능에도 CQRS를 적용합니다.

**문제점:**
- 불필요한 복잡도 증가
- 개발 속도 저하
- 유지보수 비용 증가

### 올바른 방법

CQRS는 복잡한 도메인 로직이 있거나 읽기/쓰기 요구사항이 다른 경우에 적용합니다. 단순 CRUD는 그대로 두어도 됩니다. (부록 A 참조)

---

## 안티패턴 체크리스트

코드 리뷰 시 아래 체크리스트로 안티패턴을 빠르게 점검하세요.

| 안티패턴 | 증상 | 해결 |
|---------|------|------|
| Repository로 목록 조회 | 느린 목록, 메모리 사용량 증가 | IQueryPort 사용 |
| Query에서 데이터 변경 | CQS 위반, 트랜잭션 문제 | 별도 Command 발행 |
| DTO 공유 | 불필요한 필드, 양쪽 영향 | Command/Query DTO 분리 |
| 페이지네이션 미적용 | OOM, 느린 응답 | PageRequest/Stream 사용 |
| 도메인 이벤트 무시 | 시스템 간 불일치 | AddDomainEvent 사용 |
| DbContext 직접 사용 | 계층 위반, 테스트 어려움 | IRepository 사용 |
| 과도한 CQRS 적용 | 불필요한 복잡도 | 단순 CRUD로 충분한 경우 식별 |

---

이 튜토리얼에서 사용한 CQRS 관련 용어의 정의와 코드 예시를 확인합니다.

→ [부록 E: 용어집](../E-glossary/)
