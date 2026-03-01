# CQRS 패턴으로 Command와 Query 분리하기

**C# Functorium으로 Repository와 Query 어댑터를 구현하는 실전 가이드**

---

## 이 튜토리얼에 대하여

이 튜토리얼은 **CQRS 패턴을 활용한 Command와 Query 분리**를 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 도메인 엔티티 기초에서 시작하여 Repository 패턴, Query 어댑터, Usecase 통합까지, **22개의 실습 프로젝트**를 통해 CQRS 패턴의 모든 측면을 체계적으로 학습할 수 있습니다.

> **하나의 모델로 읽기와 쓰기를 모두 처리하던 방식에서 벗어나, Command와 Query를 분리하여 각각 최적화하는 과정을 함께 경험해보세요.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기본 문법을 알고 CQRS 패턴에 입문하려는 개발자 | Part 1 (1장~4장) |
| 🟡 **중급** | 패턴을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 (1장~13장) |
| 🔴 **고급** | 아키텍처 설계와 도메인 모델링에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. **CQRS 패턴의 개념과 필요성**을 이해하고 Command/Query 분리 설계 적용
2. **IRepository 기반 Repository 패턴**으로 Aggregate Root 단위 영속화 구현
3. **IQueryPort 기반 Query 어댑터**로 읽기 전용 최적화 조회 구현
4. **FinT 모나드 합성**과 ToFinResponse 변환으로 함수형 파이프라인 구성
5. **트랜잭션 파이프라인**과 도메인 이벤트 흐름으로 완성도 높은 CQRS 아키텍처 구축

---

## 목차

### Part 0: 서론

서론에서는 CQRS 패턴의 개념과 환경 설정을 다룹니다.

- [0.1 왜 CQRS인가](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 환경 설정](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 CQRS 패턴 개요](Part0-Introduction/03-cqrs-pattern-overview.md)

### Part 1: 도메인 엔티티 기초

Entity, Aggregate Root, 도메인 이벤트 등 CQRS의 기반이 되는 도메인 모델링을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Entity와 Identity](Part1-Domain-Entity-Foundations/01-Entity-And-Identity/README.md) | Entity\<TId\>, IEntityId, Ulid 기반 ID |
| 2 | [Aggregate Root](Part1-Domain-Entity-Foundations/02-Aggregate-Root/README.md) | AggregateRoot\<TId\>, 도메인 불변식 |
| 3 | [도메인 이벤트](Part1-Domain-Entity-Foundations/03-Domain-Events/README.md) | IDomainEvent, AddDomainEvent(), ClearDomainEvents |
| 4 | [엔티티 인터페이스](Part1-Domain-Entity-Foundations/04-Entity-Interfaces/README.md) | IAuditable, ISoftDeletable |

### Part 2: Command 측 -- Repository 패턴

Aggregate Root 단위의 쓰기 작업을 위한 Repository 패턴을 구현합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 5 | [Repository 인터페이스](Part2-Command-Repository/01-Repository-Interface/README.md) | IRepository\<TAggregate, TId\>, 8개 CRUD, FinT\<IO, T\> |
| 6 | [InMemory Repository](Part2-Command-Repository/02-InMemory-Repository/README.md) | InMemoryRepositoryBase, ConcurrentDictionary |
| 7 | [EF Core Repository](Part2-Command-Repository/03-EfCore-Repository/README.md) | EfCoreRepositoryBase, ToDomain/ToModel |
| 8 | [Unit of Work](Part2-Command-Repository/04-Unit-Of-Work/README.md) | IUnitOfWork, SaveChanges, IUnitOfWorkTransaction |

### Part 3: Query 측 -- 읽기 전용 패턴

Specification 기반 검색과 DTO 프로젝션을 위한 Query 어댑터를 구현합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 9 | [IQueryPort 인터페이스](Part3-Query-Patterns/01-QueryPort-Interface/README.md) | IQueryPort\<TEntity, TDto\>, Search/SearchByCursor/Stream |
| 10 | [DTO 분리](Part3-Query-Patterns/02-DTO-Separation/README.md) | Command DTO vs Query DTO, 프로젝션 |
| 11 | [페이지네이션과 정렬](Part3-Query-Patterns/03-Pagination-And-Sorting/README.md) | PageRequest, CursorPageRequest, SortExpression |
| 12 | [InMemory Query 어댑터](Part3-Query-Patterns/04-InMemory-Query-Adapter/README.md) | InMemoryQueryBase, GetProjectedItems |
| 13 | [Dapper Query 어댑터](Part3-Query-Patterns/05-Dapper-Query-Adapter/README.md) | DapperQueryBase, SQL 생성 |

