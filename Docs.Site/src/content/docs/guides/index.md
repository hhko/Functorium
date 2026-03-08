---
title: "Functorium 가이드"
---

비즈니스 로직과 기술 코드가 뒤섞여 변경이 어렵고, 테스트하기 힘든 경험을 해본 적이 있나요? Functorium은 DDD 전술적 설계와 함수형 프로그래밍을 결합하여 관심사를 명확히 분리하고, 프로젝트에 필요한 아키텍처·구현 패턴·관측성까지 모든 가이드를 제공합니다.

## 들어가며

개발 현장에서 이런 고민을 마주한 적이 있을 것입니다.

- **비즈니스 규칙이 인프라 코드 사이에 흩어져** 변경할 때마다 어디를 수정해야 할지 파악하기 어렵지 않았나요?
- **도메인 로직을 단위 테스트하려면** 데이터베이스나 외부 서비스를 함께 준비해야 하는 부담을 느낀 적이 없나요?
- **프로젝트가 커질수록 레이어 간 의존성이 꼬여** 한 곳을 고치면 다른 곳이 깨지는 악순환을 경험하지 않았나요?

Functorium은 이러한 문제를 **DDD 전술적 설계 패턴으로 관심사를 분리하고, 함수형 프로그래밍으로 부수효과를 통제하여** 해결합니다.

### 이 가이드에서 다루는 내용

1. **내부 아키텍처 설계 원칙** — 관심사 분리, 레이어 구조, 의존성 방향의 근거
2. **DDD 전술적 설계 빌딩블록** — Value Object, Entity, Aggregate, Domain Event, Specification
3. **Application/Adapter 계층 구현** — Use Case, Port, Adapter, Pipeline, DI
4. **테스트 전략** — 단위 테스트, 통합 테스트, 테스트 라이브러리
5. **관측성(Observability)** — 로깅, 메트릭, 트레이싱 사양과 구현

> **Functorium 가이드는** WHY → WHAT → HOW 순서로 구성되어, 각 개념이 왜 필요한지 이해한 후 구현으로 나아갈 수 있도록 안내합니다.

## DDD 전술적 설계와 Functorium

Functorium은 DDD 전술적 설계 패턴과 함수형 프로그래밍을 결합한 프레임워크입니다. 가이드 문서는 DDD 빌딩블록 순서로 배치되어 있으며, 각 문서는 **WHY → WHAT → HOW** 구조를 따릅니다.

## 학습 로드맵

