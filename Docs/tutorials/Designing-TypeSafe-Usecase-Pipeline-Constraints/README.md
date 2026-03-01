# 타입 안전한 Usecase 파이프라인 제약 설계하기

**C# 제네릭 변성에서 Mediator Pipeline 제약 해결까지 실전 가이드**

---

## 이 책에 대하여

이 책은 **C# 제네릭 변성(공변성/반공변성)의 기초**에서 시작하여, **Mediator Pipeline에서 리플렉션 없이 타입 안전한 응답 처리**를 구현하기까지의 설계 과정을 단계별로 학습할 수 있도록 구성된 실전 가이드입니다. **20개의 실습 프로젝트**를 통해 변성 기초 → Fin\<T\> 한계 → IFinResponse 계층 → Pipeline 제약 → 실전 Usecase까지 체계적으로 학습할 수 있습니다.

> **`Fin<T>`는 sealed struct라 제약 조건으로 사용할 수 없다 — 이 한 줄의 제약에서 시작된 IFinResponse 인터페이스 계층 설계의 모든 과정을 함께 경험해보세요.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 초급 | C# 기본 문법을 알고 제네릭 변성에 입문하려는 개발자 | Part 1 (1장~4장) |
| 중급 | Mediator Pipeline과 타입 제약 설계에 관심 있는 개발자 | Part 1~3 (1장~13장) |
| 고급 | Pipeline 아키텍처와 함수형 패턴을 실전에 적용하려는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 책을 완료하면 다음을 할 수 있습니다:

1. **C# 제네릭 변성**(공변성, 반공변성, 불변성)의 원리와 적용 조건을 이해
2. **sealed struct의 제약 한계**와 인터페이스 계층으로 우회하는 설계 패턴 습득
3. **IFinResponse 인터페이스 계층**을 직접 설계하고 CRTP 팩토리 패턴 구현
4. **Pipeline별 최소 제약 조건**을 설계하여 리플렉션 없는 타입 안전한 Pipeline 구축
5. **Command/Query Usecase**에서 FinResponse 기반 전체 Pipeline 흐름 통합

---

## 목차

### Part 0: 서론

타입 안전한 파이프라인이 왜 필요한지, 전체 아키텍처의 개요를 소개합니다.

- [0.1 왜 타입 안전한 파이프라인인가](Part0-Introduction/01-why-this-book.md)
- [0.2 환경 설정](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 Usecase Pipeline 아키텍처 개요](Part0-Introduction/03-usecase-pipeline-overview.md)

### Part 1: 제네릭 변성 기초

C# 제네릭 변성의 핵심 개념을 코드로 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [공변성 (out)](Part1-Generic-Variance-Foundations/01-Covariance/README.md) | IEnumerable\<out T\>, 출력 위치, Dog→Animal 대입 |
| 2 | [반공변성 (in)](Part1-Generic-Variance-Foundations/02-Contravariance/README.md) | Action\<in T\>, IHandler\<in T\>, 핸들러 대체 |
| 3 | [불변성과 제약](Part1-Generic-Variance-Foundations/03-Invariance-And-Constraints/README.md) | List\<T\> 불변, sealed struct 제약 불가, where 제약 |
| 4 | [인터페이스 분리와 변성 조합](Part1-Generic-Variance-Foundations/04-Interface-Segregation-And-Variance/README.md) | 읽기(out)/쓰기(in)/팩토리 분리, ISP+변성 |

### Part 2: 문제 정의 -- Fin과 Mediator 충돌

`Fin<T>` sealed struct와 Mediator Pipeline의 제약 충돌 문제를 분석합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 5 | [Mediator Pipeline Behavior 구조](Part2-Problem-Definition/01-Mediator-Pipeline-Structure/README.md) | IPipelineBehavior, MessageHandlerDelegate, 제약 역할 |
| 6 | [Fin\<T\> 직접 사용의 한계](Part2-Problem-Definition/02-Fin-Direct-Limitation/README.md) | sealed struct 제약 불가, 리플렉션 3곳 필요 |
| 7 | [IFinResponse 래퍼의 한계](Part2-Problem-Definition/03-IFinResponse-Wrapper-Limitation/README.md) | 이중 인터페이스, 리플렉션 1곳, CreateFail 불가 |
| 8 | [요구사항 정리](Part2-Problem-Definition/04-pipeline-requirements-summary.md) | 4가지 요구사항, Pipeline별 필요 능력 매트릭스 |

### Part 3: 해결 -- IFinResponse 계층 설계

