---
title: "리포지토리 인터페이스"
---
## 개요

모든 Repository가 `Create`, `GetById`, `Update`, `Delete`를 반복 정의해야 할까요?
도메인마다 동일한 CRUD 메서드를 복사-붙여넣기하면 코드 중복이 기하급수적으로 늘어납니다.
`IRepository<TAggregate, TId>`는 이 문제를 해결하는 공통 인터페이스입니다.
제네릭 제약을 통해 Aggregate Root만 Repository의 대상이 되도록 컴파일 타임에 강제하며,
모든 메서드는 `FinT<IO, T>`를 반환하여 부수 효과와 오류 처리를 합성 가능한 형태로 다룹니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `IRepository<TAggregate, TId>` 인터페이스의 8개 CRUD 메서드를 설명할 수 있습니다.
2. `FinT<IO, T>` 반환 타입이 왜 `Task<T>`보다 합성에 유리한지 설명할 수 있습니다.
3. 제네릭 제약(`AggregateRoot<TId>`, `IEntityId<TId>`)이 잘못된 사용을 어떻게 방지하는지 설명할 수 있습니다.
4. 도메인 특화 Repository 인터페이스를 직접 정의할 수 있습니다.

---

## 핵심 개념

### 왜 필요한가?

Product, Order, Customer 도메인마다 Repository 인터페이스를 따로 정의한다고 생각해 보세요.

```csharp
// Product Repository
FinT<IO, Product> Create(Product product);
FinT<IO, Product> GetById(ProductId id);
FinT<IO, Product> Update(Product product);
FinT<IO, int>     Delete(ProductId id);

// Order Repository - 같은 패턴을 또 반복
FinT<IO, Order> Create(Order order);
FinT<IO, Order> GetById(OrderId id);
FinT<IO, Order> Update(Order order);
FinT<IO, int>   Delete(OrderId id);

// Customer Repository - 또 반복...
```

Aggregate가 늘어날 때마다 동일한 시그니처를 복사합니다. 메서드 하나의 반환 타입이 바뀌면 모든 인터페이스를 수정해야 합니다.
`IRepository<TAggregate, TId>`는 이 공통 패턴을 제네릭으로 추출하여, 한 곳에서 정의하고 모든 도메인이 재사용하게 합니다.

### 8개 CRUD 메서드

다음 테이블은 `IRepository`가 제공하는 전체 메서드 목록입니다. 단건과 복수 버전이 대칭을 이루고 있어, 단일 Aggregate든 목록이든 동일한 패턴으로 다룰 수 있습니다.

| 구분 | 메서드 | 반환 타입 |
|------|--------|-----------|
| 단건 생성 | `Create(TAggregate)` | `FinT<IO, TAggregate>` |
| 단건 조회 | `GetById(TId)` | `FinT<IO, TAggregate>` |
| 단건 수정 | `Update(TAggregate)` | `FinT<IO, TAggregate>` |
| 단건 삭제 | `Delete(TId)` | `FinT<IO, int>` |
| 복수 생성 | `CreateRange(IReadOnlyList<TAggregate>)` | `FinT<IO, int>` |
| 복수 조회 | `GetByIds(IReadOnlyList<TId>)` | `FinT<IO, Seq<TAggregate>>` |
| 복수 수정 | `UpdateRange(IReadOnlyList<TAggregate>)` | `FinT<IO, int>` |
| 복수 삭제 | `DeleteRange(IReadOnlyList<TId>)` | `FinT<IO, int>` |

### FinT\<IO, T\> 반환 타입

왜 `Task<T>`가 아니라 `FinT<IO, T>`를 반환할까요? 다음 구조를 보세요.

```
FinT<IO, T> = IO<Fin<T>>
            = IO<Succ(T) | Fail(Error)>
```

- **Fin\<T\>은** 성공(`Succ`) 또는 실패(`Fail`)를 표현하는 Result 타입입니다. 예외를 던지지 않고 실패를 값으로 다룹니다.
- **IO는** 부수 효과(Side Effect)를 추적하는 모나드입니다. DB 접근 같은 부수 효과를 타입으로 명시합니다.
- **FinT는** 두 모나드의 합성(Monad Transformer)입니다. 여러 Repository 호출을 `|` 연산자로 체이닝할 수 있습니다.

