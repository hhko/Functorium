---
title: "용어집"
---
## A

### Aggregate Root (집합체 루트)
도메인 모델에서 일관성 경계(consistency boundary)를 정의하는 최상위 엔티티. 외부에서는 Aggregate Root를 통해서만 내부 엔티티에 접근합니다. Functorium에서는 `AggregateRoot<TId>`로 구현합니다.

```csharp
public class Order : AggregateRoot<OrderId>
{
    public void AddItem(Product product, int qty) { /* ... */ }
}
```

### AggregateRoot\<TId\>
Functorium의 Aggregate Root 추상 클래스. `Entity<TId>`를 상속하며, 도메인 이벤트 관리 기능을 포함합니다.

### AuditableEntity (감사 가능 엔티티)
`IAuditable` 인터페이스를 구현한 엔티티. 생성 시간, 수정 시간 등 감사 정보를 자동으로 추적합니다.

---

## C

### Command (명령)
시스템의 상태를 변경하는 요청. 값을 반환하지 않거나 결과만 반환합니다. CQRS에서 쓰기 작업을 담당합니다.

### CQS (Command Query Separation)
Bertrand Meyer가 정의한 원칙. 메서드를 Command(상태 변경, 반환 없음)와 Query(상태 변경 없음, 값 반환)로 분리합니다.

### CQRS (Command Query Responsibility Segregation)
Greg Young이 CQS를 아키텍처 수준으로 확장한 패턴. 읽기 모델과 쓰기 모델을 분리하여 각각 최적화합니다.

### CursorPagedResult\<T\>
Cursor 기반 페이지네이션 결과를 담는 타입. 다음 페이지를 가리키는 커서 값을 포함합니다.

### CursorPageRequest
Cursor 기반 페이지네이션 요청. 이전 페이지의 마지막 커서 값과 페이지 크기를 포함합니다. Offset 방식보다 deep page에서 성능이 우수합니다.

---

## D

### Dapper
경량 ORM(Object-Relational Mapper). SQL을 직접 작성하면서 객체 매핑을 제공합니다. Query 측 어댑터에서 읽기 성능 최적화에 사용합니다.

### DapperQueryBase
Functorium의 Dapper 기반 Query 어댑터 추상 클래스. SQL을 생성하여 IQueryPort를 구현합니다.

### Domain Event (도메인 이벤트)
도메인에서 발생한 의미 있는 사건을 나타내는 객체. `IDomainEvent` 인터페이스를 구현합니다.

```csharp
public record OrderCreatedEvent(OrderId OrderId) : IDomainEvent;
```

### DomainEventCollector
도메인 이벤트를 수집하고 발행하는 서비스. Aggregate Root에서 추가된 이벤트를 트랜잭션 커밋 후 발행합니다.

### DTO (Data Transfer Object)
계층 간 데이터 전달을 위한 객체. CQRS에서 Query 측은 DTO를 반환하여 읽기에 최적화합니다.

---

## E

### EF Core (Entity Framework Core)
Microsoft의 ORM 프레임워크. Command 측 Repository 구현에 사용합니다.

### EfCoreRepositoryBase
Functorium의 EF Core 기반 Repository 추상 클래스. `ToDomain`/`ToModel` 변환 메서드를 통해 도메인 모델과 DB 모델을 분리합니다.

### Entity\<TId\>
Functorium의 엔티티 추상 클래스. ID로 식별되는 도메인 객체의 기본 클래스입니다.

---

## F

### Fin\<T\>
LanguageExt의 Result 타입. 성공(T) 또는 실패(Error)를 표현합니다. `IsSucc`/`IsFail`로 상태를 확인합니다.

### FinResponse\<T\>
Functorium의 Usecase 반환 타입. `Fin<T>`를 기반으로 하며 Mediator 파이프라인과 호환됩니다.

### FinT\<IO, T\>
LanguageExt의 모나드 변환자. `IO<Fin<T>>`를 래핑하여 함수형 합성을 지원합니다. Repository 메서드의 반환 타입입니다.

---

## G

### guard()
LanguageExt의 함수. 조건이 충족되지 않으면 FinT 파이프라인을 실패시킵니다.

```csharp
from _ in guard(order.CanCancel(), Error.New("취소 불가"))
```

---

## I

### IAuditable
감사 정보(생성 시간, 수정 시간 등)를 추적하는 인터페이스.

### ICommandRequest\<TSuccess\>
Command Usecase의 요청 인터페이스. `ICommand<FinResponse<TSuccess>>`를 상속합니다.

### ICommandUsecase\<TCommand, TSuccess\>
Command Usecase의 핸들러 인터페이스. `ICommandHandler`를 상속합니다.

### IDomainEvent
도메인 이벤트의 마커 인터페이스.

### IDomainEventCollector
도메인 이벤트를 수집하고 추적하는 인터페이스. Track 메서드로 Aggregate의 이벤트를 수집합니다.

