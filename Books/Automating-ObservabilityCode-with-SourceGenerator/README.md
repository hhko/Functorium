# 소스 생성기를 이용한 관찰 가능성 코드 자동화하기

**C# Roslyn API로 로깅, 추적, 메트릭 코드를 자동 생성하는 실전 가이드**

---

## 이 책에 대하여

이 책은 **C# 소스 생성기(Source Generator)**를 처음부터 배워 실전에서 활용할 수 있도록 안내합니다. Roslyn 컴파일러 플랫폼의 기초부터 시작하여, **IIncrementalGenerator 패턴**을 활용한 고성능 소스 생성기 개발까지 단계별로 학습합니다.

> **반복적인 보일러플레이트 코드를 자동화하고, 100% 일관된 관찰 가능성을 보장하는 소스 생성기를 직접 구현해보세요.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기초 문법을 알지만 소스 생성기는 처음인 개발자 | Part 0~1 |
| 🟡 **중급** | Roslyn API 경험이 없는 개발자 | Part 2 전체 |
| 🔴 **고급** | 반복적인 보일러플레이트 코드를 자동화하고 싶은 개발자 | Part 3~4 + 부록 |

### 학습 목표

이 책을 완료하면 다음을 할 수 있습니다:

1. **Roslyn 컴파일러 플랫폼**의 구조와 동작 원리 이해
2. **IIncrementalGenerator 인터페이스**를 구현한 소스 생성기 개발
3. **심볼 분석**을 통한 코드 메타데이터 추출
4. **결정적(deterministic) 코드 생성** 기법 적용
5. 소스 생성기 **단위 테스트** 작성

---

## 목차

### Part 0: 서론

소스 생성기의 개념과 필요성을 이해합니다.

- [0.1 소스 생성기란?](Part0-Introduction/01-what-is-source-generator.md)
- [0.2 소스 생성기가 필요한 이유](Part0-Introduction/02-why-source-generator.md)
- [0.3 프로젝트 개요](Part0-Introduction/03-project-overview.md)

### Part 1: 기초

개발 환경을 설정하고 Roslyn 컴파일러 플랫폼을 이해합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [개발 환경](Part1-Fundamentals/01-development-environment.md) | 개발 환경 설정 |
| 2 | [프로젝트 구조](Part1-Fundamentals/02-project-structure.md) | 소스 생성기 프로젝트 구성 |
| 3 | [디버깅 설정](Part1-Fundamentals/03-debugging-setup.md) | 디버깅 환경 구축 |
| 4 | [Roslyn 아키텍처](Part1-Fundamentals/04-roslyn-architecture.md) | 컴파일러 플랫폼 구조 |
| 5 | [Syntax API](Part1-Fundamentals/05-syntax-api.md) | 구문 트리 분석 |
| 6 | [Semantic API](Part1-Fundamentals/06-semantic-api.md) | 의미 분석 |
| 7 | [심볼 타입](Part1-Fundamentals/07-symbol-types.md) | 심볼 유형 이해 |

### Part 2: 핵심 개념

증분 소스 생성기 구현과 코드 생성 기법을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [IIncrementalGenerator 인터페이스](Part2-Core-Concepts/01-iincrementalgenerator-interface.md) | 증분 생성기 인터페이스 |
| 2 | [Provider 패턴](Part2-Core-Concepts/02-provider-pattern.md) | 데이터 제공자 패턴 |
| 3 | [ForAttributeWithMetadataName](Part2-Core-Concepts/03-forattributewithmetadataname.md) | 속성 기반 필터링 |
| 4 | [증분 캐싱](Part2-Core-Concepts/04-incremental-caching.md) | 성능 최적화 |
| 5 | [INamedTypeSymbol](Part2-Core-Concepts/05-inamedtypesymbol.md) | 타입 심볼 분석 |
| 6 | [IMethodSymbol](Part2-Core-Concepts/06-imethodsymbol.md) | 메서드 심볼 분석 |
| 7 | [SymbolDisplayFormat](Part2-Core-Concepts/07-symboldisplayformat.md) | 심볼 표시 형식 |
| 8 | [타입 추출](Part2-Core-Concepts/08-type-extraction.md) | 타입 정보 추출 |
| 9 | [StringBuilder 패턴](Part2-Core-Concepts/09-stringbuilder-pattern.md) | 코드 생성 기본 |
| 10 | [템플릿 설계](Part2-Core-Concepts/10-template-design.md) | 코드 템플릿 구조화 |
| 11 | [네임스페이스 처리](Part2-Core-Concepts/11-namespace-handling.md) | 네임스페이스 관리 |
| 12 | [결정적 출력](Part2-Core-Concepts/12-deterministic-output.md) | 결정적 코드 생성 |

### Part 3: 고급

복잡한 케이스 처리와 테스트 전략을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [생성자 처리](Part3-Advanced/01-constructor-handling.md) | 생성자 분석 및 생성 |
| 2 | [제네릭 타입](Part3-Advanced/02-generic-types.md) | 제네릭 타입 처리 |
| 3 | [컬렉션 타입](Part3-Advanced/03-collection-types.md) | 컬렉션 타입 처리 |
| 4 | [LoggerMessage.Define 제한](Part3-Advanced/04-loggermessage-define-limits.md) | 로거 메시지 제약 |
| 5 | [단위 테스트 설정](Part3-Advanced/05-unit-testing-setup.md) | 테스트 환경 구축 |
| 6 | [Verify 스냅샷 테스트](Part3-Advanced/06-verify-snapshot-testing.md) | 스냅샷 테스트 |
| 7 | [테스트 시나리오](Part3-Advanced/07-test-scenarios.md) | 테스트 케이스 작성 |