### Part 4: CQRS Usecase 통합

Command/Query Usecase와 Mediator 패턴을 통합하여 완전한 CQRS 아키텍처를 구성합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 14 | [Command Usecase](Part4-CQRS-Usecase-Integration/01-Command-Usecase/README.md) | ICommandRequest, ICommandUsecase, FinResponse |
| 15 | [Query Usecase](Part4-CQRS-Usecase-Integration/02-Query-Usecase/README.md) | IQueryRequest, IQueryUsecase, IQueryPort 연동 |
| 16 | [FinT -> FinResponse](Part4-CQRS-Usecase-Integration/03-FinT-To-FinResponse/README.md) | ToFinResponse(), LINQ 모나딕 합성 |
| 17 | [도메인 이벤트 흐름](Part4-CQRS-Usecase-Integration/04-Domain-Event-Flow/README.md) | IDomainEventCollector, Track, 발행 |
| 18 | [트랜잭션 파이프라인](Part4-CQRS-Usecase-Integration/05-Transaction-Pipeline/README.md) | 트랜잭션 파이프라인, Command 자동커밋 |

### Part 5: 도메인별 실전 예제

다양한 도메인에서 CQRS 패턴을 적용하는 실전 예제입니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 19 | [주문 관리](Part5-Domain-Examples/01-Ecommerce-Order-Management/README.md) | 주문 CQRS 완전 예제 |
| 20 | [고객 관리](Part5-Domain-Examples/02-Customer-Management/README.md) | 고객 관리 + Specification 검색 |
| 21 | [재고 관리](Part5-Domain-Examples/03-Inventory-Management/README.md) | 재고 + Soft Delete + Cursor 페이징 |
| 22 | [카탈로그 검색](Part5-Domain-Examples/04-Catalog-Search/README.md) | 3가지 페이지네이션 비교 |

### [부록](Appendix/)

- [A. CQRS vs 전통적 CRUD](Appendix/A-cqrs-vs-crud.md)
- [B. Repository vs Query 어댑터 선택 가이드](Appendix/B-repository-vs-query-adapter-guide.md)
- [C. FinT / FinResponse 타입 참조](Appendix/C-fint-finresponse-reference.md)
- [D. CQRS 안티패턴](Appendix/D-anti-patterns.md)
- [E. 용어집](Appendix/E-glossary.md)
- [F. 참고 자료](Appendix/F-references.md)

---

## 핵심 진화 과정

```
1장: Entity/Identity       ->  2장: Aggregate Root      ->  3장: 도메인 이벤트
     |
4장: 엔티티 인터페이스     ->  5장: Repository 인터페이스 ->  6장: InMemory Repository
     |
7장: EF Core Repository    ->  8장: Unit of Work
     |
9장: IQueryPort            ->  10장: DTO 분리            ->  11장: 페이지네이션/정렬
     |
12장: InMemory Query       ->  13장: Dapper Query
     |
14장: Command Usecase      ->  15장: Query Usecase       ->  16장: FinT -> FinResponse
     |
17장: 도메인 이벤트 흐름   ->  18장: 트랜잭션 파이프라인
     |
19장: 주문 관리            ->  20장: 고객 관리           ->  21장: 재고 관리
     |
22장: 카탈로그 검색
```

---

## Functorium CQRS 타입 계층