리플렉션 없이 타입 안전한 Pipeline을 가능하게 하는 IFinResponse 인터페이스 계층을 직접 설계합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 9 | [IFinResponse 비제네릭 마커](Part3-IFinResponse-Hierarchy/01-IFinResponse-Marker/README.md) | IsSucc/IsFail, Pipeline 읽기 전용 접근 |
| 10 | [IFinResponse\<out A\> 공변 인터페이스](Part3-IFinResponse-Hierarchy/02-IFinResponse-Covariant/README.md) | out 적용, 공변적 Pipeline 접근 |
| 11 | [IFinResponseFactory CRTP 팩토리](Part3-IFinResponse-Hierarchy/03-IFinResponseFactory-CRTP/README.md) | static abstract, CRTP, CreateFail |
| 12 | [IFinResponseWithError 에러 접근](Part3-IFinResponse-Hierarchy/04-IFinResponseWithError/README.md) | Error 속성, Fail에만 구현, 패턴 매칭 |
| 13 | [FinResponse\<A\> Discriminated Union](Part3-IFinResponse-Hierarchy/05-FinResponse-Discriminated-Union/README.md) | Succ/Fail sealed records, Match/Map/Bind, 암시적 변환 |

### Part 4: Pipeline 제약 패턴 적용

IFinResponse 계층을 활용하여 각 Pipeline에 최소 제약 조건을 적용합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 14 | [Create-Only 제약](Part4-Pipeline-Constraint-Patterns/01-Create-Only-Constraint/README.md) | `where TResponse : IFinResponseFactory<TResponse>` |
| 15 | [Read+Create 제약](Part4-Pipeline-Constraint-Patterns/02-Read-Create-Constraint/README.md) | `where TResponse : IFinResponse, IFinResponseFactory<TResponse>` |
| 16 | [Transaction/Caching Pipeline](Part4-Pipeline-Constraint-Patterns/03-Transaction-Caching-Pipeline/README.md) | Command/Query 분기, ICacheable 조건부 |
| 17 | [Fin → FinResponse 브릿지](Part4-Pipeline-Constraint-Patterns/04-Fin-To-FinResponse-Bridge/README.md) | ToFinResponse() 확장 메서드, 계층 간 변환 |

### Part 5: 실전 Usecase 예제

전체 Pipeline을 통합한 Command/Query Usecase 완전 예제입니다.

- [5.1 Command Usecase 완전 예제](Part5-Practical-Usecase-Examples/01-Command-Usecase-Example/README.md)
- [5.2 Query Usecase 완전 예제](Part5-Practical-Usecase-Examples/02-Query-Usecase-Example/README.md)
- [5.3 Pipeline 전체 흐름 통합](Part5-Practical-Usecase-Examples/03-Full-Pipeline-Integration/README.md)

### [부록](Appendix/)

- [A. IFinResponse 인터페이스 계층 전체 참조](Appendix/A-interface-hierarchy-reference.md)
- [B. Pipeline 제약 조건 vs 대안 비교](Appendix/B-constraint-vs-alternatives.md)
- [C. Railway Oriented Programming 참조](Appendix/C-railway-oriented-programming.md)
- [D. 용어집](Appendix/D-glossary.md)
- [E. 참고 자료](Appendix/E-references.md)

---

## 핵심 진화 과정

```
1장: 공변성(out)            ->  2장: 반공변성(in)           ->  3장: 불변성과 제약
     |
4장: ISP + 변성 조합        ->  5장: Mediator Pipeline    ->  6장: Fin<T> 직접 사용 한계
     |
7장: IFinResponse 래퍼 한계 ->  8장: 요구사항 정리
     |
9장: IFinResponse 마커      ->  10장: IFinResponse<out A> ->  11장: IFinResponseFactory
     |
12장: IFinResponseWithError ->  13장: FinResponse<A> DU
     |
14장: Create-Only 제약      ->  15장: Read+Create 제약    ->  16장: Transaction/Caching
     |
17장: Fin→FinResponse       ->  18장: Command Usecase     ->  19장: Query Usecase
     |
20장: 전체 통합
```

---

## IFinResponse 타입 계층

```
IFinResponse                              비제네릭 마커 (IsSucc/IsFail)
├── IFinResponse<out A>                   공변 인터페이스 (읽기 전용)
│
IFinResponseFactory<TSelf>                CRTP 팩토리 (CreateFail)
│
IFinResponseWithError                     에러 접근 (Error 속성)
│
FinResponse<A>                            Discriminated Union
├── : IFinResponse<A>                     공변 인터페이스 구현
├── : IFinResponseFactory<FinResponse<A>> CRTP 팩토리 구현
│
├── sealed record Succ(A Value)           성공 케이스
│
└── sealed record Fail(Error Error)       실패 케이스
    └── : IFinResponseWithError           Fail에서만 에러 접근
```

### Pipeline별 제약 조건

```
Pipeline                    TResponse 제약 조건                      능력
──────────────────────────  ─────────────────────────────────────    ────────────
Validation Pipeline         IFinResponseFactory<TResponse>           CreateFail
Exception Pipeline          IFinResponseFactory<TResponse>           CreateFail
Logging Pipeline            IFinResponse, IFinResponseFactory<...>   Read + Create
Tracing Pipeline            IFinResponse, IFinResponseFactory<...>   Read + Create
Metrics Pipeline            IFinResponse, IFinResponseFactory<...>   Read + Create
Transaction Pipeline        IFinResponse, IFinResponseFactory<...>   Read + Create
Caching Pipeline            IFinResponse, IFinResponseFactory<...>   Read + Create
```

