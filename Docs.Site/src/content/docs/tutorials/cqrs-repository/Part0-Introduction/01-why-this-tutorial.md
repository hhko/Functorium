---
title: "왜 CQRS인가"
---
## 개요

주문 목록 API에 새 필터가 필요할 때마다 Repository에 메서드를 추가하고 있나요? `GetByCustomer`, `GetRecent`, `GetSummaries`, `Search`... 조회 조건이 늘어날수록 메서드도 끝없이 불어나고, 읽기 전용 필드가 도메인 모델에 스며들어 쓰기 로직을 오염시킵니다.

이 튜토리얼은 그 문제를 **Command와 Query의 책임 분리(CQRS)로** 해결합니다. 도메인 엔티티 기초에서 시작하여 Repository 패턴, Query 어댑터, Usecase 통합까지, **22개의 실습 프로젝트**를 통해 CQRS 패턴의 모든 측면을 단계별로 학습합니다.

---

## 하나의 모델로 모든 것을 처리하는 문제

### 전통적인 CRUD 방식

대부분의 애플리케이션은 하나의 모델로 읽기와 쓰기를 모두 처리합니다. 다음 코드를 보세요.

```csharp
// 하나의 Repository가 모든 책임을 짊어짐
public interface IOrderRepository
{
    // 쓰기 (Command)
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid id);

    // 읽기 (Query)
    Task<Order> GetByIdAsync(Guid id);
    Task<List<Order>> GetByCustomerAsync(Guid customerId);
    Task<List<Order>> GetRecentOrdersAsync(int count);
    Task<List<OrderSummary>> GetOrderSummariesAsync(int page, int size);
    Task<List<Order>> SearchAsync(string keyword, DateTime? from, DateTime? to);
    // ... 조회 조건이 늘어날 때마다 메서드 추가
}
```

하나의 인터페이스에 쓰기 4개, 읽기 5개 메서드가 뒤섞여 있고, 새 조회 조건이 생길 때마다 메서드가 추가됩니다. 다음 표는 이 방식이 만들어내는 구체적인 문제를 정리합니다.

| 문제 | 설명 |
|------|------|
| **읽기/쓰기 요구사항 충돌** | 쓰기는 도메인 불변식 검증이 필요하고, 읽기는 빠른 프로젝션이 필요 |
| **모델 비대화** | 읽기 전용 필드와 쓰기 전용 로직이 하나의 클래스에 혼재 |
| **성능 최적화 어려움** | 읽기와 쓰기의 성능 특성이 다르지만 동일 경로를 사용 |
| **메서드 폭발** | 조회 조건 조합마다 새로운 메서드가 필요 |
| **테스트 복잡도** | 하나의 Repository에 대한 테스트가 비대해짐 |

---

## CQRS가 해결하는 것

CQRS(Command Query Responsibility Segregation)는 **쓰기 모델과 읽기 모델을 분리**하여 각각의 요구사항에 맞게 최적화합니다. 쓰기는 Aggregate Root 단위로, 읽기는 Specification 기반 동적 검색으로 접근하면 위 문제가 모두 해소됩니다.

```csharp
// Command 측: Aggregate Root 단위 영속화
public interface IRepository<TAggregate, TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);
}

// Query 측: DTO 프로젝션 + 페이지네이션
public interface IQueryPort<TEntity, TDto>
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);
}
```

다음 표는 앞서 나열한 각 문제가 CQRS에서 어떻게 해결되는지 대응시킵니다.

| 문제 | CQRS 해결 방식 |
|------|----------------|
| 읽기/쓰기 충돌 | Command(IRepository)와 Query(IQueryPort)를 분리 |
| 모델 비대화 | Command는 도메인 모델, Query는 DTO로 분리 |
| 성능 최적화 | 읽기와 쓰기를 독립적으로 최적화 가능 |
| 메서드 폭발 | Specification 기반 동적 검색으로 해결 |
| 테스트 복잡도 | Command와 Query를 독립적으로 테스트 |

---

## 대상 독자

여러분의 경험 수준에 따라 학습 범위를 선택할 수 있습니다.

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 기본 문법을 알고 CQRS 패턴에 입문하려는 개발자 | Part 1 (1장~4장) |
| **중급** | 패턴을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 (1장~13장) |
| **고급** | 아키텍처 설계와 도메인 모델링에 관심 있는 개발자 | Part 4~5 + 부록 |

---

## 학습 전제 조건

이 튜토리얼을 효과적으로 학습하려면 C# 기본 문법(클래스, 인터페이스, 제네릭)과 객체지향 프로그래밍 기초 개념을 이해하고 있어야 하며, .NET 프로젝트를 실행해 본 경험이 필요합니다.

LINQ 기본 문법, 단위 테스트 경험, 도메인 주도 설계(DDD) 기초 개념(Entity, Aggregate Root), Entity Framework Core 기본 사용 경험이 있으면 학습이 더 수월합니다. 다만 이들은 필수가 아니므로 튜토리얼을 진행하면서 익혀도 됩니다.

