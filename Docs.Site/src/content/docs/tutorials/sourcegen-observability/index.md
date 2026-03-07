---
title: "Source Generator Observability"
---

**C# Roslyn API로 로깅, 추적, 메트릭 코드를 자동 생성하는 실전 가이드**

---

## 이 튜토리얼에 대하여

모든 Repository 메서드마다 로깅, 추적, 메트릭 코드를 손으로 복붙하고 있다면 — 소스 생성기가 그 반복 작업을 끝내줄 수 있습니다.

이 튜토리얼은 **C# 소스 생성기(Source Generator)를** 처음부터 배워 실전에서 활용할 수 있도록 안내합니다. Roslyn 컴파일러 플랫폼의 기초부터 시작하여, **IIncrementalGenerator 패턴**을 활용한 고성능 소스 생성기 개발까지 단계별로 학습합니다.

> **반복적인 보일러플레이트 코드를 자동화하고, 100% 일관된 관찰 가능성을 보장하는 소스 생성기를 직접 구현해보세요.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 기초 문법을 알지만 소스 생성기는 처음인 개발자 | Part 0~1 |
| **중급** | Roslyn API 경험이 없는 개발자 | Part 2 전체 |
| **고급** | 반복적인 보일러플레이트 코드를 자동화하고 싶은 개발자 | Part 3~4 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. **Roslyn 컴파일러 플랫폼**의 구조와 동작 원리 이해
2. **IIncrementalGenerator 인터페이스**를 구현한 소스 생성기 개발
3. **심볼 분석**을 통한 코드 메타데이터 추출
4. **결정적(deterministic) 코드 생성** 기법 적용
5. 소스 생성기 **단위 테스트** 작성

---

### Part 0: 서론

Source Generator의 개념과 필요성을 이해합니다.

- [0.1 Source Generator란?](Part0-Introduction/01-what-is-source-generator.md)
- [0.2 Hello World 생성기](Part0-Introduction/02-hello-world-generator/)
- [0.3 Source Generator가 필요한 이유](Part0-Introduction/03-why-source-generator.md)
- [0.4 Reflection vs Source Generator](Part0-Introduction/04-reflection-vs-sourcegen/)
- [0.5 프로젝트 개요](Part0-Introduction/05-project-overview.md)

### Part 1: 기초

개발 환경을 설정하고 Roslyn 컴파일러 플랫폼을 이해합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [개발 환경](Part1-Fundamentals/01-development-environment.md) | 개발 환경 설정 |
| 2 | [프로젝트 구조](Part1-Fundamentals/02-Data-Models/) | 소스 생성기 프로젝트 구성 |
| 3 | [Debugging 설정](Part1-Fundamentals/03-Debugging-Setup/) | Debugging 환경 구축 |
| 4 | [Roslyn 아키텍처](Part1-Fundamentals/04-Roslyn-Architecture/) | 컴파일러 플랫폼 구조 |
| 5 | [Syntax API](Part1-Fundamentals/05-Syntax-Api/) | 구문 트리 분석 |
| 6 | [Semantic API](Part1-Fundamentals/06-Semantic-Api/) | 의미 분석 |
| 7 | [Symbol Type](Part1-Fundamentals/07-Symbol-Types/) | Symbol 유형 이해 |

### Part 2: 핵심 개념