```
Command 측 (쓰기)
├── IRepository<TAggregate, TId>
│   ├── Create / GetById / Update / Delete
│   ├── CreateRange / GetByIds / UpdateRange / DeleteRange
│   └── 반환 타입: FinT<IO, T>
├── InMemoryRepositoryBase (ConcurrentDictionary 기반)
├── EfCoreRepositoryBase (EF Core 기반)
└── IUnitOfWork
    ├── SaveChanges() : FinT<IO, Unit>
    └── BeginTransactionAsync() : IUnitOfWorkTransaction

Query 측 (읽기)
├── IQueryPort<TEntity, TDto>
│   ├── Search(spec, page, sort) : FinT<IO, PagedResult<TDto>>
│   ├── SearchByCursor(spec, cursor, sort) : FinT<IO, CursorPagedResult<TDto>>
│   └── Stream(spec, sort) : IAsyncEnumerable<TDto>
├── InMemoryQueryBase
└── DapperQueryBase

Usecase 통합
├── ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>>
├── ICommandUsecase<TCommand, TSuccess> : ICommandHandler
├── IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>>
├── IQueryUsecase<TQuery, TSuccess> : IQueryHandler
└── ToFinResponse() : Fin<A> -> FinResponse<A>

Specification (검색 조건)
├── Specification<T> (추상 클래스)
│   ├── IsSatisfiedBy(T) : bool
│   ├── And() / Or() / Not() 조합
│   ├── & / | / ! 연산자
│   └── All (항등원, 동적 필터 빌더 시드)
└── ExpressionSpecification<T> (EF Core/SQL 지원)
    ├── ToExpression() → Expression<Func<T, bool>>
    └── sealed IsSatisfiedBy (컴파일 + 캐싱)
```

---

## 필수 준비물

- .NET 10.0 SDK 이상
- VS Code + C# Dev Kit 확장
- C# 기초 문법 지식
- DDD 기초 개념 (Entity, Aggregate Root)

---

## 프로젝트 구조

```
Implementing-CQRS-Repository-And-Query-Patterns/
├── Part0-Introduction/                     # Part 0: 서론
├── Part1-Domain-Entity-Foundations/         # Part 1: 도메인 엔티티 기초 (4개)
│   ├── 01-Entity-And-Identity/
│   ├── 02-Aggregate-Root/
│   ├── 03-Domain-Events/
│   └── 04-Entity-Interfaces/
├── Part2-Command-Repository/               # Part 2: Command 측 Repository (4개)
│   ├── 01-Repository-Interface/
│   ├── 02-InMemory-Repository/
│   ├── 03-EfCore-Repository/
│   └── 04-Unit-Of-Work/
├── Part3-Query-Patterns/                   # Part 3: Query 측 읽기 전용 (5개)
│   ├── 01-QueryPort-Interface/
│   ├── 02-DTO-Separation/
│   ├── 03-Pagination-And-Sorting/
│   ├── 04-InMemory-Query-Adapter/
│   └── 05-Dapper-Query-Adapter/
├── Part4-CQRS-Usecase-Integration/         # Part 4: Usecase 통합 (5개)
│   ├── 01-Command-Usecase/
│   ├── 02-Query-Usecase/
│   ├── 03-FinT-To-FinResponse/
│   ├── 04-Domain-Event-Flow/
│   └── 05-Transaction-Pipeline/
├── Part5-Domain-Examples/                  # Part 5: 도메인별 실전 예제 (4개)
│   ├── 01-Ecommerce-Order-Management/
│   ├── 02-Customer-Management/
│   ├── 03-Inventory-Management/
│   └── 04-Catalog-Search/
├── Appendix/                               # 부록
└── README.md                               # 이 문서
```

---

## 테스트

모든 Part의 예제 프로젝트에는 단위 테스트가 포함되어 있습니다. 테스트는 [15a-unit-testing.md](../../Docs/guides/15a-unit-testing.md) 가이드를 따릅니다.

### 테스트 실행 방법

```bash
# Part 1 테스트 실행
cd Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/Part1-Domain-Entity-Foundations/01-Entity-And-Identity/EntityAndIdentity.Tests.Unit
dotnet test

# Part 2 테스트 실행
cd Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/Part2-Command-Repository/01-Repository-Interface/RepositoryInterface.Tests.Unit
dotnet test

# Part 3 테스트 실행
cd Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/Part3-Query-Patterns/01-QueryPort-Interface/QueryPortInterface.Tests.Unit
dotnet test

# Part 4 테스트 실행
cd Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/Part4-CQRS-Usecase-Integration/01-Command-Usecase/CommandUsecase.Tests.Unit
dotnet test

# Part 5 테스트 실행
cd Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/Part5-Domain-Examples/01-Ecommerce-Order-Management/EcommerceOrderManagement.Tests.Unit
dotnet test
```

