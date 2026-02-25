# Functorium 가이드

이 폴더는 Functorium 프레임워크 사용을 위한 Claude Code 가이드 문서를 포함합니다.

## DDD 전술적 설계와 Functorium

Functorium은 DDD 전술적 설계 패턴과 함수형 프로그래밍을 결합한 프레임워크입니다. 가이드 문서는 DDD 빌딩블록 순서로 배치되어 있으며, 각 문서는 **WHY → WHAT → HOW** 구조를 따릅니다.

## 학습 로드맵

```
[00] 00-writing-guide.md ─── 문서 작성 가이드

Architecture
├── [01] 01-project-structure.md ─── 프로젝트 구조
├── [02] 02-solution-configuration.md ─── 솔루션 구성
├── [02b] 02b-ci-cd.md ─── CI/CD 워크플로우
├── [02c] 02c-versioning.md ─── 버전 관리
└── [03] 03-dotnet-tools.md ─── .NET 도구

[04] 04-ddd-tactical-overview.md ─── DDD 전술적 설계 개요
│
├── Domain Layer
│   ├── [05a] 05a-value-objects.md ─── 값 객체 (핵심 개념·검증·구현 패턴)
│   ├── [05b] 05b-value-objects-validation.md ─── 값 객체 (열거형·실전·FAQ)
│   │   └── [06a] 06a-aggregate-design.md ─── Aggregate 설계 (WHY + WHAT)
│   │       └── [06b] 06b-entity-aggregate-implementation.md ─── Entity/Aggregate 구현 (HOW)
│   │           └── [07] 07-domain-events.md ─── 도메인 이벤트
│   ├── [08a] 08a-error-system.md ─── 에러 시스템: 기초와 네이밍
│   ├── [08b] 08b-error-system-layers.md ─── 에러 시스템: 레이어별 구현과 테스트
│   ├── [09] 09-domain-services.md ─── 도메인 서비스
│   └── [10] 10-specifications.md ─── Specification 패턴
│
├── Application Layer
│   ├── [11] 11-usecases-and-cqrs.md ─── Use Case와 CQRS
│   └── [11b] A03-response-type-evolution.md ─── FinResponse 타입 진화
│
├── Adapter Layer
│   ├── [12] 12-ports.md ─── Port 정의
│   ├── [13] 13-adapters.md ─── Adapter 구현
│   └── [14] 14-adapter-wiring.md ─── Pipeline, DI, 테스트
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
│   ├── [18] 18-observability-spec.md ─── 사양
│   ├── [19] 19-observability-naming.md ─── 네이밍
│   ├── [20] 19-observability-logging.md ─── 로깅
│   ├── [21] 20-observability-metrics.md ─── 메트릭
│   └── [22] 21-observability-tracing.md ─── 트레이싱
│
├── 진단
│   └── [23] 22-crash-diagnostics.md ─── 크래시 덤프
│
└── 개발 도구
    ├── [24] A01-vscode-debugging.md ─── VSCode 디버깅
    └── [25] A02-git-reference.md ─── Git 참조
```

## 빠른 참조 (작업별 가이드 바로가기)

