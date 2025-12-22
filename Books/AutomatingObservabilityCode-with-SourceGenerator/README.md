# 소스 생성기를 이용한 관찰 가능성 코드 자동화하기

**C# Roslyn API로 로깅, 추적, 메트릭 코드를 자동 생성하는 실전 가이드**

---

## 이 책에 대하여

이 책은 C# 개발자가 소스 생성기(Source Generator)를 처음부터 배워 실전에서 활용할 수 있도록 안내합니다. Roslyn 컴파일러 플랫폼의 기초부터 시작하여, IIncrementalGenerator 패턴을 활용한 고성능 소스 생성기 개발까지 단계별로 학습합니다.

### 대상 독자

- C# 기초 문법을 알지만 소스 생성기는 처음인 개발자
- Roslyn API 경험이 없는 초보자
- 반복적인 보일러플레이트 코드를 자동화하고 싶은 개발자

### 학습 목표

이 책을 완료하면 다음을 할 수 있습니다:

- Roslyn 컴파일러 플랫폼의 구조와 동작 원리 이해
- IIncrementalGenerator 인터페이스를 구현한 소스 생성기 개발
- 심볼 분석을 통한 코드 메타데이터 추출
- 결정적(deterministic) 코드 생성 기법 적용
- 소스 생성기 단위 테스트 작성

---

## 목차

### Part 1: 기초

#### [1장: 소개](01-introduction/)

소스 생성기란 무엇인가를 살펴봅니다.

- [1.1 소스 생성기란?](01-introduction/01-what-is-source-generator.md)
- [1.2 소스 생성기가 필요한 이유](01-introduction/02-why-source-generator.md)
- [1.3 프로젝트 개요](01-introduction/03-project-overview.md)

#### [2장: 사전 준비](02-prerequisites/)

개발 환경을 설정하고 필요한 도구를 설치합니다.

- [2.1 개발 환경](02-prerequisites/01-development-environment.md)
- [2.2 프로젝트 구조](02-prerequisites/02-project-structure.md)
- [2.3 디버깅 설정](02-prerequisites/03-debugging-setup.md)

#### [3장: Roslyn 기초](03-roslyn-fundamentals/)

컴파일러 플랫폼의 구조와 동작 원리를 이해합니다.

- [3.1 Roslyn 아키텍처](03-roslyn-fundamentals/01-roslyn-architecture.md)
- [3.2 Syntax API](03-roslyn-fundamentals/02-syntax-api.md)
- [3.3 Semantic API](03-roslyn-fundamentals/03-semantic-api.md)
- [3.4 심볼 타입](03-roslyn-fundamentals/04-symbol-types.md)

### Part 2: 핵심 개념

#### [4장: IIncrementalGenerator 패턴](04-incremental-generator-pattern/)

증분 소스 생성기를 구현하는 방법을 배웁니다.

- [4.1 IIncrementalGenerator 인터페이스](04-incremental-generator-pattern/01-iincrementalgenerator-interface.md)
- [4.2 Provider 패턴](04-incremental-generator-pattern/02-provider-pattern.md)
- [4.3 ForAttributeWithMetadataName](04-incremental-generator-pattern/03-forattributewithmetadataname.md)
- [4.4 증분 캐싱](04-incremental-generator-pattern/04-incremental-caching.md)

#### [5장: 심볼 분석](05-symbol-analysis/)

코드 메타데이터를 추출하는 방법을 배웁니다.

- [5.1 INamedTypeSymbol](05-symbol-analysis/01-inamedtypesymbol.md)
- [5.2 IMethodSymbol](05-symbol-analysis/02-imethodsymbol.md)
- [5.3 SymbolDisplayFormat](05-symbol-analysis/03-symboldisplayformat.md)
- [5.4 타입 추출](05-symbol-analysis/04-type-extraction.md)

#### [6장: 코드 생성](06-code-generation/)

소스 코드를 출력하는 방법을 배웁니다.