Incremental Source Generator 구현과 코드 생성 기법을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [IIncrementalGenerator 인터페이스](Part2-Core-Concepts/01-IIncrementalGenerator/) | Incremental Generator 인터페이스 |
| 2 | [Provider Pattern](Part2-Core-Concepts/02-Provider-Pattern/) | 데이터 Provider Pattern |
| 3 | [ForAttributeWithMetadataName](Part2-Core-Concepts/03-ForAttribute/) | 속성 기반 필터링 |
| 4 | [Incremental Caching](Part2-Core-Concepts/04-Incremental-Caching/) | 성능 최적화 |
| 5 | [INamedTypeSymbol](Part2-Core-Concepts/05-INamedTypeSymbol/) | Type Symbol 분석 |
| 6 | [IMethodSymbol](Part2-Core-Concepts/06-IMethodSymbol/) | Method Symbol 분석 |
| 7 | [SymbolDisplayFormat](Part2-Core-Concepts/07-SymbolDisplayFormat/) | Symbol 표시 형식 |
| 8 | [Type 추출](Part2-Core-Concepts/08-Type-Extraction/) | Type 정보 추출 |
| 9 | [StringBuilder Pattern](Part2-Core-Concepts/09-StringBuilder-Pattern/) | 코드 생성 기본 |
| 10 | [Template 설계](Part2-Core-Concepts/10-Template-Design/) | 코드 Template 구조화 |
| 11 | [Namespace 처리](Part2-Core-Concepts/11-Namespace-Handling/) | Namespace 관리 |
| 12 | [Deterministic Output](Part2-Core-Concepts/12-Deterministic-Output/) | Deterministic 코드 생성 |

### Part 3: 고급

복잡한 케이스 처리와 테스트 전략을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Constructor 처리](Part3-Advanced/01-Constructor-Handling/) | Constructor 분석 및 생성 |
| 2 | [Generic Type](Part3-Advanced/02-Generic-Types/) | Generic Type 처리 |
| 3 | [Collection Type](Part3-Advanced/03-Collection-Types/) | Collection Type 처리 |
| 4 | [LoggerMessage.Define 제한](Part3-Advanced/04-LoggerMessage-Limits/) | Logger Message 제약 |
| 5 | [Unit Test 설정](Part3-Advanced/05-Unit-Testing-Setup/) | Test 환경 구축 |
| 6 | [Verify Snapshot Test](Part3-Advanced/06-Verify-Snapshot-Testing/) | Snapshot Test |
| 7 | [Test Scenario](Part3-Advanced/07-Test-Scenarios/) | Test Case 작성 |

### Part 4: 개발 절차서

다양한 실용적 예제를 통해 Source Generator 개발 절차를 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Source Generator 개발 절차](Part4-Cookbook/01-Development-Workflow/) | 개발 절차 개요 |
| 2 | [Entity ID Generator](Part4-Cookbook/02-Entity-Id-Generator/) | DDD 강타입 Id (Ulid 기반) |
| 3 | [EF Core Value Converter](Part4-Cookbook/03-EfCore-Value-Converter/) | ValueConverter 자동 생성 |
| 4 | [Validation 생성기](Part4-Cookbook/04-Validation-Generator/) | FluentValidation 규칙 생성 |
| 5 | [Custom Generator Template](Part4-Cookbook/05-Custom-Generator-Template/) | 새 프로젝트 시작 가이드 |

### Part 5: 결론

전체 내용을 정리하고 다음 단계를 안내합니다.

- [5.1 정리](Part5-Conclusion/01-summary.md)
- [5.2 다음 단계](Part5-Conclusion/02-next-steps.md)

### [부록](Appendix/)

- [A. 개발 환경 준비](Appendix/A-development-environment.md)
- [B. API 레퍼런스](Appendix/B-api-reference.md)
- [C. Test Scenario Catalog](Appendix/C-test-scenario-catalog.md)
- [D. 문제 해결](Appendix/D-troubleshooting.md)

---

## 핵심 진화 과정

가장 단순한 Hello World 생성기에서 출발하여, 점진적으로 복잡성을 높여가며 실전 수준의 소스 생성기를 완성합니다. 각 Phase는 이전 단계의 결과물 위에 쌓이므로, 순서대로 진행하는 것을 권장합니다.

```
Phase 1: Hello World (Part 0~1)
├── 가장 단순한 Source Generator 만들기
└── 난이도: ★☆☆☆☆

Phase 2: 속성 기반 필터링 (Part 2: 1~4장)
├── [GenerateObservablePort] 속성이 붙은 클래스만 처리
└── 난이도: ★★☆☆☆

Phase 3: 메서드 분석 (Part 2: 5~8장)
├── 인터페이스의 메서드 시그니처 추출
└── 난이도: ★★★☆☆

Phase 4: 코드 생성 (Part 2: 9~12장)
├── 추출된 정보로 Observable 클래스 생성
└── 난이도: ★★★★☆

Phase 5: 고급 처리 (Part 3)
├── Constructor, Generic, Collection 등 복잡한 케이스 처리
└── 난이도: ★★★★★
```