| 하고 싶은 작업 | 참조 문서 |
|---------------|----------|
| **프로젝트 구성/폴더 구조** | [01-project-structure.md](./01-project-structure.md) |
| **솔루션 구성 파일/빌드 스크립트** | [02-solution-configuration.md](./02-solution-configuration.md) |
| **도구 사용법 (커버리지/스냅샷/ER 다이어그램)** | [03-dotnet-tools.md](./03-dotnet-tools.md) |
| **값 객체 만들기** | [05a-value-objects.md](./05a-value-objects.md) |
| **검증 메서드 확인** | [05a-value-objects.md — 검증 시스템](./05a-value-objects.md) |
| **열거형(SmartEnum) 패턴** | [05b-value-objects-validation.md — 열거형 구현 패턴](./05b-value-objects-validation.md) |
| **Aggregate 경계 설계하기** | [06a-aggregate-design.md](./06a-aggregate-design.md) |
| **Entity/Aggregate 구현하기** | [06b-entity-aggregate-implementation.md](./06b-entity-aggregate-implementation.md) |
| **생성 패턴 (Create/CreateFromValidated)** | [06b-entity-aggregate-implementation.md — 8. 생성 패턴](./06b-entity-aggregate-implementation.md) |
| **도메인 이벤트 정의/발행** | [07-domain-events.md](./07-domain-events.md) |
| **Event Handler 만들기** | [07-domain-events.md — 5. Event Handler](./07-domain-events.md) |
| **에러 타입 정의하기** | [08a-error-system.md](./08a-error-system.md), [08b-error-system-layers.md](./08b-error-system-layers.md) |
| **에러 테스트 작성하기** | [08b-error-system-layers.md — 4~6. 레이어별 에러 테스트](./08b-error-system-layers.md) |
| **범용 에러 Assertion (ErrorCode, Exceptional)** | [08b-error-system-layers.md — 범용 에러 Assertion 유틸리티](./08b-error-system-layers.md) |
| **도메인 서비스 만들기** | [09-domain-services.md](./09-domain-services.md) |
| **Specification 만들기** | [10-specifications.md](./10-specifications.md) |
| **Usecase 만들기** | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) |
| **Port 인터페이스 정의하기** | [12-ports.md](./12-ports.md) |
| **Adapter 구현하기** | [13-adapters.md](./13-adapters.md) |
| **EF Core Repository 만들기** | [13-adapters.md — §2.3 Repository Adapter](./13-adapters.md) |
| **Persistence Model/Mapper 만들기** | [13-adapters.md — §2.2 공통 패턴](./13-adapters.md) |
| **Endpoint Response DTO 만들기** | [13-adapters.md — §2.2 공통 패턴](./13-adapters.md) |
| **Options 패턴 (OptionsConfigurator)** | [14-adapter-wiring.md — §4.6 Options 패턴](./14-adapter-wiring.md) |
| **Pipeline/DI 등록** | [14-adapter-wiring.md](./14-adapter-wiring.md) |
| **DTO 전략/재사용 규칙** | [17-dto-strategy.md](./17-dto-strategy.md) |
| **크래시 덤프 설정/분석** | [22-crash-diagnostics.md](./22-crash-diagnostics.md) |
| **Observability 사양** | [18-observability-spec.md](./18-observability-spec.md) |
| **구조화된 로그 테스트** | [16-testing-library.md — 구조화된 로그 테스트](./16-testing-library.md) |
| **아키텍처 규칙 검증** | [16-testing-library.md — 아키텍처 규칙 검증](./16-testing-library.md) |
| **소스 생성기 테스트** | [16-testing-library.md — 소스 생성기 테스트](./16-testing-library.md) |
| **스케줄 Job 테스트** | [16-testing-library.md — 스케줄 Job 통합 테스트](./16-testing-library.md) |
| **모듈과 프로젝트 구조 매핑** | [04-ddd-tactical-overview.md — §6](./04-ddd-tactical-overview.md) |
| **네이밍 규칙/용어집** | [04-ddd-tactical-overview.md — §7](./04-ddd-tactical-overview.md) |
| **Bounded Context/Context Map** | [04-ddd-tactical-overview.md — §8](./04-ddd-tactical-overview.md) |
| **DTO 전략 리뷰 확인** | [dto-strategy-review.md](../../.claude/dto-strategy-review.md) |
| **CI/CD 워크플로우** | [02b-ci-cd.md](./02b-ci-cd.md) |
| **버전 관리 (MinVer)** | [02c-versioning.md](./02c-versioning.md) |
| **FinResponse 타입 진화** | [A03-response-type-evolution.md](./A03-response-type-evolution.md) |
| **통합 테스트 (HostTestFixture)** | [15b-integration-testing.md](./15b-integration-testing.md) |
| **VSCode 디버깅/개발 환경 설정** | [A01-vscode-debugging.md](./A01-vscode-debugging.md) |
| **Git 명령어/Hooks** | [A02-git-reference.md](./A02-git-reference.md) |
| **문서 작성 가이드** | [00-writing-guide.md](./00-writing-guide.md) |

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
| 04 | [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) | DDD 전술적 설계 개요, 빌딩블록 맵, Functorium 타입 매핑 |
| 05a | [05a-value-objects.md](./05a-value-objects.md) | 값 객체 (핵심 개념, 기반 클래스, 검증 시스템, 구현 패턴) |
| 05b | [05b-value-objects-validation.md](./05b-value-objects-validation.md) | 값 객체 (열거형 패턴, 실전 예제, Application 검증, FAQ) |
| 06a | [06a-aggregate-design.md](./06a-aggregate-design.md) | Aggregate 설계 (WHY + WHAT: 설계 규칙, 경계 설정, 안티패턴) |
| 06b | [06b-entity-aggregate-implementation.md](./06b-entity-aggregate-implementation.md) | Entity/Aggregate 구현 (HOW: 클래스 계층, ID, 생성 패턴, 이벤트) |
| 07 | [07-domain-events.md](./07-domain-events.md) | 도메인 이벤트 정의, 발행, 핸들러 구현 |
| 08a | [08a-error-system.md](./08a-error-system.md) | 에러 시스템: 기초와 네이밍 (WHY, Fin 패턴, 네이밍 규칙) |
| 08b | [08b-error-system-layers.md](./08b-error-system-layers.md) | 에러 시스템: 레이어별 구현과 테스트 (Domain/Application/Adapter 에러, 체크리스트) |
| 09 | [09-domain-services.md](./09-domain-services.md) | 도메인 서비스 (교차 Aggregate 순수 로직, IDomainService) |
| 10 | [10-specifications.md](./10-specifications.md) | Specification 패턴 (비즈니스 규칙 캡슐화, 조합, Repository 통합) |
| 11 | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) | Use Case 구현 (CQRS Command/Query) |
| 11b | [A03-response-type-evolution.md](./A03-response-type-evolution.md) | FinResponse 타입 진화 기록 |
| 12 | [12-ports.md](./12-ports.md) | Port 아키텍처, IObservablePort 계층, Port 정의 규칙 |
| 13 | [13-adapters.md](./13-adapters.md) | Adapter 구현 (Repository, External API, Messaging, Query) |
| 14 | [14-adapter-wiring.md](./14-adapter-wiring.md) | Pipeline 생성, DI 등록, Options 패턴, 테스트 |
| 17 | [17-dto-strategy.md](./17-dto-strategy.md) | DTO 전략 (레이어별 소유권, 재사용 규칙, 변환 패턴) |

