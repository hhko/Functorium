---
title: "참고 자료"
---
## 공식 문서

### Functorium
- **GitHub**: https://github.com/your-org/functorium
- **Repository 타입**: `Src/Functorium/Domains/Repositories/`
- **Query 어댑터**: `Src/Functorium/Applications/Queries/`
- **Usecase 인터페이스**: `Src/Functorium/Applications/Usecases/`

### .NET
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **Dapper**: https://github.com/DapperLib/Dapper

### LanguageExt
- **GitHub**: https://github.com/louthy/language-ext
- **FinT 문서**: https://github.com/louthy/language-ext/wiki

---

## 참고 자료

### Domain-Driven Design
- **제목**: Domain-Driven Design: Tackling Complexity in the Heart of Software
- **저자**: Eric Evans
- **출판**: Addison-Wesley, 2003
- **핵심 내용**: Entity, Aggregate Root, Repository, Specification 패턴의 원론적 정의

### Implementing Domain-Driven Design
- **제목**: Implementing Domain-Driven Design
- **저자**: Vaughn Vernon
- **출판**: Addison-Wesley, 2013
- **핵심 내용**: DDD 실전 구현, CQRS와 Event Sourcing

### Patterns of Enterprise Application Architecture
- **제목**: Patterns of Enterprise Application Architecture
- **저자**: Martin Fowler
- **출판**: Addison-Wesley, 2002
- **핵심 내용**: Repository 패턴, Unit of Work 패턴, Query Object 패턴

### Functional Programming in C#
- **제목**: Functional Programming in C#: How to write better C# code
- **저자**: Enrico Buonanno
- **출판**: Manning, 2022 (2nd Edition)
- **핵심 내용**: 함수형 프로그래밍, 모나드, 에러 처리

---

## 온라인 리소스

### CQRS

**Greg Young의 CQRS Documents**
- https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf
- CQRS 패턴의 원본 문서. Command와 Query 분리의 이론적 기초.

**Martin Fowler의 CQRS**
- https://martinfowler.com/bliki/CQRS.html
- CQRS 패턴의 간결한 설명과 적용 가이드.

**Microsoft CQRS Pattern**
- https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Microsoft의 CQRS 패턴 설명과 Azure에서의 적용.

### DDD & Repository 패턴

**Martin Fowler의 Repository Pattern**
- https://martinfowler.com/eaaCatalog/repository.html
- Repository 패턴의 정의와 설명.

**Martin Fowler의 Unit of Work**
- https://martinfowler.com/eaaCatalog/unitOfWork.html
- Unit of Work 패턴의 정의.

### Specification 패턴

**Eric Evans & Martin Fowler의 Specification Pattern**
- https://www.martinfowler.com/apsupp/spec.pdf
- Specification 패턴의 원본 논문. IQueryPort에서 검색 조건으로 사용.

---

## 관련 라이브러리

### CQRS & Mediator

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| Functorium | 본 튜토리얼의 CQRS 프레임워크 | - |
| Mediator | Source Generator 기반 Mediator 패턴 | ✅ |

### ORM & 데이터 접근

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| Microsoft.EntityFrameworkCore | EF Core ORM (Command 측) | ✅ |
| Dapper | 경량 ORM (Query 측) | ✅ |

### 함수형 프로그래밍

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| LanguageExt.Core | 함수형 프로그래밍 타입 (Fin, FinT, IO) | ✅ |

### 테스트

| 라이브러리 | 설명 | NuGet |
|-----------|------|-------|
| xUnit | 테스트 프레임워크 | ✅ |
| Shouldly | 단언문 라이브러리 | ✅ |
| NSubstitute | 목 라이브러리 | ✅ |

---

## Functorium 프로젝트 참조

### 소스 코드 위치

```
Src/Functorium/
├── Domains/
│   ├── Entities/                  # Entity<TId>, AggregateRoot<TId>, IEntityId
│   ├── Repositories/              # IRepository<TAggregate, TId>
│   └── Specifications/            # Specification<T>
├── Applications/
│   ├── Queries/                   # IQueryPort<TEntity, TDto>
│   ├── Usecases/                  # ICommandRequest, IQueryRequest, FinResponse
│   └── Persistence/               # IUnitOfWork, IUnitOfWorkTransaction
└── Adapters/
    ├── Repositories/              # InMemoryRepositoryBase, EfCoreRepositoryBase
    ├── Events/                    # DomainEventCollector
    └── Observabilities/Pipelines/ # UsecaseTransactionPipeline
```

### 튜토리얼 프로젝트

```
Docs.Site/src/content/docs/tutorials/cqrs-repository/
├── Part1-Domain-Entity-Foundations/   # 도메인 엔티티 (4개)
├── Part2-Command-Repository/          # Repository 패턴 (4개)
├── Part3-Query-Patterns/              # Query 패턴 (5개)
├── Part4-CQRS-Usecase-Integration/    # Usecase 통합 (5개)
└── Part5-Domain-Examples/             # 실전 예제 (4개)
```

---

## 관련 튜토리얼

이 튜토리얼은 다음 튜토리얼과 함께 학습하면 더 효과적입니다:

- **[Specification 패턴으로 도메인 규칙 구현하기](../specification-pattern/)**: Specification 패턴 기초부터 Repository 통합까지. 이 튜토리얼의 IQueryPort, IRepository에서 Specification을 매개변수로 사용합니다.

---

## 추가 학습 권장 순서

### 초급자 (패턴 입문)
1. Eric Evans DDD 책의 Repository 챕터
2. 본 튜토리얼 Part 1
3. Functorium Repository/Entity 소스 코드 읽기

### 중급자 (실전 적용)
1. Martin Fowler의 CQRS 문서
2. 본 튜토리얼 Part 2~3
3. EF Core / Dapper 공식 문서

### 고급자 (아키텍처 설계)
1. Greg Young의 CQRS Documents
2. 본 튜토리얼 Part 4~5
3. LanguageExt 함수형 타입 심화 학습

---

이 튜토리얼은 Functorium 프로젝트의 실제 CQRS 프레임워크 개발 경험을 바탕으로 작성되었습니다.