### 테스트 프로젝트 구조

**Part 1: 도메인 엔티티 기초** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `EntityAndIdentity.Tests.Unit` | Entity\<TId\>, IEntityId 동작 검증 |
| 2 | `AggregateRoot.Tests.Unit` | AggregateRoot 불변식 검증 |
| 3 | `DomainEvents.Tests.Unit` | 도메인 이벤트 추가/삭제 검증 |
| 4 | `EntityInterfaces.Tests.Unit` | IAuditable, ISoftDeletable 검증 |

**Part 2: Command 측 -- Repository 패턴** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 5 | `RepositoryInterface.Tests.Unit` | IRepository 8개 CRUD 검증 |
| 6 | `InMemoryRepository.Tests.Unit` | InMemory 구현체 검증 |
| 7 | `EfCoreRepository.Tests.Unit` | EF Core 구현체 검증 |
| 8 | `UnitOfWork.Tests.Unit` | SaveChanges, 트랜잭션 검증 |

**Part 3: Query 측 -- 읽기 전용 패턴** (5개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 9 | `QueryPortInterface.Tests.Unit` | IQueryPort Search/Stream 검증 |
| 10 | `DtoSeparation.Tests.Unit` | Command/Query DTO 분리 검증 |
| 11 | `PaginationAndSorting.Tests.Unit` | 페이지네이션, 정렬 검증 |
| 12 | `InMemoryQueryAdapter.Tests.Unit` | InMemory Query 어댑터 검증 |
| 13 | `DapperQueryAdapter.Tests.Unit` | Dapper SQL 생성 검증 |

**Part 4: CQRS Usecase 통합** (5개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 14 | `CommandUsecase.Tests.Unit` | Command 핸들러 검증 |
| 15 | `QueryUsecase.Tests.Unit` | Query 핸들러 검증 |
| 16 | `FinTToFinResponse.Tests.Unit` | ToFinResponse 변환 검증 |
| 17 | `DomainEventFlow.Tests.Unit` | 이벤트 수집/발행 검증 |
| 18 | `TransactionPipeline.Tests.Unit` | 트랜잭션 파이프라인 검증 |

**Part 5: 도메인별 실전 예제** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 19 | `EcommerceOrderManagement.Tests.Unit` | 주문 CQRS 검증 |
| 20 | `CustomerManagement.Tests.Unit` | 고객 관리 검증 |
| 21 | `InventoryManagement.Tests.Unit` | 재고 관리 검증 |
| 22 | `CatalogSearch.Tests.Unit` | 페이지네이션 비교 검증 |

### 테스트 명명 규칙

T1_T2_T3 명명 규칙을 따릅니다:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Create_ReturnsAggregate_WhenValid()
{
    // Arrange
    var order = Order.Create(OrderId.New(), customerId);
    // Act
    var actual = await repository.Create(order).RunAsync();
    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

---

## 소스 코드

이 튜토리얼의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- Repository 인터페이스: `Src/Functorium/Domains/Repositories/`
- Repository 구현체: `Src/Functorium/Adapters/Repositories/`
- Query 어댑터: `Src/Functorium/Applications/Queries/`
- Usecase 인터페이스: `Src/Functorium/Applications/Usecases/`
- 트랜잭션 파이프라인: `Src/Functorium/Adapters/Observabilities/Pipelines/`
- 튜토리얼 프로젝트: `Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/`

### 관련 튜토리얼

이 튜토리얼은 다음 튜토리얼과 함께 학습하면 더 효과적입니다:

- **[Specification 패턴으로 도메인 규칙 구현하기](../Implementing-Specification-Pattern/README.md)**: Specification 패턴 기초부터 Repository 통합까지. 이 튜토리얼의 IQueryPort, IRepository에서 Specification을 매개변수로 사용합니다.

---

이 튜토리얼은 Functorium 프로젝트의 실제 CQRS 프레임워크 개발 경험을 바탕으로 작성되었습니다.