```
[00] 00-writing-guide.md ─── 문서 작성 가이드

Architecture
├── [00] 00-architecture-design-principles.md ─── 내부 아키텍처 설계 원칙
├── [01] 01-project-structure.md ─── 프로젝트 구조
├── [02] 02-solution-configuration.md ─── 솔루션 구성
├── [02b] 02b-ci-cd-and-versioning.md ─── CI/CD 워크플로우 및 버전 관리
└── [03] 03-dotnet-tools.md ─── .NET 도구

[04] 04-ddd-tactical-overview.md ─── DDD 전술적 설계 개요
│
├── Domain Layer
│   ├── [05a] 05a-value-objects.md ─── 값 객체 (핵심 개념·검증·구현 패턴)
│   ├── [05b] 05b-value-objects-validation.md ─── 값 객체 (열거형·실전·FAQ)
│   │   └── [06a] 06a-aggregate-design.md ─── Aggregate 설계 (WHY + WHAT)
│   │       ├── [06b] 06b-entity-aggregate-core.md ─── Entity/Aggregate 핵심 패턴 (HOW)
│   │       └── [06c] 06c-entity-aggregate-advanced.md ─── Entity/Aggregate 고급 패턴
│   │           └── [07] 07-domain-events.md ─── 도메인 이벤트
│   ├── [08a] 08a-error-system.md ─── 에러 시스템: 기초와 네이밍
│   ├── [08b] 08b-error-system-domain-app.md ─── 에러 시스템: Domain/Application 에러
│   ├── [08c] 08c-error-system-adapter-testing.md ─── 에러 시스템: Adapter 에러와 테스트
│   ├── [09] 09-domain-services.md ─── 도메인 서비스
│   └── [10] 10-specifications.md ─── Specification 패턴
│
├── Application Layer
│   └── [11] 11-usecases-and-cqrs.md ─── Use Case와 CQRS
│
├── Adapter Layer
│   ├── [12] 12-ports.md ─── Port 정의
│   ├── [13] 13-adapters.md ─── Adapter 구현
│   ├── [14a] 14a-adapter-pipeline-di.md ─── Pipeline, DI
│   ├── [14b] 14b-adapter-testing.md ─── 단위 테스트
│   └── [14c] 14c-repository-query-implementation-guide.md ─── Repository & Query 구현 가이드
│
├── Testing
│   ├── [15] 15a-unit-testing.md ─── 단위 테스트
│   ├── [15b] 15b-integration-testing.md ─── 통합 테스트
│   └── [16] 16-testing-library.md ─── 테스트 라이브러리
│
├── DTO 전략
│   └── [17] 17-dto-strategy.md ─── DTO 전략
│
├── Observability
│   ├── [18a] 18a-observability-spec.md ─── 사양
│   ├── [18b] 18b-observability-naming.md ─── 네이밍
│   ├── [19] 19-observability-logging.md ─── 로깅
│   ├── [20] 20-observability-metrics.md ─── 메트릭
│   └── [21] 21-observability-tracing.md ─── 트레이싱
│
├── 진단
│   └── [22] 22-crash-diagnostics.md ─── 크래시 덤프
│
└── Appendix
    ├── [A01] A01-vscode-debugging.md ─── VSCode 디버깅
    ├── [A02] A02-git-reference.md ─── Git 참조
    ├── [A03] A03-response-type-evolution.md ─── FinResponse 타입 진화
    └── [A04] A04-architecture-rules-coverage.md ─── 아키텍처 규칙 검증 커버리지
```

## 빠른 참조 (작업별 가이드 바로가기)

