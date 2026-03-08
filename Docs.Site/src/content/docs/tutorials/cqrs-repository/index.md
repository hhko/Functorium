---
title: "CQRS 리포지토리 패턴"
---

**C# Functorium으로 Repository와 Query 어댑터를 구현하는 실전 가이드**

---

## 이 튜토리얼에 대하여

주문 목록 API에 새 필터가 추가될 때마다 `GetByCustomer`, `GetRecent`, `SearchByKeyword`... Repository 메서드가 끝없이 늘어나고 있나요? 읽기용 프로퍼티가 도메인 모델에 스며들어 쓰기 로직을 오염시키고, 하나를 고치면 다른 쪽이 깨지는 악순환이 반복됩니다.

이 튜토리얼은 그 문제를 **Command와 Query의 책임 분리(CQRS)로** 해결합니다. 도메인 엔티티 기초에서 시작하여 Repository 패턴, Query 어댑터, Usecase 통합까지, **22개의 실습 프로젝트**를 통해 CQRS 패턴의 모든 측면을 단계별로 학습합니다.

### 대상 독자

여러분의 경험 수준에 따라 학습 범위를 선택할 수 있습니다.

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 기본 문법을 알고 CQRS 패턴에 입문하려는 개발자 | Part 1 |
| **중급** | 패턴을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 |
| **고급** | 아키텍처 설계와 도메인 모델링에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. CQRS 패턴의 개념과 필요성을 이해하고 Command/Query 분리 설계를 적용할 수 있습니다
2. IRepository 기반 Repository 패턴으로 Aggregate Root 단위 영속화를 구현할 수 있습니다
3. IQueryPort 기반 Query 어댑터로 읽기 전용 최적화 조회를 구현할 수 있습니다
4. FinT 모나드 합성과 ToFinResponse 변환으로 함수형 파이프라인을 구성할 수 있습니다
5. 트랜잭션 파이프라인과 도메인 이벤트 흐름으로 완성도 높은 CQRS 아키텍처를 구축할 수 있습니다

---

### Part 0: 서론

CQRS가 왜 필요한지, 어떤 문제를 해결하는지부터 시작합니다. 환경 설정을 마치고 CQRS 아키텍처의 전체 그림을 파악합니다.

- [0.1 왜 CQRS인가](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 환경 설정](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 CQRS 패턴 개요](Part0-Introduction/03-cqrs-pattern-overview.md)

### Part 1: 도메인 엔티티 기초

같은 이름의 상품 두 개는 같은 상품일까요? Entity의 정체성(Identity)부터 시작하여 Aggregate Root, 도메인 이벤트, 엔티티 인터페이스까지 CQRS의 기반이 되는 도메인 모델링을 구축합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Entity와 Identity](Part1-Domain-Entity-Foundations/01-Entity-And-Identity/) | Entity\<TId\>, IEntityId, Ulid 기반 ID |
| 2 | [Aggregate Root](Part1-Domain-Entity-Foundations/02-Aggregate-Root/) | AggregateRoot\<TId\>, 도메인 불변식 |
| 3 | [도메인 이벤트](Part1-Domain-Entity-Foundations/03-Domain-Events/) | IDomainEvent, AddDomainEvent(), ClearDomainEvents |
| 4 | [엔티티 인터페이스](Part1-Domain-Entity-Foundations/04-Entity-Interfaces/) | IAuditable, ISoftDeletable |

### Part 2: Command 측 -- Repository 패턴

도메인 모델의 불변식을 보장하면서 영속화하려면 어떤 인터페이스가 필요할까요? Aggregate Root 단위의 쓰기 작업을 위한 IRepository 설계부터 InMemory, EF Core 구현, Unit of Work까지 진행합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Repository 인터페이스](Part2-Command-Repository/01-Repository-Interface/) | IRepository\<TAggregate, TId\>, 8개 CRUD, FinT\<IO, T\> |
| 2 | [InMemory Repository](Part2-Command-Repository/02-InMemory-Repository/) | InMemoryRepositoryBase, ConcurrentDictionary |
| 3 | [EF Core Repository](Part2-Command-Repository/03-EfCore-Repository/) | EfCoreRepositoryBase, ToDomain/ToModel |
| 4 | [Unit of Work](Part2-Command-Repository/04-Unit-Of-Work/) | IUnitOfWork, SaveChanges, IUnitOfWorkTransaction |

### Part 3: Query 측 -- 읽기 전용 패턴

조회 조건이 늘어날 때마다 메서드를 추가하는 대신, Specification 하나로 동적 검색을 처리합니다. DTO 프로젝션과 3가지 페이지네이션을 통해 읽기 전용 경로를 최적화합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [IQueryPort 인터페이스](Part3-Query-Patterns/01-QueryPort-Interface/) | IQueryPort\<TEntity, TDto\>, Search/SearchByCursor/Stream |
| 2 | [DTO 분리](Part3-Query-Patterns/02-DTO-Separation/) | Command DTO vs Query DTO, 프로젝션 |
| 3 | [페이지네이션과 정렬](Part3-Query-Patterns/03-Pagination-And-Sorting/) | PageRequest, CursorPageRequest, SortExpression |
| 4 | [InMemory Query 어댑터](Part3-Query-Patterns/04-InMemory-Query-Adapter/) | InMemoryQueryBase, GetProjectedItems |
| 5 | [Dapper Query 어댑터](Part3-Query-Patterns/05-Dapper-Query-Adapter/) | DapperQueryBase, SQL 생성 |