---

## 기대 효과

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

### 1. Aggregate Root 단위 Repository로 쓰기 작업 구현

IRepository를 통해 도메인 모델의 불변식을 보장하면서 영속화할 수 있습니다.

```csharp
// IRepository로 Aggregate 단위 영속화
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

### 2. Query 어댑터로 읽기 전용 최적화 조회 구현

IQueryPort와 Specification을 조합하면 조회 조건이 늘어나도 메서드를 추가할 필요가 없습니다.

```csharp
// IQueryPort로 DTO 프로젝션 + 페이지네이션
public class SearchOrdersUsecase(IQueryPort<Order, OrderDto> query)
    : IQueryUsecase<SearchOrdersQuery, PagedResult<OrderDto>>
{
    public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
        SearchOrdersQuery request, CancellationToken ct)
    {
        var spec = new OrderByCustomerSpec(request.CustomerId);
        var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
        return fin.ToFinResponse();
    }
}
```

### 3. FinT 모나드로 함수형 파이프라인 구성

여러 Repository 호출을 `from...select` 구문으로 연결하면 에러 처리가 자동으로 전파됩니다.

```csharp
// from...select 구문으로 모나딕 합성
var pipeline =
    from order in repository.GetById(orderId)
    from _     in guard(order.CanCancel(), Error.New("취소 불가"))
    from __    in repository.Update(order.Cancel())
    select order.Id;

var fin = await pipeline.RunAsync();
return fin.ToFinResponse();
```

### 4. 트랜잭션 파이프라인으로 일관성 보장

Command Usecase는 트랜잭션 파이프라인을 자동으로 통과하므로, SaveChanges와 도메인 이벤트 발행을 직접 호출할 필요가 없습니다.

```csharp
// Command는 자동으로 트랜잭션 파이프라인을 통과
// SaveChanges + 도메인 이벤트 발행이 자동 처리됨
ICommandRequest<TSuccess> -> UsecaseTransactionPipeline -> ICommandUsecase
```

---

## 이 튜토리얼의 구성

```
Part 0: 서론
├── CQRS 패턴의 개념과 필요성
├── 환경 설정
└── CQRS 아키텍처 개요

Part 1: 도메인 엔티티 기초
├── Entity<TId>와 IEntityId
├── AggregateRoot<TId>
├── 도메인 이벤트
└── 엔티티 인터페이스 (IAuditable, ISoftDeletable)

Part 2: Command 측 -- Repository 패턴
├── IRepository<TAggregate, TId> 인터페이스
├── InMemory Repository 구현
├── EF Core Repository 구현
└── Unit of Work 패턴

Part 3: Query 측 -- 읽기 전용 패턴
├── IQueryPort<TEntity, TDto> 인터페이스
├── Command DTO vs Query DTO 분리
├── 페이지네이션과 정렬
├── InMemory Query 어댑터
└── Dapper Query 어댑터

Part 4: CQRS Usecase 통합
├── Command/Query Usecase
├── FinT -> FinResponse 변환
├── 도메인 이벤트 흐름
└── 트랜잭션 파이프라인

Part 5: 도메인별 실전 예제
├── 주문 관리
├── 고객 관리
├── 재고 관리
└── 카탈로그 검색
```

---

## Tutorial과의 차이점

다음 표는 빠른 실습 중심의 Tutorial과 이 튜토리얼의 접근 방식 차이를 비교합니다.

| 구분 | Tutorial | 이 튜토리얼 |
|------|----------|-------|
| **목적** | 빠른 실습과 결과 확인 | 개념 이해와 설계 원리 학습 |
| **깊이** | 핵심 사용법 중심 | 내부 구현과 원리 심화 |
| **범위** | CQRS 기본 사용 | Repository, Query 어댑터, 트랜잭션, 이벤트 |
| **대상** | 바로 적용하려는 개발자 | 패턴을 깊이 이해하려는 개발자 |

---

## 학습 경로

```
초급 (Part 1: 1~4장)
├── Entity와 Identity 구현
├── Aggregate Root와 도메인 불변식
├── 도메인 이벤트
└── 엔티티 인터페이스

중급 (Part 2~3: 5~13장)
├── Repository 인터페이스와 구현
├── Unit of Work 패턴
├── Query 어댑터와 DTO 분리
└── 페이지네이션과 정렬

고급 (Part 4~5 + 부록)
├── Command/Query Usecase 통합
├── FinT 모나딕 합성
├── 트랜잭션 파이프라인
└── 도메인별 실전 예제
```

---

## 다음 단계

CQRS가 왜 필요한지 확인했으니, 이제 개발 환경을 준비할 차례입니다. 다음 장에서는 .NET SDK 설치, VS Code 설정, 튜토리얼 프로젝트 클론까지 환경 설정 전 과정을 안내합니다.