### IEntityId\<TId\>
Entity ID의 인터페이스. Ulid 기반으로 구현됩니다.

### InMemoryQueryBase
Functorium의 InMemory 기반 Query 어댑터 추상 클래스. 테스트용으로 사용합니다.

### InMemoryRepositoryBase
Functorium의 InMemory 기반 Repository 추상 클래스. `ConcurrentDictionary`를 사용하여 메모리에 데이터를 저장합니다.

### IO
LanguageExt의 순수 함수형 IO 효과 타입. 부수 효과를 명시적으로 표현합니다.

### IQueryPort\<TEntity, TDto\>
Query 측 어댑터 인터페이스. Specification 기반 검색, 페이지네이션, 스트리밍을 지원합니다.

### IQueryRequest\<TSuccess\>
Query Usecase의 요청 인터페이스. `IQuery<FinResponse<TSuccess>>`를 상속합니다.

### IQueryUsecase\<TQuery, TSuccess\>
Query Usecase의 핸들러 인터페이스. `IQueryHandler`를 상속합니다.

### IRepository\<TAggregate, TId\>
Command 측 Repository 인터페이스. Aggregate Root 단위의 8개 CRUD 메서드를 정의합니다.

### ISoftDeletable
논리 삭제를 지원하는 인터페이스. 물리적으로 삭제하지 않고 삭제 플래그를 설정합니다.

### IUnitOfWork
작업 단위 인터페이스. `SaveChanges()`로 변경사항을 영속화하고, `BeginTransactionAsync()`로 명시적 트랜잭션을 시작합니다.

### IUnitOfWorkTransaction
명시적 트랜잭션 스코프 인터페이스. `CommitAsync()`로 커밋하며, Dispose 시 미커밋 트랜잭션은 자동 롤백됩니다.

---

## M

### Mediator 패턴
요청과 핸들러 사이의 직접적인 의존성을 제거하는 패턴. Functorium은 Mediator 라이브러리를 사용하여 Command/Query를 디스패치합니다.

---

## P

### PagedResult\<T\>
Offset 기반 페이지네이션 결과를 담는 타입. 전체 건수, 현재 페이지, 페이지 크기, 데이터 목록을 포함합니다.

### PageRequest
Offset 기반 페이지네이션 요청. 페이지 번호와 페이지 크기를 포함합니다.

### Pipeline (파이프라인)
Mediator의 요청 처리 파이프라인. 검증, 로깅, 트랜잭션 등의 횡단 관심사를 처리합니다.

---

## Q

### Query (질의)
시스템의 상태를 변경하지 않고 데이터를 반환하는 요청. CQRS에서 읽기 작업을 담당합니다.

### Query Adapter (질의 어댑터)
IQueryPort의 구현체. InMemoryQueryBase, DapperQueryBase 등이 있습니다.

---

## R

### Repository 패턴
데이터 접근 로직을 추상화하는 패턴. CQRS에서 Command 측의 Aggregate Root 영속화를 담당합니다.

---

## S

### SortExpression
정렬 조건을 표현하는 타입. 정렬 필드와 방향(오름차순/내림차순)을 포함합니다.

### Specification\<T\>
비즈니스 규칙을 캡슐화하는 추상 클래스. `IsSatisfiedBy` 메서드로 후보 객체의 충족 여부를 판단합니다. IQueryPort의 검색 조건으로 사용됩니다.

```csharp
var spec = new ActiveOrderSpec() & new OrderByCustomerSpec(customerId);
var result = await query.Search(spec, page, sort).RunAsync();
```

### Stream
IQueryPort의 스트리밍 조회 메서드. `IAsyncEnumerable<TDto>`를 반환하여 대량 데이터를 메모리에 전체 적재하지 않고 건별 처리합니다.

---

## T

### ToFinResponse()
`Fin<T>`를 `FinResponse<T>`로 변환하는 확장 메서드. Repository 계층에서 Usecase 계층으로의 타입 변환에 사용합니다.

### Transaction Pipeline (트랜잭션 파이프라인)
Command Usecase 실행 후 자동으로 SaveChanges를 호출하고 도메인 이벤트를 발행하는 파이프라인.

---

## U

### Ulid
Universally Unique Lexicographically Sortable Identifier. Functorium의 Entity ID는 Ulid 기반입니다. UUID보다 정렬 가능하고 시간순으로 생성됩니다.

### Unit of Work (작업 단위)
여러 Repository 작업을 하나의 트랜잭션으로 묶는 패턴. `IUnitOfWork.SaveChanges()`로 일괄 커밋합니다.

---

## V

### Value Object (값 객체)
ID가 아닌 값으로 식별되는 도메인 객체. 불변이며 동등성 비교가 가능합니다.

---

## 다음 단계

참고 자료를 확인합니다.
