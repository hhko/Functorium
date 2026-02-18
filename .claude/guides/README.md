# Functorium 가이드

이 폴더는 Functorium 프레임워크 사용을 위한 Claude Code 가이드 문서를 포함합니다.

## DDD 전술적 설계와 Functorium

Functorium은 DDD 전술적 설계 패턴과 함수형 프로그래밍을 결합한 프레임워크입니다. 가이드 문서는 DDD 빌딩블록 순서로 배치되어 있으며, 각 문서는 **WHY → WHAT → HOW** 구조를 따릅니다.

## 학습 로드맵

```
Architecture
├── [01] 01-project-structure.md ─── 프로젝트 구조
├── [02] 02-solution-configuration.md ─── 솔루션 구성
└── [03] 03-dotnet-tools.md ─── .NET 도구

[04] 04-ddd-tactical-overview.md ─── DDD 전술적 설계 개요
│
├── Domain Layer
│   ├── [05] 05-value-objects.md ─── 값 객체
│   │   └── [06] 06-entities-and-aggregates.md ─── Entity와 Aggregate
│   │       └── [07] 07-domain-events.md ─── 도메인 이벤트
│   ├── [08] 08-error-system.md ─── 에러 시스템 (Domain/Application/Adapter)
│   ├── [09] 09-domain-services.md ─── 도메인 서비스
│   └── [10] 10-specifications.md ─── Specification 패턴
│
├── Application Layer
│   └── [11] 11-usecases-and-cqrs.md ─── Use Case와 CQRS
│
├── Adapter Layer
│   └── [12] 12-ports-and-adapters.md ─── Port와 Adapter
│
├── DTO 전략
│   └── [15] 15-dto-strategy.md ─── DTO 전략
│
├── Testing
│   ├── [13] 13-unit-testing.md ─── 단위 테스트
│   └── [14] 14-testing-library.md ─── 테스트 라이브러리
│
├── Observability
│   ├── observability-spec.md ─── 사양
│   ├── observability-naming.md ─── 네이밍
│   ├── observability-logging.md ─── 로깅
│   ├── observability-metrics.md ─── 메트릭
│   └── observability-tracing.md ─── 트레이싱
│
├── 진단
│   └── crash-diagnostics.md ─── 크래시 덤프
│
└── 리뷰
    └── dto-strategy-review.md ─── DTO 매핑 전략 리뷰 (DDD & Hexagonal)
```

## 빠른 참조 (작업별 가이드 바로가기)

| 하고 싶은 작업 | 참조 문서 |
|---------------|----------|
| **프로젝트 구성/폴더 구조** | [01-project-structure.md](./01-project-structure.md) |
| **솔루션 구성 파일/빌드 스크립트** | [02-solution-configuration.md](./02-solution-configuration.md) |
| **도구 사용법 (커버리지/스냅샷/ER 다이어그램)** | [03-dotnet-tools.md](./03-dotnet-tools.md) |
| **값 객체 만들기** | [05-value-objects.md](./05-value-objects.md) |
| **Entity/Aggregate 만들기** | [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) |
| **Aggregate 경계 설계하기** | [06-entities-and-aggregates.md — Part 1: Aggregate 경계 설계](./06-entities-and-aggregates.md) |
| **도메인 이벤트 정의/발행** | [07-domain-events.md](./07-domain-events.md) |
| **Event Handler 만들기** | [07-domain-events.md — 5. Event Handler](./07-domain-events.md) |
| **도메인 서비스 만들기** | [09-domain-services.md](./09-domain-services.md) |
| **Specification 만들기** | [10-specifications.md](./10-specifications.md) |
| **에러 타입 정의하기** | [08-error-system.md](./08-error-system.md) |
| **에러 테스트 작성하기** | [08-error-system.md — 4~6. 레이어별 에러 테스트](./08-error-system.md) |
| **Usecase 만들기** | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) |
| **Adapter 만들기** | [12-ports-and-adapters.md](./12-ports-and-adapters.md) |
| **EF Core Repository 만들기** | [12-ports-and-adapters.md — 2.8 EF Core Repository](./12-ports-and-adapters.md) |
| **Persistence Model/Mapper 만들기** | [12-ports-and-adapters.md — 2.6 데이터 변환](./12-ports-and-adapters.md) |
| **Endpoint Response DTO 만들기** | [12-ports-and-adapters.md — 2.6 데이터 변환](./12-ports-and-adapters.md) |
| **DTO 전략/재사용 규칙** | [15-dto-strategy.md](./15-dto-strategy.md) |
| **Options 패턴 (OptionsConfigurator)** | [12-ports-and-adapters.md — 4.6 Options 패턴](./12-ports-and-adapters.md) |
| **검증 메서드 확인** | [05-value-objects.md — 검증 시스템](./05-value-objects.md) |
| **크래시 덤프 설정/분석** | [crash-diagnostics.md](./crash-diagnostics.md) |
| **Observability 사양** | [observability-spec.md](./observability-spec.md) |
| **구조화된 로그 테스트** | [14-testing-library.md — 구조화된 로그 테스트](./14-testing-library.md) |
| **아키텍처 규칙 검증** | [14-testing-library.md — 아키텍처 규칙 검증](./14-testing-library.md) |
| **소스 생성기 테스트** | [14-testing-library.md — 소스 생성기 테스트](./14-testing-library.md) |
| **스케줄 Job 테스트** | [14-testing-library.md — 스케줄 Job 통합 테스트](./14-testing-library.md) |
| **생성 패턴 (Create/CreateFromValidated)** | [06-entities-and-aggregates.md — 8. 생성 패턴](./06-entities-and-aggregates.md) |
| **모듈과 프로젝트 구조 매핑** | [04-ddd-tactical-overview.md — §6](./04-ddd-tactical-overview.md) |
| **네이밍 규칙/용어집** | [04-ddd-tactical-overview.md — §7](./04-ddd-tactical-overview.md) |
| **Bounded Context/Context Map** | [04-ddd-tactical-overview.md — §8](./04-ddd-tactical-overview.md) |
| **DTO 전략 리뷰 확인** | [dto-strategy-review.md](../dto-strategy-review.md) |