### 제네릭 제약

다음 제약이 잘못된 Repository 사용을 컴파일 타임에 차단합니다.

```csharp
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>  // Aggregate Root만 허용
    where TId : struct, IEntityId<TId>     // 값 타입 ID만 허용
```

- `AggregateRoot<TId>` 제약: Entity나 Value Object를 직접 영속화하려고 하면 컴파일 에러가 발생합니다.
- `IEntityId<TId>` 제약: Ulid 기반 식별자를 강제하여 ID 생성 전략을 통일합니다.

### IObservablePort

IRepository는 `IObservablePort`를 상속합니다. `IObservablePort`는 `RequestCategory` 속성 하나를 가지며, Observability 파이프라인이 Command/Query를 구분하여 메트릭과 로그를 수집하는 데 사용됩니다. Repository 구현체는 `RequestCategory => "Command"`를 반환합니다.

---

## 프로젝트 설명

### 프로젝트 구조

```
01-Repository-Interface/
├── RepositoryInterface/
│   ├── RepositoryInterface.csproj
│   ├── Program.cs              # 콘솔 데모
│   ├── ProductId.cs            # Ulid 기반 식별자
│   ├── Product.cs              # Aggregate Root
│   └── IProductRepository.cs   # 도메인 특화 Repository 인터페이스
├── RepositoryInterface.Tests.Unit/
│   ├── RepositoryInterface.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── ProductTests.cs
└── README.md
```

### 핵심 코드

공통 인터페이스를 정의했으니, 도메인 특화 메서드는 어떻게 추가할까요? `IRepository`를 상속하면 됩니다.

**IProductRepository** -- IRepository를 확장한 도메인 특화 인터페이스:

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, bool> Exists(Specification<Product> spec);
}
```

`IRepository`의 8개 CRUD 메서드를 그대로 상속받으면서, Product 도메인에만 필요한 `Exists` 메서드를 추가합니다. 새로운 도메인이 추가되어도 CRUD 시그니처를 다시 정의할 필요가 없습니다.

---

## 한눈에 보는 정리

다음 테이블은 이 장에서 다룬 핵심 항목을 요약합니다.

| 항목 | 설명 |
|------|------|
| 인터페이스 | `IRepository<TAggregate, TId>` |
| CRUD 메서드 | 단건 4개 + 복수 4개 = 8개 |
| 반환 타입 | `FinT<IO, T>` (합성 가능한 모나드) |
| 제약 조건 | Aggregate Root + Ulid 기반 ID |
| 확장 방법 | 도메인 특화 인터페이스로 상속 |

---

## FAQ

### Q1: 왜 Repository는 Aggregate Root만 다루나요?
**A**: DDD에서 Aggregate Root는 일관성 경계(Consistency Boundary)입니다. 내부 Entity는 Aggregate Root를 통해서만 접근해야 하므로, Repository도 Aggregate Root 단위로 동작합니다.

### Q2: FinT\<IO, T\>가 Task\<T\>보다 나은 점은 무엇인가요?
**A**: `Task<T>`는 예외를 던지지만, `FinT<IO, T>`는 실패를 값으로 표현합니다. 이를 통해 오류 처리를 합성(compose)할 수 있고, 예외 기반 제어 흐름을 피할 수 있습니다.

### Q3: IReadOnlyList를 매개변수로 쓰는 이유는 무엇인가요?
**A**: `IReadOnlyList<T>`는 인덱스 접근과 Count를 제공하면서도 변경을 허용하지 않아, 안전하고 유연한 컬렉션 인터페이스입니다. `List<T>`, 배열, `Seq<T>` 등 다양한 타입을 받을 수 있습니다.

---

공통 Repository 인터페이스를 정의했습니다. 그런데 DB 없이 이 인터페이스를 테스트하려면 어떻게 해야 할까요? 다음 장에서는 ConcurrentDictionary 기반 InMemory Repository를 구현하여, 실제 DB 연결 없이 빠르게 동작을 검증하는 방법을 살펴봅니다.

→ [2장: InMemory Repository](../02-InMemory-Repository/)
