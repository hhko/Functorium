# 부록 A. CQRS vs 전통적 CRUD

> **부록** | [목차](../README.md) | [다음: B. Repository vs Query 어댑터 선택 가이드 ->](B-repository-vs-query-adapter-guide.md)

---

## 개요

CQRS와 전통적 CRUD 방식을 비교하여 각 접근법의 장단점을 이해합니다. 모든 프로젝트에 CQRS가 필요한 것은 아니며, 상황에 맞는 올바른 선택이 중요합니다.

---

## 전체 비교

| 특성 | 전통적 CRUD | CQRS |
|------|-----------|------|
| **모델** | 단일 모델 (읽기/쓰기 공유) | 이중 모델 (Command/Query 분리) |
| **Repository** | 하나의 Repository | IRepository (쓰기) + IQueryPort (읽기) |
| **DTO** | 단일 DTO 또는 Entity 직접 노출 | Command DTO + Query DTO 분리 |
| **복잡도** | 낮음 | 중간~높음 |
| **확장성** | 제한적 | 읽기/쓰기 독립 확장 |
| **성능 최적화** | 일괄 적용 | 읽기/쓰기 개별 최적화 |
| **학습 곡선** | 낮음 | 중간 |

---

## 상세 비교

### 1. 단일 모델 vs 이중 모델

#### 전통적 CRUD: 단일 모델

```csharp
// 하나의 Entity가 모든 책임을 처리
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }

    // 읽기용 조인 결과도 같은 클래스에
    public string CustomerName { get; set; }
    public string StatusDisplayText { get; set; }
}
```

#### CQRS: 이중 모델

```csharp
// Command 측: 도메인 모델 (비즈니스 로직 포함)
public class Order : AggregateRoot<OrderId>
{
    public CustomerId CustomerId { get; private set; }
    private readonly List<OrderItem> _items = [];
    public OrderStatus Status { get; private set; }

    public void AddItem(Product product, int qty) { /* 불변식 검증 */ }
    public void Cancel() { /* 도메인 규칙 */ }
}

// Query 측: DTO (표시에 최적화)
public record OrderDto(
    string Id,
    string CustomerName,
    string StatusText,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt);
```

---

### 2. Repository 설계

#### 전통적 CRUD

```csharp
public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order> GetByIdAsync(Guid id);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid id);

    // 읽기 메서드가 점점 늘어남
    Task<List<Order>> GetByCustomerAsync(Guid customerId);
    Task<List<Order>> GetRecentAsync(int count);
    Task<PagedList<Order>> SearchAsync(OrderFilter filter, int page, int size);
    Task<List<OrderSummary>> GetSummariesAsync();
    // ...
}
```

#### CQRS

```csharp
// Command: 깔끔한 8개 CRUD 메서드
public interface IRepository<TAggregate, TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);
    // + Range 메서드 4개
}

// Query: Specification 기반 동적 검색
public interface IQueryPort<TEntity, TDto>
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);
}
```

---

### 3. 확장성

#### 전통적 CRUD

```
Client -> Service -> Repository -> 단일 DB
                                     |
                    읽기/쓰기가 같은 DB를 경유
                    읽기 트래픽이 쓰기 성능에 영향
```

#### CQRS

```
Client -> Mediator
             |
    Command Path          Query Path
         |                    |
    IRepository          IQueryPort
         |                    |
    쓰기 DB              읽기 DB (또는 같은 DB)
                              |
                    독립적으로 읽기 복제본 추가 가능
```

---

### 4. 복잡도 트레이드오프

#### 전통적 CRUD가 적합한 경우

| 상황 | 이유 |
|------|------|
| 단순한 데이터 입출력 | 도메인 로직이 거의 없음 |
| 읽기/쓰기 비율이 비슷 | 분리의 이점이 적음 |
| 소규모 팀/프로젝트 | CQRS의 초기 비용 대비 이점이 적음 |
| 관리자 CRUD 화면 | 읽기 최적화가 불필요 |
| 프로토타입 | 빠른 개발이 우선 |

#### CQRS가 적합한 경우

| 상황 | 이유 |
|------|------|
| 읽기가 쓰기보다 훨씬 많음 | 읽기 최적화의 이점이 큼 |
| 복잡한 도메인 로직 | Command 모델에 집중 가능 |
| 다양한 읽기 요구사항 | Query 모델을 용도별로 최적화 |
| 성능 요구사항이 높음 | 읽기/쓰기 독립 확장 |
| 이벤트 소싱 적용 | CQRS와 자연스러운 조합 |

---

## 선택 가이드

```
도메인 로직이 복잡한가?
├── No -> 읽기 요구사항이 다양한가?
│         ├── No -> 전통적 CRUD
│         └── Yes -> CQRS (Query 측만 분리)
└── Yes -> 읽기/쓰기 성능 요구사항이 다른가?
           ├── No -> CQRS (같은 DB)
           └── Yes -> CQRS (DB 분리 고려)
```

---

## 점진적 도입

CQRS는 전부 아니면 전무가 아닙니다. 점진적으로 도입할 수 있습니다:

### 1단계: DTO 분리

기존 Repository를 유지하면서 읽기용 DTO만 분리합니다.

### 2단계: Query 전용 경로 추가

IQueryPort를 도입하여 복잡한 읽기 요구사항을 처리합니다.

### 3단계: Command/Query Usecase 분리

Mediator 패턴으로 Command와 Query를 완전히 분리합니다.

### 4단계: 인프라 분리 (선택)

필요에 따라 읽기 DB 복제본이나 캐시 계층을 추가합니다.

---

## 다음 단계

Repository와 Query 어댑터의 선택 가이드를 확인합니다.

-> [B. Repository vs Query 어댑터 선택 가이드](B-repository-vs-query-adapter-guide.md)