---

## 필수 준비물

- .NET 10.0 SDK 이상
- VS Code + C# Dev Kit 확장
- C# 기초 문법 지식
- 제네릭 기초 개념 (타입 파라미터, where 제약)

---

## 프로젝트 구조

```
Designing-TypeSafe-Usecase-Pipeline-Constraints/
├── Part0-Introduction/                        # Part 0: 서론 (3개)
├── Part1-Generic-Variance-Foundations/         # Part 1: 제네릭 변성 기초 (4개)
│   ├── 01-Covariance/
│   ├── 02-Contravariance/
│   ├── 03-Invariance-And-Constraints/
│   └── 04-Interface-Segregation-And-Variance/
├── Part2-Problem-Definition/                  # Part 2: 문제 정의 (4개)
│   ├── 01-Mediator-Pipeline-Structure/
│   ├── 02-Fin-Direct-Limitation/
│   ├── 03-IFinResponse-Wrapper-Limitation/
│   └── 04-pipeline-requirements-summary.md
├── Part3-IFinResponse-Hierarchy/              # Part 3: IFinResponse 계층 (5개)
│   ├── 01-IFinResponse-Marker/
│   ├── 02-IFinResponse-Covariant/
│   ├── 03-IFinResponseFactory-CRTP/
│   ├── 04-IFinResponseWithError/
│   └── 05-FinResponse-Discriminated-Union/
├── Part4-Pipeline-Constraint-Patterns/        # Part 4: Pipeline 제약 (4개)
│   ├── 01-Create-Only-Constraint/
│   ├── 02-Read-Create-Constraint/
│   ├── 03-Transaction-Caching-Pipeline/
│   └── 04-Fin-To-FinResponse-Bridge/
├── Part5-Practical-Usecase-Examples/          # Part 5: 실전 예제 (3개)
│   ├── 01-Command-Usecase-Example/
│   ├── 02-Query-Usecase-Example/
│   └── 03-Full-Pipeline-Integration/
├── Appendix/                                  # 부록
└── README.md                                  # 이 문서
```

---

## 테스트

모든 Part의 예제 프로젝트에는 단위 테스트가 포함되어 있습니다. 테스트는 [15a-unit-testing.md](../../Docs/guides/15a-unit-testing.md) 가이드를 따릅니다.

### 테스트 실행 방법

```bash
# Part 1 테스트 실행
cd Docs/tutorials/Designing-TypeSafe-Usecase-Pipeline-Constraints/Part1-Generic-Variance-Foundations/01-Covariance/Covariance.Tests.Unit
dotnet test

# Part 3 테스트 실행
cd Docs/tutorials/Designing-TypeSafe-Usecase-Pipeline-Constraints/Part3-IFinResponse-Hierarchy/01-IFinResponse-Marker/FinResponseMarker.Tests.Unit
dotnet test

# Part 4 테스트 실행
cd Docs/tutorials/Designing-TypeSafe-Usecase-Pipeline-Constraints/Part4-Pipeline-Constraint-Patterns/01-Create-Only-Constraint/CreateOnlyConstraint.Tests.Unit
dotnet test
```

### 테스트 명명 규칙

T1_T2_T3 명명 규칙을 따릅니다:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Assign_Succeeds_WhenCovarianceApplies()
{
    // Arrange
    IEnumerable<Animal> animals;

    // Act
    animals = new List<Dog>();

    // Assert
    animals.ShouldNotBeNull();
}
```

---

## 소스 코드

이 책의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- IFinResponse 인터페이스: `Src/Functorium/Applications/Usecases/IFinResponse.cs`
- FinResponse 구현체: `Src/Functorium/Applications/Usecases/IFinResponse.Impl.cs`
- 정적 팩토리: `Src/Functorium/Applications/Usecases/IFinResponse.Factory.cs`
- Fin→FinResponse 변환: `Src/Functorium/Applications/Usecases/IFinResponse.FinConversions.cs`
- Command/Query 인터페이스: `Src/Functorium/Applications/Usecases/ICommandRequest.cs`, `IQueryRequest.cs`
- Pipeline 구현: `Src/Functorium/Adapters/Observabilities/Pipelines/`

### 관련 도서

이 책은 다음 도서와 함께 학습하면 더 효과적입니다:

- **[CQRS 패턴으로 Command와 Query 분리하기](../Implementing-CQRS-Repository-And-Query-Patterns/README.md)**: CQRS 패턴 기초부터 Usecase 통합까지. 이 책의 Pipeline 제약이 적용되는 CQRS 아키텍처를 학습합니다.

---

이 책은 Functorium 프로젝트의 IFinResponse 인터페이스 계층 설계 경험을 바탕으로 작성되었습니다.