---

## 실습 프로젝트: ObservablePortGenerator

이 튜토리얼에서는 **ObservablePortGenerator**라는 실제 Source Generator를 단계별로 구현합니다.

어댑터 메서드 하나를 추가할 때마다 로깅 호출, Activity 생성, 메트릭 카운터 업데이트를 빠짐없이 작성해야 한다면, 팀원 간 구현 편차가 생기고 실수가 발생하는 것은 시간 문제입니다. 이 프로젝트는 바로 그 고통에서 출발합니다 — 관찰 가능성 보일러플레이트를 컴파일 타임에 자동 생성하여, 개발자가 비즈니스 로직에만 집중할 수 있도록 하는 것이 목표입니다.

### ObservablePortGenerator란?

어댑터 클래스에 **관찰 가능성(Observability)** 코드를 자동으로 생성하는 Source Generator입니다:

| 기능 | 설명 |
|------|------|
| **로깅(Logging)** | 요청/응답 자동 기록, 고성능 LoggerMessage.Define 사용 |
| **추적(Tracing)** | 분산 추적 Activity 자동 생성 및 컨텍스트 전파 |
| **메트릭(Metrics)** | 응답 시간, 성공/실패 카운터 자동 측정 |

```csharp
// 개발자가 작성하는 코드 - 비즈니스 로직만 집중
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    public FinT<IO, User> GetUserAsync(int id) => /* 순수 로직 */;
}

// 소스 생성기가 자동 생성 - 관찰 가능성 코드 포함
public class UserRepositoryObservable : UserRepository
{
    // 로깅, 추적, 메트릭이 모든 메서드에 자동 적용
}
```

**기대 효과**: 보일러플레이트 코드 75% 감소, 100% 일관된 관찰 가능성 보장

---

## 필수 준비물

- .NET 10.0 SDK (Preview 또는 정식 버전)
- Visual Studio 2022 (17.12 이상) 또는 VS Code (C# Dev Kit 확장)
- C# 13 기초 문법 지식

---

## 프로젝트 구조

```
sourcegen-observability/
├── Part0-Introduction/         # Part 0: 서론
│   ├── 01-what-is-source-generator.md
│   ├── 02-hello-world-generator/
│   ├── 03-why-source-generator.md
│   ├── 04-reflection-vs-sourcegen/
│   └── 05-project-overview.md
├── Part1-Fundamentals/         # Part 1: 기초
│   ├── 01-development-environment.md
│   ├── 02-Data-Models/
│   ├── 03-Debugging-Setup/
│   ├── ...
│   └── 07-Symbol-Types/
├── Part2-Core-Concepts/        # Part 2: 핵심 개념
│   ├── 01-IIncrementalGenerator/
│   ├── ...
│   └── 12-Deterministic-Output/
├── Part3-Advanced/             # Part 3: 고급
│   ├── 01-Constructor-Handling/
│   ├── ...
│   └── 07-Test-Scenarios/
├── Part4-Cookbook/              # Part 4: 개발 절차서
│   ├── 01-Development-Workflow/
│   ├── ...
│   └── 05-Custom-Generator-Template/
├── Part5-Conclusion/           # Part 5: 결론
│   ├── 01-summary.md
│   └── 02-next-steps.md
└── Appendix/                   # 부록
    ├── A-development-environment.md
    ├── B-api-reference.md
    ├── C-test-scenario-catalog.md
    └── D-troubleshooting.md
```

---

## 소스 코드

이 튜토리얼의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- 소스 생성기: `Src/Functorium.SourceGenerators/`
- 테스트: `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/`
- 테스트 유틸리티: `Src/Functorium.Testing/SourceGenerators/`

---

이 튜토리얼은 Functorium 프로젝트의 실제 소스 생성기 개발 경험을 바탕으로 작성되었습니다.
