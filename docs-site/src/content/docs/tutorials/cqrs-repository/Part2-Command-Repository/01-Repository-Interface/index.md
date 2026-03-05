---
title: "리포지토리 인터페이스"
---
## 개요

`IRepository<TAggregate, TId>`는 Aggregate Root 단위 영속화를 위한 공통 인터페이스입니다.
제네릭 제약을 통해 Aggregate Root만 Repository의 대상이 될 수 있도록 컴파일 타임에 강제하며,
모든 메서드는 `FinT<IO, T>`를 반환하여 부수 효과와 오류 처리를 합성 가능한 형태로 다룹니다.

---

## 학습 목표

- `IRepository<TAggregate, TId>` 인터페이스의 8개 CRUD 메서드를 이해합니다.
- `FinT<IO, T>` 반환 타입의 의미와 합성 가능성을 파악합니다.
- 제네릭 제약(`AggregateRoot<TId>`, `IEntityId<TId>`)의 역할을 학습합니다.
- 도메인 특화 Repository 인터페이스를 정의하는 방법을 익힙니다.

---

## 핵심 개념

### 8개 CRUD 메서드

| 구분 | 메서드 | 반환 타입 |
|------|--------|-----------|
| 단건 생성 | `Create(TAggregate)` | `FinT<IO, TAggregate>` |
| 단건 조회 | `GetById(TId)` | `FinT<IO, TAggregate>` |
| 단건 수정 | `Update(TAggregate)` | `FinT<IO, TAggregate>` |
| 단건 삭제 | `Delete(TId)` | `FinT<IO, int>` |
| 복수 생성 | `CreateRange(IReadOnlyList<TAggregate>)` | `FinT<IO, Seq<TAggregate>>` |
| 복수 조회 | `GetByIds(IReadOnlyList<TId>)` | `FinT<IO, Seq<TAggregate>>` |
| 복수 수정 | `UpdateRange(IReadOnlyList<TAggregate>)` | `FinT<IO, Seq<TAggregate>>` |
| 복수 삭제 | `DeleteRange(IReadOnlyList<TId>)` | `FinT<IO, int>` |

### FinT\<IO, T\> 반환 타입

```
FinT<IO, T> = IO<Fin<T>>
            = IO<Succ(T) | Fail(Error)>
```

- **Fin\<T\>**: 성공(`Succ`) 또는 실패(`Fail`)를 표현하는 Result 타입
- **IO**: 부수 효과(Side Effect)를 추적하는 모나드
- **FinT**: 두 모나드의 합성 (Monad Transformer)

### 제네릭 제약

```csharp
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>  // Aggregate Root만 허용
    where TId : struct, IEntityId<TId>     // 값 타입 ID만 허용
```

- `AggregateRoot<TId>` 제약: Entity나 Value Object는 직접 영속화할 수 없습니다.
- `IEntityId<TId>` 제약: Ulid 기반 식별자를 강제합니다.

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

**IProductRepository** -- IRepository를 확장한 도메인 특화 인터페이스:

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, bool> Exists(Specification<Product> spec);
}
```

`IRepository`의 8개 CRUD 메서드를 상속받으면서, 도메인에 필요한 `Exists` 메서드를 추가합니다.

---

## 한눈에 보는 정리

| 항목 | 설명 |
|------|------|
| 인터페이스 | `IRepository<TAggregate, TId>` |
| CRUD 메서드 | 단건 4개 + 복수 4개 = 8개 |
| 반환 타입 | `FinT<IO, T>` (합성 가능한 모나드) |
| 제약 조건 | Aggregate Root + Ulid 기반 ID |
| 확장 방법 | 도메인 특화 인터페이스로 상속 |

---

## FAQ

**Q: 왜 Repository는 Aggregate Root만 다루나요?**
A: DDD에서 Aggregate Root는 일관성 경계(Consistency Boundary)입니다. 내부 Entity는 Aggregate Root를 통해서만 접근해야 하므로, Repository도 Aggregate Root 단위로 동작합니다.

**Q: FinT\<IO, T\>가 Task\<T\>보다 나은 점은 무엇인가요?**
A: `Task<T>`는 예외를 던지지만, `FinT<IO, T>`는 실패를 값으로 표현합니다. 이를 통해 오류 처리를 합성(compose)할 수 있고, 예외 기반 제어 흐름을 피할 수 있습니다.

**Q: IReadOnlyList를 매개변수로 쓰는 이유는 무엇인가요?**
A: `IReadOnlyList<T>`는 인덱스 접근과 Count를 제공하면서도 변경을 허용하지 않아, 안전하고 유연한 컬렉션 인터페이스입니다. `List<T>`, 배열, `Seq<T>` 등 다양한 타입을 받을 수 있습니다.