### Part 4: CQRS Usecase 통합

Repository와 Query 어댑터가 준비되었으니, 이제 이들을 Usecase로 통합합니다. Mediator 패턴으로 Command/Query를 디스패치하고, FinT에서 FinResponse로의 변환, 도메인 이벤트 흐름, 트랜잭션 파이프라인까지 CQRS 아키텍처를 완성합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Command Usecase](Part4-CQRS-Usecase-Integration/01-Command-Usecase/) | ICommandRequest, ICommandUsecase, FinResponse |
| 2 | [Query Usecase](Part4-CQRS-Usecase-Integration/02-Query-Usecase/) | IQueryRequest, IQueryUsecase, IQueryPort 연동 |
| 3 | [FinT -> FinResponse](Part4-CQRS-Usecase-Integration/03-FinT-To-FinResponse/) | ToFinResponse(), LINQ 모나딕 합성 |
| 4 | [도메인 이벤트 흐름](Part4-CQRS-Usecase-Integration/04-Domain-Event-Flow/) | IDomainEventCollector, Track, 발행 |
| 5 | [트랜잭션 파이프라인](Part4-CQRS-Usecase-Integration/05-Transaction-Pipeline/) | 트랜잭션 파이프라인, Command 자동커밋 |

### Part 5: 도메인별 실전 예제

지금까지 배운 CQRS 패턴을 실제 도메인에 적용합니다. 주문, 고객, 재고, 카탈로그 각 도메인에서 Command/Query 분리가 어떤 이점을 가져오는지 직접 확인합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [주문 관리](Part5-Domain-Examples/01-Ecommerce-Order-Management/) | 주문 CQRS 완전 예제 |
| 2 | [고객 관리](Part5-Domain-Examples/02-Customer-Management/) | 고객 관리 + Specification 검색 |
| 3 | [재고 관리](Part5-Domain-Examples/03-Inventory-Management/) | 재고 + Soft Delete + Cursor 페이징 |
| 4 | [카탈로그 검색](Part5-Domain-Examples/04-Catalog-Search/) | 3가지 페이지네이션 비교 |

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
Part 1: 도메인 엔티티 기초
  1장: Entity/Identity     ->  2장: Aggregate Root    ->  3장: 도메인 이벤트  ->  4장: 엔티티 인터페이스
     |
Part 2: Command 측 Repository
  1장: Repository 인터페이스 ->  2장: InMemory Repository ->  3장: EF Core Repository ->  4장: Unit of Work
     |
Part 3: Query 측 읽기 전용
  1장: IQueryPort          ->  2장: DTO 분리          ->  3장: 페이지네이션/정렬
     |
  4장: InMemory Query      ->  5장: Dapper Query
     |
Part 4: CQRS Usecase 통합
  1장: Command Usecase     ->  2장: Query Usecase     ->  3장: FinT -> FinResponse
     |
  4장: 도메인 이벤트 흐름  ->  5장: 트랜잭션 파이프라인
     |
Part 5: 도메인별 실전 예제
  1장: 주문 관리           ->  2장: 고객 관리         ->  3장: 재고 관리  ->  4장: 카탈로그 검색
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
| 1 | `RepositoryInterface.Tests.Unit` | IRepository 8개 CRUD 검증 |
| 2 | `InMemoryRepository.Tests.Unit` | InMemory 구현체 검증 |
| 3 | `EfCoreRepository.Tests.Unit` | EF Core 구현체 검증 |
| 4 | `UnitOfWork.Tests.Unit` | SaveChanges, 트랜잭션 검증 |

**Part 3: Query 측 -- 읽기 전용 패턴** (5개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `QueryPortInterface.Tests.Unit` | IQueryPort Search/Stream 검증 |
| 2 | `DtoSeparation.Tests.Unit` | Command/Query DTO 분리 검증 |
| 3 | `PaginationAndSorting.Tests.Unit` | 페이지네이션, 정렬 검증 |
| 4 | `InMemoryQueryAdapter.Tests.Unit` | InMemory Query 어댑터 검증 |
| 5 | `DapperQueryAdapter.Tests.Unit` | Dapper SQL 생성 검증 |

**Part 4: CQRS Usecase 통합** (5개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `CommandUsecase.Tests.Unit` | Command 핸들러 검증 |
| 2 | `QueryUsecase.Tests.Unit` | Query 핸들러 검증 |
| 3 | `FinTToFinResponse.Tests.Unit` | ToFinResponse 변환 검증 |
| 4 | `DomainEventFlow.Tests.Unit` | 이벤트 수집/발행 검증 |
| 5 | `TransactionPipeline.Tests.Unit` | 트랜잭션 파이프라인 검증 |

**Part 5: 도메인별 실전 예제** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `EcommerceOrderManagement.Tests.Unit` | 주문 CQRS 검증 |
| 2 | `CustomerManagement.Tests.Unit` | 고객 관리 검증 |
| 3 | `InventoryManagement.Tests.Unit` | 재고 관리 검증 |
| 4 | `CatalogSearch.Tests.Unit` | 페이지네이션 비교 검증 |

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

- **[Specification 패턴으로 도메인 규칙 구현하기](../Implementing-Specification-Pattern/)**: Specification 패턴 기초부터 Repository 통합까지. 이 튜토리얼의 IQueryPort, IRepository에서 Specification을 매개변수로 사용합니다.

---

이 튜토리얼은 Functorium 프로젝트의 실제 CQRS 프레임워크 개발 경험을 바탕으로 작성되었습니다.