| 하고 싶은 작업 | 참조 문서 |
|---------------|----------|
| **프로젝트 구성/폴더 구조** | [01-project-structure.md](./architecture/01-project-structure) |
| **아키텍처 설계 원칙 이해하기** | [00-architecture-design-principles.md](./architecture/00-architecture-design-principles) |
| **솔루션 구성 파일/빌드 스크립트** | [02-solution-configuration.md](./architecture/02-solution-configuration) |
| **도구 사용법 (커버리지/스냅샷/ER 다이어그램)** | [03-dotnet-tools.md](./architecture/03-dotnet-tools) |
| **값 객체 만들기** | [05a-value-objects.md](./domain/05a-value-objects) |
| **검증 메서드 확인** | [05a-value-objects.md — 검증 시스템](./domain/05a-value-objects) |
| **열거형(SmartEnum) 패턴** | [05b-value-objects-validation.md — 열거형 구현 패턴](./domain/05b-value-objects-validation) |
| **Aggregate 경계 설계하기** | [06a-aggregate-design.md](./domain/06a-aggregate-design) |
| **Entity/Aggregate 구현하기** | [06b-entity-aggregate-core.md](./domain/06b-entity-aggregate-core) |
| **생성 패턴 (Create/CreateFromValidated)** | [06b-entity-aggregate-core.md — 생성 패턴](./domain/06b-entity-aggregate-core) |
| **Cross-Aggregate 관계, 부가 인터페이스** | [06c-entity-aggregate-advanced.md](./domain/06c-entity-aggregate-advanced) |
| **도메인 이벤트 정의/발행** | [07-domain-events.md](./domain/07-domain-events) |
| **Event Handler 만들기** | [07-domain-events.md — 5. Event Handler](./domain/07-domain-events) |
| **에러 타입 정의하기** | [08a-error-system.md](./domain/08a-error-system), [08b-error-system-domain-app.md](./domain/08b-error-system-domain-app), [08c-error-system-adapter-testing.md](./domain/08c-error-system-adapter-testing) |
| **에러 테스트 작성하기** | [08b-error-system-domain-app.md](./domain/08b-error-system-domain-app), [08c-error-system-adapter-testing.md — 테스트 모범 사례](./domain/08c-error-system-adapter-testing) |
| **범용 에러 Assertion (ErrorCode, Exceptional)** | [08c-error-system-adapter-testing.md — 범용 에러 Assertion 유틸리티](./domain/08c-error-system-adapter-testing) |
| **도메인 서비스 만들기** | [09-domain-services.md](./domain/09-domain-services) |
| **Specification 만들기** | [10-specifications.md](./domain/10-specifications) |
| **Usecase 만들기** | [11-usecases-and-cqrs.md](./application/11-usecases-and-cqrs) |
| **Port 인터페이스 정의하기** | [12-ports.md](./adapter/12-ports) |
| **Adapter 구현하기** | [13-adapters.md](./adapter/13-adapters) |
| **EF Core Repository 만들기** | [13-adapters.md — §2.3 Repository Adapter](./adapter/13-adapters) |
| **Persistence Model/Mapper 만들기** | [13-adapters.md — §2.2 공통 패턴](./adapter/13-adapters) |
| **Endpoint Response DTO 만들기** | [13-adapters.md — §2.2 공통 패턴](./adapter/13-adapters) |
| **Options 패턴 (OptionsConfigurator)** | [14a-adapter-pipeline-di.md — §4.6 Options 패턴](./adapter/14a-adapter-pipeline-di) |
| **Pipeline/DI 등록** | [14a-adapter-pipeline-di.md](./adapter/14a-adapter-pipeline-di) |
| **DTO 전략/재사용 규칙** | [17-dto-strategy.md](./application/17-dto-strategy) |
| **크래시 덤프 설정/분석** | [22-crash-diagnostics.md](./observability/22-crash-diagnostics) |
| **Observability 사양** | [18a-observability-spec.md](./observability/18a-observability-spec) |
| **구조화된 로그 테스트** | [16-testing-library.md — 구조화된 로그 테스트](./testing/16-testing-library) |
| **아키텍처 규칙 검증** | [16-testing-library.md — 아키텍처 규칙 검증](./testing/16-testing-library) |
| **아키텍처 규칙 커버리지 매트릭스** | [A04-architecture-rules-coverage.md](./appendix/A04-architecture-rules-coverage) |
| **소스 생성기 테스트** | [16-testing-library.md — 소스 생성기 테스트](./testing/16-testing-library) |
| **스케줄 Job 테스트** | [16-testing-library.md — 스케줄 Job 통합 테스트](./testing/16-testing-library) |
| **모듈과 프로젝트 구조 매핑** | [04-ddd-tactical-overview.md — §6](./domain/04-ddd-tactical-overview) |
| **네이밍 규칙/용어집** | [04-ddd-tactical-overview.md — §7](./domain/04-ddd-tactical-overview) |
| **Bounded Context/Context Map** | [04-ddd-tactical-overview.md — §8](./domain/04-ddd-tactical-overview) |
| **DTO 전략 리뷰 확인** | [dto-strategy-review.md](../../.claude/dto-strategy-review.md) |
| **CI/CD 워크플로우 및 버전 관리** | [02b-ci-cd-and-versioning.md](./architecture/02b-ci-cd-and-versioning) |
| **FinResponse 타입 진화** | [A03-response-type-evolution.md](./appendix/A03-response-type-evolution) |
| **새 Repository 구현** | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) |
| **새 Query Adapter 구현** | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) |
| **CRUD 대칭성 확인** | [14c-repository-query-implementation-guide.md — §2.7](./adapter/14c-repository-query-implementation-guide) |
| **통합 테스트 (HostTestFixture)** | [15b-integration-testing.md](./testing/15b-integration-testing) |
| **VSCode 디버깅/개발 환경 설정** | [A01-vscode-debugging.md](./appendix/A01-vscode-debugging) |
| **Git 명령어/Hooks** | [A02-git-reference.md](./appendix/A02-git-reference) |
| **캐싱 파이프라인 설정** | [14a-adapter-pipeline-di.md](./adapter/14a-adapter-pipeline-di) |
| **Cursor 페이지네이션 구현** | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) |
| **DapperSpecTranslator 사용** | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) |
| **문서 작성 가이드** | [00-writing-guide.md](./architecture/00-writing-guide) |