- [6.1 StringBuilder 패턴](06-code-generation/01-stringbuilder-pattern.md)
- [6.2 템플릿 설계](06-code-generation/02-template-design.md)
- [6.3 네임스페이스 처리](06-code-generation/03-namespace-handling.md)
- [6.4 결정적 출력](06-code-generation/04-deterministic-output.md)

### Part 3: 실전

#### [7장: 고급 시나리오](07-advanced-scenarios/)

복잡한 케이스를 처리하는 방법을 배웁니다.

- [7.1 생성자 처리](07-advanced-scenarios/01-constructor-handling.md)
- [7.2 제네릭 타입](07-advanced-scenarios/02-generic-types.md)
- [7.3 컬렉션 타입](07-advanced-scenarios/03-collection-types.md)
- [7.4 LoggerMessage.Define 제한](07-advanced-scenarios/04-loggermessage-define-limits.md)

#### [8장: 테스트 전략](08-testing-strategies/)

소스 생성기 단위 테스트를 작성하는 방법을 배웁니다.

- [8.1 단위 테스트 설정](08-testing-strategies/01-unit-testing-setup.md)
- [8.2 Verify 스냅샷 테스트](08-testing-strategies/02-verify-snapshot-testing.md)
- [8.3 테스트 시나리오](08-testing-strategies/03-test-scenarios.md)

#### [9장: 결론](09-conclusion/)

전체 내용을 정리하고 다음 단계를 안내합니다.

- [9.1 정리](09-conclusion/01-summary.md)
- [9.2 다음 단계](09-conclusion/02-next-steps.md)

### [부록](appendix/)

- [A. 개발 환경 준비](appendix/A-development-environment.md) - 상세 환경 설정
- [B. API 레퍼런스](appendix/B-api-reference.md) - Roslyn API 빠른 참조
- [C. 테스트 시나리오 카탈로그](appendix/C-test-scenario-catalog.md) - 27개 테스트 케이스
- [D. 문제 해결](appendix/D-troubleshooting.md) - FAQ 및 디버깅 팁

---

## 실습 프로젝트

이 책에서는 **AdapterPipelineGenerator**라는 실제 소스 생성기를 단계별로 구현합니다.

### AdapterPipelineGenerator란?

어댑터 클래스에 **관찰 가능성(Observability)** 코드를 자동으로 생성하는 소스 생성기입니다:

- **로깅(Logging)**: 요청/응답 자동 기록, 고성능 LoggerMessage.Define 사용
- **추적(Tracing)**: 분산 추적 Activity 자동 생성 및 컨텍스트 전파
- **메트릭(Metrics)**: 응답 시간, 성공/실패 카운터 자동 측정

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

```
실습 진행 순서
==============

Phase 1: Hello World (01-02장)
├── 가장 단순한 소스 생성기 만들기
└── 난이도: ★☆☆☆☆

Phase 2: 속성 기반 필터링 (03-04장)
├── [GeneratePipeline] 속성이 붙은 클래스만 처리
└── 난이도: ★★☆☆☆

Phase 3: 메서드 분석 (05장)
├── 인터페이스의 메서드 시그니처 추출
└── 난이도: ★★★☆☆

Phase 4: 코드 생성 (06장)
├── 추출된 정보로 Pipeline 클래스 생성
└── 난이도: ★★★★☆

Phase 5: 고급 처리 (07장)
├── 생성자, 제네릭, 컬렉션 등 복잡한 케이스 처리
└── 난이도: ★★★★★
```

---

## 필수 준비물

- .NET 10.0 SDK (Preview 또는 정식 버전)
- Visual Studio 2022 (17.12 이상) 또는 VS Code (C# Dev Kit 확장)
- C# 13 기초 문법 지식

---

## 소스 코드

이 책의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- 소스 생성기: `Src/Functorium.Adapters.SourceGenerator/`
- 테스트: `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/`
- 테스트 유틸리티: `Src/Functorium.Testing/SourceGenerators/`

---

## 저자 정보

이 책은 Functorium 프로젝트의 실제 소스 생성기 개발 경험을 바탕으로 작성되었습니다.