### 아키텍처

| 문서 | 설명 |
|------|------|
| [01-project-structure.md](./01-project-structure.md) | 서비스 프로젝트 구성 (폴더, 네이밍, 의존성) |
| [02-solution-configuration.md](./02-solution-configuration.md) | 솔루션 루트 구성 파일 및 빌드 스크립트 |
| [02b-ci-cd.md](./02b-ci-cd.md) | CI/CD 워크플로우 (GitHub Actions, NuGet 패키지) |
| [02c-versioning.md](./02c-versioning.md) | 버전 관리 (MinVer, 버전 제안 커맨드) |
| [03-dotnet-tools.md](./03-dotnet-tools.md) | .NET 도구 가이드 (CLI 도구, 소스 생성기, 파일 기반 스크립트) |

### 테스트

| 문서 | 설명 |
|------|------|
| [15a-unit-testing.md](./15a-unit-testing.md) | 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정) |
| [15b-integration-testing.md](./15b-integration-testing.md) | 통합 테스트 (HostTestFixture, 환경 설정) |
| [16-testing-library.md](./16-testing-library.md) | Functorium.Testing 라이브러리 (로그/아키텍처/소스생성기/Job 테스트) |

### Observability

| 문서 | 설명 |
|------|------|
| [18-observability-spec.md](./18-observability-spec.md) | Observability 사양 (Field/Tag, Meter, 메시지 템플릿) |
| [19-observability-naming.md](./19-observability-naming.md) | Observability 네이밍 가이드 (코드, 필드, Logger 메서드) |
| [19-observability-logging.md](./19-observability-logging.md) | Observability 로깅 상세 |
| [20-observability-metrics.md](./20-observability-metrics.md) | Observability 메트릭 상세 |
| [21-observability-tracing.md](./21-observability-tracing.md) | Observability 트레이싱 상세 |

### 진단

| 문서 | 설명 |
|------|------|
| [22-crash-diagnostics.md](./22-crash-diagnostics.md) | 크래시 덤프 핸들러 설정 및 분석 가이드 |

### 개발 도구

| 문서 | 설명 |
|------|------|
| [A01-vscode-debugging.md](./A01-vscode-debugging.md) | VSCode 디버깅 및 개발 환경 설정 |
| [A02-git-reference.md](./A02-git-reference.md) | Git 명령어 참조 및 Git Hooks |

### 리뷰

| 문서 | 설명 |
|------|------|
| [dto-strategy-review.md](../../.claude/dto-strategy-review.md) | DTO 매핑 전략 리뷰 (DDD & Hexagonal Architecture 관점) |

### 기타

| 문서 | 설명 |
|------|------|
| [book-writing-guide.md](../../.claude/book-writing-guide.md) | 서적 집필 가이드 |
| [00-writing-guide.md](./00-writing-guide.md) | 문서 작성 가이드 (가이드 문서 작성 규칙) |