## 코드 예시 규칙

| 구분 | 형식 | 설명 |
|------|------|------|
| 규칙 구현 코드 | 실제 C# 코드 | 컴파일 가능한 수준의 코드 (타입, 메서드, 패턴 예시) |
| 아키텍처 흐름 설명 | pseudo-code 허용 | 반드시 `pseudo-code` 또는 `개념 코드` 라벨을 코드 블록 앞에 표기 |
| 코드 블록 언어 태그 | 항상 명시 | ` ```csharp `, ` ```xml `, ` ```bash `, ` ```promql ` 등 |

## 문서 전체 목록

### DDD 전술적 설계 (번호순 학습 경로)

| 번호 | 문서 | 설명 |
|------|------|------|
| 04 | [04-ddd-tactical-overview.md](./domain/04-ddd-tactical-overview) | DDD 전술적 설계 개요, 빌딩블록 맵, Functorium 타입 매핑 |
| 05a | [05a-value-objects.md](./domain/05a-value-objects) | 값 객체 (핵심 개념, 기반 클래스, 검증 시스템, 구현 패턴) |
| 05b | [05b-value-objects-validation.md](./domain/05b-value-objects-validation) | 값 객체 (열거형 패턴, 실전 예제, Application 검증, FAQ) |
| 06a | [06a-aggregate-design.md](./domain/06a-aggregate-design) | Aggregate 설계 (WHY + WHAT: 설계 규칙, 경계 설정, 안티패턴) |
| 06b | [06b-entity-aggregate-core.md](./domain/06b-entity-aggregate-core) | Entity/Aggregate 핵심 패턴 (HOW: 클래스 계층, ID, 생성 패턴, 이벤트) |
| 06c | [06c-entity-aggregate-advanced.md](./domain/06c-entity-aggregate-advanced) | Entity/Aggregate 고급 패턴 (Cross-Aggregate 관계, 부가 인터페이스, 실전 예제) |
| 07 | [07-domain-events.md](./domain/07-domain-events) | 도메인 이벤트 정의, 발행, 핸들러 구현 |
| 08a | [08a-error-system.md](./domain/08a-error-system) | 에러 시스템: 기초와 네이밍 (WHY, Fin 패턴, 네이밍 규칙) |
| 08b | [08b-error-system-domain-app.md](./domain/08b-error-system-domain-app) | 에러 시스템: Domain/Application 에러 (Domain/Application/Event 에러 정의와 테스트) |
| 08c | [08c-error-system-adapter-testing.md](./domain/08c-error-system-adapter-testing) | 에러 시스템: Adapter 에러와 테스트 (Adapter 에러, Custom 에러, 테스트 모범 사례, 체크리스트) |
| 09 | [09-domain-services.md](./domain/09-domain-services) | 도메인 서비스 (교차 Aggregate 순수 로직, IDomainService) |
| 10 | [10-specifications.md](./domain/10-specifications) | Specification 패턴 (비즈니스 규칙 캡슐화, 조합, Repository 통합) |
| 11 | [11-usecases-and-cqrs.md](./application/11-usecases-and-cqrs) | Use Case 구현 (CQRS Command/Query) |
| 12 | [12-ports.md](./adapter/12-ports) | Port 아키텍처, IObservablePort 계층, Port 정의 규칙 |
| 13 | [13-adapters.md](./adapter/13-adapters) | Adapter 구현 (Repository, External API, Messaging, Query) |
| 14a | [14a-adapter-pipeline-di.md](./adapter/14a-adapter-pipeline-di) | Pipeline 생성, DI 등록, Options 패턴 |
| 14b | [14b-adapter-testing.md](./adapter/14b-adapter-testing) | Adapter 단위 테스트, E2E Walkthrough |
| 14c | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) | Repository & Query 구현 가이드 |
| 17 | [17-dto-strategy.md](./application/17-dto-strategy) | DTO 전략 (레이어별 소유권, 재사용 규칙, 변환 패턴) |