## 문서 전체 목록

### DDD 전술적 설계 (번호순 학습 경로)

| 번호 | 문서 | 설명 |
|------|------|------|
| 04 | [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) | DDD 전술적 설계 개요, 빌딩블록 맵, Functorium 타입 매핑 |
| 05 | [05-value-objects.md](./05-value-objects.md) | 값 객체 구현 (기반 클래스, 검증 시스템, 구현 패턴) |
| 06 | [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) | Entity/Aggregate 설계 원칙과 구현 |
| 07 | [07-domain-events.md](./07-domain-events.md) | 도메인 이벤트 정의, 발행, 핸들러 구현 |
| 08 | [08-error-system.md](./08-error-system.md) | 레이어별 에러 시스템 (정의, 네이밍, 테스트) |
| 09 | [09-domain-services.md](./09-domain-services.md) | 도메인 서비스 (교차 Aggregate 순수 로직, IDomainService) |
| 10 | [10-specifications.md](./10-specifications.md) | Specification 패턴 (비즈니스 규칙 캡슐화, 조합, Repository 통합) |
| 11 | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) | Use Case 구현 (CQRS Command/Query) |
| 12 | [12-ports-and-adapters.md](./12-ports-and-adapters.md) | Port 정의, Adapter 구현, Pipeline 자동 생성 |
| 15 | [15-dto-strategy.md](./15-dto-strategy.md) | DTO 전략 (레이어별 소유권, 재사용 규칙, 변환 패턴) |

### 아키텍처

| 문서 | 설명 |
|------|------|
| [01-project-structure.md](./01-project-structure.md) | 서비스 프로젝트 구성 (폴더, 네이밍, 의존성) |
| [02-solution-configuration.md](./02-solution-configuration.md) | 솔루션 루트 구성 파일 및 빌드 스크립트 |
| [03-dotnet-tools.md](./03-dotnet-tools.md) | .NET 도구 가이드 (CLI 도구, 소스 생성기, 파일 기반 스크립트) |

### 테스트

| 문서 | 설명 |
|------|------|
| [13-unit-testing.md](./13-unit-testing.md) | 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정) |
| [14-testing-library.md](./14-testing-library.md) | Functorium.Testing 라이브러리 (로그/아키텍처/소스생성기/Job 테스트) |

### Observability

| 문서 | 설명 |
|------|------|
| [observability-spec.md](./observability-spec.md) | Observability 사양 (Field/Tag, Meter, 메시지 템플릿) |
| [observability-naming.md](./observability-naming.md) | Observability 네이밍 가이드 (코드, 필드, Logger 메서드) |
| [observability-logging.md](./observability-logging.md) | Observability 로깅 상세 |
| [observability-metrics.md](./observability-metrics.md) | Observability 메트릭 상세 |
| [observability-tracing.md](./observability-tracing.md) | Observability 트레이싱 상세 |

### 리뷰

| 문서 | 설명 |
|------|------|
| [dto-strategy-review.md](../dto-strategy-review.md) | DTO 매핑 전략 리뷰 (DDD & Hexagonal Architecture 관점) |

### 기타

| 문서 | 설명 |
|------|------|
| [crash-diagnostics.md](./crash-diagnostics.md) | 크래시 덤프 핸들러 설정 및 분석 가이드 |
| [_book-writing-guide.md](./_book-writing-guide.md) | 서적 집필 가이드 |

