---
title: "참고 자료"
---

## 개요

이 튜토리얼의 학습에 도움이 되는 참고 자료를 정리합니다.

---

## C# 언어 및 .NET 문서

### 제네릭 변성

- [Covariance and Contravariance in Generics - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/generics/covariance-and-contravariance)
- [Covariance and Contravariance (C# Programming Guide)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/covariance-contravariance/)
- [out (generic modifier) - C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/out-generic-modifier)
- [in (generic modifier) - C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-generic-modifier)

### static abstract 멤버

- [Static abstract members in interfaces - C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#generic-math-support)
- [Tutorial: Explore static virtual members in interfaces](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members)

### Record 타입

- [Records - C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Use record types - C# Tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/records)

---

## 라이브러리

### LanguageExt

C#을 위한 함수형 프로그래밍 라이브러리. `Fin<T>`, `Option<T>`, `Either<L, R>` 등의 모나딕 타입을 제공합니다.

- [GitHub - louthy/language-ext](https://github.com/louthy/language-ext)
- [LanguageExt Documentation](https://louthy.github.io/language-ext/)

### Mediator

고성능 .NET Mediator 패턴 라이브러리. Source Generator 기반으로 리플렉션 없이 요청을 라우팅합니다.

- [GitHub - martinothamar/Mediator](https://github.com/martinothamar/Mediator)

---

## 함수형 프로그래밍

### Railway Oriented Programming

- [Railway Oriented Programming - Scott Wlaschin](https://fsharpforfunandprofit.com/rop/)
- [Against Railway-Oriented Programming - Scott Wlaschin](https://fsharpforfunandprofit.com/posts/against-railway-oriented-programming/)

### 함수형 C#

- [Functional Programming in C# - Enrico Buonanno (Manning)](https://www.manning.com/books/functional-programming-in-c-sharp-second-edition)

---

## 설계 패턴

### CQRS

- [CQRS Pattern - Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Martin Fowler - CQRS](https://martinfowler.com/bliki/CQRS.html)

### Mediator Pattern

- [Mediator Pattern - Refactoring Guru](https://refactoring.guru/design-patterns/mediator)
- [Pipeline Pattern / Chain of Responsibility](https://refactoring.guru/design-patterns/chain-of-responsibility)

### CRTP (Curiously Recurring Template Pattern)

- [CRTP in C# - Wikipedia](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern)

---

## Functorium 소스 파일

이 튜토리얼에서 다루는 Functorium의 핵심 소스 파일 목록입니다.

### IFinResponse 인터페이스 계층

| 파일 | 설명 |
|------|------|
| `Src/Functorium/Applications/Usecases/IFinResponse.cs` | 인터페이스 정의 (IFinResponse, IFinResponseFactory 등) |
| `Src/Functorium/Applications/Usecases/IFinResponse.Impl.cs` | FinResponse\<A\> 레코드 (Succ/Fail, Match/Map/Bind) |
| `Src/Functorium/Applications/Usecases/IFinResponse.Factory.cs` | FinResponse 정적 팩토리 클래스 |
| `Src/Functorium/Applications/Usecases/IFinResponse.FinConversions.cs` | Fin\<A\> → FinResponse\<A\> 변환 확장 메서드 |

### Command/Query 인터페이스

| 파일 | 설명 |
|------|------|
| `Src/Functorium/Applications/Usecases/ICommandRequest.cs` | ICommandRequest\<TSuccess\>, ICommandUsecase |
| `Src/Functorium/Applications/Usecases/IQueryRequest.cs` | IQueryRequest\<TSuccess\>, IQueryUsecase |
| `Src/Functorium/Applications/Usecases/ICacheable.cs` | ICacheable 인터페이스 |

### Pipeline 구현

모든 파일은 `Src/Functorium.Adapters/Observabilities/Pipelines/` 디렉토리에 위치합니다.

| 파일 | 설명 |
|------|------|
| `UsecasePipelineBase.cs` | 공통 헬퍼 베이스 클래스 (CQRS 타입 식별, 핸들러명 추출) |
| `UsecaseMetricsPipeline.cs` | Metrics Pipeline (Read + Create) |
| `UsecaseTracingPipeline.cs` | Tracing Pipeline (Read + Create) |
| `UsecaseLoggingPipeline.cs` | Logging Pipeline (Read + Create) |
| `UsecaseValidationPipeline.cs` | Validation Pipeline (CreateFail) |
| `UsecaseCachingPipeline.cs` | Caching Pipeline (Read + Create, Query 전용) |
| `UsecaseExceptionPipeline.cs` | Exception Pipeline (CreateFail) |
| `UsecaseTransactionPipeline.cs` | Transaction Pipeline (Read + Create, Command 전용) |
| `ICustomUsecasePipeline.cs` | Custom Pipeline 마커 인터페이스 (Scrutor 자동 검색용) |
| `UsecaseMetricCustomPipelineBase.cs` | Custom Metric Pipeline 베이스 클래스 |
| `UsecaseTracingCustomPipelineBase.cs` | Custom Tracing Pipeline 베이스 클래스 |
| `IUsecaseCtxEnricher.cs` | 로그 커스텀 속성 Enricher 인터페이스 |

---

## 관련 튜토리얼

| 튜토리얼 | 위치 | 설명 |
|------|------|------|
| CQRS 패턴으로 Command와 Query 분리하기 | `Docs.Site/src/content/docs/tutorials/cqrs-repository/` | CQRS 패턴 기초부터 Usecase 통합까지 |