### Part 4: 개발 절차서

다양한 실용적 예제를 통해 소스 생성기 개발 절차를 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [개발 워크플로우](Part4-Cookbook/01-development-workflow.md) | 개발 절차 개요 |
| 2 | [Entity Id 생성기](Part4-Cookbook/02-entity-id-generator.md) | DDD 강타입 Id (Ulid 기반) |
| 3 | [EF Core 값 변환기](Part4-Cookbook/03-efcore-value-converter.md) | ValueConverter 자동 생성 |
| 4 | [Validation 생성기](Part4-Cookbook/04-validation-generator.md) | FluentValidation 규칙 생성 |
| 5 | [커스텀 생성기 템플릿](Part4-Cookbook/05-custom-generator-template.md) | 새 프로젝트 시작 가이드 |

### Part 5: 결론

전체 내용을 정리하고 다음 단계를 안내합니다.

- [5.1 정리](Part5-Conclusion/01-summary.md)
- [5.2 다음 단계](Part5-Conclusion/02-next-steps.md)

### [부록](appendix/)

- [A. 개발 환경 준비](appendix/A-development-environment.md)
- [B. API 레퍼런스](appendix/B-api-reference.md)
- [C. 테스트 시나리오 카탈로그](appendix/C-test-scenario-catalog.md)
- [D. 문제 해결](appendix/D-troubleshooting.md)

---

## 실습 진화 과정

```
Phase 1: Hello World (Part 0~1)
├── 가장 단순한 소스 생성기 만들기
└── 난이도: ★☆☆☆☆

Phase 2: 속성 기반 필터링 (Part 2: 1~4장)
├── [GeneratePipeline] 속성이 붙은 클래스만 처리
└── 난이도: ★★☆☆☆

Phase 3: 메서드 분석 (Part 2: 5~8장)
├── 인터페이스의 메서드 시그니처 추출
└── 난이도: ★★★☆☆

Phase 4: 코드 생성 (Part 2: 9~12장)
├── 추출된 정보로 Pipeline 클래스 생성
└── 난이도: ★★★★☆

Phase 5: 고급 처리 (Part 3)
├── 생성자, 제네릭, 컬렉션 등 복잡한 케이스 처리
└── 난이도: ★★★★★
```

---

## 실습 프로젝트: AdapterPipelineGenerator

이 책에서는 **AdapterPipelineGenerator**라는 실제 소스 생성기를 단계별로 구현합니다.

### AdapterPipelineGenerator란?

어댑터 클래스에 **관찰 가능성(Observability)** 코드를 자동으로 생성하는 소스 생성기입니다:

| 기능 | 설명 |
|------|------|
| **로깅(Logging)** | 요청/응답 자동 기록, 고성능 LoggerMessage.Define 사용 |
| **추적(Tracing)** | 분산 추적 Activity 자동 생성 및 컨텍스트 전파 |
| **메트릭(Metrics)** | 응답 시간, 성공/실패 카운터 자동 측정 |

```csharp
// 개발자가 작성하는 코드 - 비즈니스 로직만 집중
[GeneratePipeline]
public class UserRepository(ILogger<UserRepository> logger) : IAdapter
{
    public FinT<IO, User> GetUserAsync(int id) => /* 순수 로직 */;
}

// 소스 생성기가 자동 생성 - 관찰 가능성 코드 포함
public class UserRepositoryPipeline : UserRepository
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
Automating-ObservabilityCode-with-SourceGenerator/
├── Part0-Introduction/         # Part 0: 서론
│   ├── 01-what-is-source-generator.md
│   ├── 02-why-source-generator.md
│   └── 03-project-overview.md
├── Part1-Fundamentals/         # Part 1: 기초
│   ├── 01-development-environment.md
│   ├── 02-project-structure.md
│   ├── 03-debugging-setup.md
│   ├── 04-roslyn-architecture.md
│   ├── 05-syntax-api.md
│   ├── 06-semantic-api.md
│   └── 07-symbol-types.md
├── Part2-Core-Concepts/        # Part 2: 핵심 개념
│   ├── 01-iincrementalgenerator-interface.md
│   ├── ...
│   └── 12-deterministic-output.md
├── Part3-Advanced/             # Part 3: 고급
│   ├── 01-constructor-handling.md
│   ├── ...
│   └── 07-test-scenarios.md
├── Part4-Cookbook/             # Part 4: 개발 절차서
│   ├── 01-development-workflow.md
│   ├── ...
│   └── 05-custom-generator-template.md
├── Part5-Conclusion/           # Part 5: 결론
│   ├── 01-summary.md
│   └── 02-next-steps.md
├── appendix/                   # 부록
│   ├── A-development-environment.md
│   ├── B-api-reference.md
│   ├── C-test-scenario-catalog.md
│   └── D-troubleshooting.md
└── README.md                   # 이 문서
```

---

## 소스 코드

이 책의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- 소스 생성기: `Src/Functorium.Adapters.SourceGenerator/`
- 테스트: `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/`
- 테스트 유틸리티: `Src/Functorium.Testing/SourceGenerators/`

---

이 책은 Functorium 프로젝트의 실제 소스 생성기 개발 경험을 바탕으로 작성되었습니다.