### 아키텍처

| 문서 | 설명 |
|------|------|
| [00-architecture-design-principles.md](./architecture/00-architecture-design-principles) | 내부 아키텍처 설계 원칙 (관심사 분리, 레이어 구조, 의존성 방향) |
| [01-project-structure.md](./architecture/01-project-structure) | 서비스 프로젝트 구성 (폴더, 네이밍, 의존성) |
| [02-solution-configuration.md](./architecture/02-solution-configuration) | 솔루션 루트 구성 파일 및 빌드 스크립트 |
| [02b-ci-cd-and-versioning.md](./architecture/02b-ci-cd-and-versioning) | CI/CD 워크플로우 및 버전 관리 (GitHub Actions, NuGet 패키지, MinVer, 버전 제안 커맨드) |
| [03-dotnet-tools.md](./architecture/03-dotnet-tools) | .NET 도구 가이드 (CLI 도구, 소스 생성기, 파일 기반 스크립트) |

### 테스트

| 문서 | 설명 |
|------|------|
| [15a-unit-testing.md](./testing/15a-unit-testing) | 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정) |
| [15b-integration-testing.md](./testing/15b-integration-testing) | 통합 테스트 (HostTestFixture, 환경 설정) |
| [16-testing-library.md](./testing/16-testing-library) | Functorium.Testing 라이브러리 (로그/아키텍처/소스생성기/Job 테스트) |

### Observability

| 문서 | 설명 |
|------|------|
| [18a-observability-spec.md](./observability/18a-observability-spec) | Observability 사양 (Field/Tag, Meter, 메시지 템플릿) |
| [18b-observability-naming.md](./observability/18b-observability-naming) | Observability 네이밍 가이드 (코드, Logger 메서드) |
| [19-observability-logging.md](./observability/19-observability-logging) | Observability 로깅 상세 |
| [20-observability-metrics.md](./observability/20-observability-metrics) | Observability 메트릭 상세 |
| [21-observability-tracing.md](./observability/21-observability-tracing) | Observability 트레이싱 상세 |

### 진단

| 문서 | 설명 |
|------|------|
| [22-crash-diagnostics.md](./observability/22-crash-diagnostics) | 크래시 덤프 핸들러 설정 및 분석 가이드 |

### Appendix

| 문서 | 설명 |
|------|------|
| [A01-vscode-debugging.md](./appendix/A01-vscode-debugging) | VSCode 디버깅 및 개발 환경 설정 |
| [A02-git-reference.md](./appendix/A02-git-reference) | Git 명령어 참조 및 Git Hooks |
| [A03-response-type-evolution.md](./appendix/A03-response-type-evolution) | FinResponse 타입 진화 기록 |
| [A04-architecture-rules-coverage.md](./appendix/A04-architecture-rules-coverage) | 아키텍처 규칙 검증 커버리지 매트릭스 |

### 리뷰

| 문서 | 설명 |
|------|------|
| [dto-strategy-review.md](../../.claude/dto-strategy-review.md) | DTO 매핑 전략 리뷰 (DDD & Hexagonal Architecture 관점) |

### 기타

| 문서 | 설명 |
|------|------|
| [book-writing-guide.md](../../.claude/book-writing-guide.md) | 서적 집필 가이드 |
| [00-writing-guide.md](./architecture/00-writing-guide) | 문서 작성 가이드 (가이드 문서 작성 규칙) |
