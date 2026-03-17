---
title: "Repository vs Query 선택"
---
## 개요

Functorium CQRS에서 데이터 접근은 **IRepository**(Command 측)와 **IQueryPort**(Query 측) 두 가지 경로로 나뉩니다. 이 가이드는 상황에 따라 어떤 경로를 선택해야 하는지 안내합니다.

---

## IRepository vs IQueryPort 비교

두 인터페이스의 설계 목적과 사용 방식이 어떻게 다른지 확인하세요.

| 특성 | IRepository | IQueryPort |
|------|-----------|-----------|
| **목적** | Aggregate Root 단위 영속화 | 읽기 전용 조회 |
| **대상** | AggregateRoot\<TId\> | DTO 프로젝션 |
| **반환 타입** | FinT\<IO, TAggregate\> | FinT\<IO, PagedResult\<TDto\>\> |
| **메서드** | Create, GetById, Update, Delete | Search, SearchByCursor, Stream |
| **트랜잭션** | IUnitOfWork와 함께 사용 | 불필요 |
| **페이지네이션** | 없음 (ID 기반 조회) | Offset, Cursor, Stream |
| **Specification** | 사용하지 않음 | 검색 조건으로 사용 |
| **구현체** | InMemoryRepositoryBase, EfCoreRepositoryBase | InMemoryQueryBase, DapperQueryBase |

### Specification과의 관계

`Specification<T>`는 IQueryPort의 핵심 검색 매개변수입니다. Search, SearchByCursor, Stream 메서드는 모두 `Specification<TEntity>`를 첫 번째 매개변수로 받아 동적 필터링을 수행합니다. Specification의 And, Or, Not 조합은 Query 측에서 복합 검색 조건을 구성하는 데 사용됩니다.

Specification 패턴의 상세 학습은 [Specification 패턴으로 도메인 규칙 구현하기](../../specification-pattern/)를 참조하세요.

---

## 선택 기준

### IRepository를 사용해야 하는 경우

데이터를 변경하거나 도메인 로직을 실행해야 하는 상황에서 사용합니다.

| 상황 | 이유 |
|------|------|
| 데이터 생성/수정/삭제 | Repository는 Command 전용 |
| ID로 단건 조회 후 수정 | GetById -> 도메인 로직 -> Update |
| 도메인 불변식 검증 필요 | Aggregate Root의 비즈니스 규칙 실행 |
| 트랜잭션이 필요한 작업 | IUnitOfWork와 함께 사용 |
| 도메인 이벤트 발행 | AggregateRoot의 도메인 이벤트 수집 |

```csharp
// IRepository 사용 예: 주문 취소 (Command)
public class CancelOrderUsecase(
    IRepository<Order, OrderId> repository)
    : ICommandUsecase<CancelOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CancelOrderCommand command, CancellationToken ct)
    {
        var pipeline =
            from order in repository.GetById(command.OrderId)
            from _     in guard(order.CanCancel(), Error.New("취소 불가"))
            from __    in repository.Update(order.Cancel())
            select order.Id;

        var fin = await pipeline.RunAsync();
        return fin.ToFinResponse();
    }
}
```

### IQueryPort를 사용해야 하는 경우

읽기 전용 조회, 특히 목록/검색/집계가 필요한 상황에서 사용합니다.

| 상황 | 이유 |
|------|------|
| 목록 조회 | 페이지네이션 + 정렬 지원 |
| 검색 기능 | Specification 기반 동적 필터링 |
| DTO 프로젝션 | 필요한 필드만 선택하여 반환 |
| 조인이 필요한 조회 | 여러 테이블의 데이터를 하나의 DTO로 |
| 대량 데이터 스트리밍 | Stream 메서드로 메모리 효율적 조회 |
| 읽기 성능 최적화 | Dapper 등으로 SQL 직접 제어 |

```csharp
// IQueryPort 사용 예: 주문 목록 검색 (Query)
public class SearchOrdersUsecase(
    IQueryPort<Order, OrderDto> query)
    : IQueryUsecase<SearchOrdersQuery, PagedResult<OrderDto>>
{
    public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
        SearchOrdersQuery request, CancellationToken ct)
    {
        var spec = BuildSpec(request);
        var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
        return fin.ToFinResponse();
    }

    private static Specification<Order> BuildSpec(SearchOrdersQuery request)
    {
        var spec = Specification<Order>.All;
        if (request.CustomerId is not null)
            spec &= new OrderByCustomerSpec(request.CustomerId.Value);
        if (request.Status is not null)
            spec &= new OrderByStatusSpec(request.Status.Value);
        return spec;
    }
}
```

---

## 의사결정 트리

```
데이터를 변경하는가? (Create/Update/Delete)
├── Yes -> IRepository
│         └── IUnitOfWork로 트랜잭션 관리
└── No (읽기 전용)
    ├── ID로 단건 조회 후 비즈니스 로직 실행?
    │   ├── Yes -> IRepository.GetById
    │   └── No -> 계속
    ├── 목록 조회 + 페이지네이션?
    │   └── Yes -> IQueryPort.Search / SearchByCursor
    ├── 대량 데이터 스트리밍?
    │   └── Yes -> IQueryPort.Stream
    └── 단순 DTO 프로젝션?
        └── Yes -> IQueryPort.Search
```

---

## 일반적인 시나리오별 가이드

실무에서 자주 만나는 시나리오별로 어떤 경로를 선택해야 하는지 정리합니다.

| 시나리오 | 선택 | 이유 |
|---------|------|------|
| 주문 생성 | IRepository.Create | Aggregate 생성 + 불변식 검증 |
| 주문 상태 변경 | IRepository.GetById + Update | 도메인 로직 실행 필요 |
| 주문 목록 조회 | IQueryPort.Search | 페이지네이션 + DTO 프로젝션 |
| 주문 상세 조회 (표시용) | IQueryPort.Search | 조인된 DTO 필요 |
| 주문 상세 조회 (수정용) | IRepository.GetById | 도메인 모델 필요 |
| 주문 검색 | IQueryPort.Search | Specification 기반 동적 필터 |
| 대시보드 집계 | IQueryPort.Search | 읽기 전용 DTO |
| 데이터 내보내기 | IQueryPort.Stream | 대량 데이터 스트리밍 |

---

## 안티패턴

### IRepository로 목록 조회하기

```csharp
// 안티패턴: Repository의 GetByIds로 목록 조회
var ids = await GetAllOrderIds(); // 전체 ID 목록을 먼저 가져옴
var orders = await repository.GetByIds(ids).RunAsync(); // Aggregate 전체 로드
var dtos = orders.Map(o => o.ToDto()); // 수동 변환

// 올바른 방법: IQueryPort 사용
var result = await query.Search(spec, page, sort).RunAsync();
```

### IQueryPort로 쓰기 작업하기

Query 측은 읽기 전용입니다. 데이터 변경은 반드시 IRepository를 통해야 합니다.

### 같은 Usecase에서 IRepository와 IQueryPort 혼합

```csharp
// 안티패턴: Command Usecase에서 IQueryPort 사용
public class CreateOrderUsecase(
    IRepository<Order, OrderId> repository,
    IQueryPort<Order, OrderDto> query)  // Command에 Query를 혼합
{ ... }

// 올바른 방법: Command는 IRepository만, Query는 IQueryPort만
```

---

## 다음 단계

FinT와 FinResponse 타입 참조를 확인합니다.
