# Functorium 가이드

이 폴더는 Functorium 프레임워크 사용을 위한 Claude Code 가이드 문서를 포함합니다.

## DDD 전술적 설계와 Functorium

Functorium은 DDD 전술적 설계 패턴과 함수형 프로그래밍을 결합한 프레임워크입니다. 가이드 문서는 DDD 빌딩블록 순서로 배치되어 있으며, 각 문서는 **WHY → WHAT → HOW** 구조를 따릅니다.

## 학습 로드맵

```
[01] 01-ddd-tactical-overview.md ─── DDD 전술적 설계 개요
│
├── Domain Layer
│   ├── [02] 02-value-objects.md ─── 값 객체
│   │   └── [03] 03-entities-and-aggregates.md ─── Entity와 Aggregate
│   │       └── [04] 04-domain-events.md ─── 도메인 이벤트
│   ├── [05] 05-error-system.md ─── 에러 시스템 (Domain/Application/Adapter)
│   └── [06] 06-domain-services.md ─── 도메인 서비스
│
├── Application Layer
│   └── [07] 07-usecases-and-cqrs.md ─── Use Case와 CQRS
│
├── Adapter Layer
│   └── [08] 08-ports-and-adapters.md ─── Port와 Adapter
│
├── Cross-cutting
│   ├── [09] 09-unit-testing.md ─── 단위 테스트
│   ├── [10] 10-testing-library.md ─── 테스트 라이브러리
│   ├── [11] 11-project-structure.md ─── 프로젝트 구조
│   └── [12] 12-solution-configuration.md ─── 솔루션 구성
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
└── 개선
    └── ddd-tactical-improvements.md ─── DDD 전술적 설계 갭 분석/로드맵
```

## 빠른 참조 (작업별 가이드 바로가기)

| 하고 싶은 작업 | 참조 문서 |
|---------------|----------|
| **값 객체 만들기** | [02-value-objects.md](./02-value-objects.md) |
| **Entity/Aggregate 만들기** | [03-entities-and-aggregates.md](./03-entities-and-aggregates.md) |
| **Aggregate 경계 설계하기** | [03-entities-and-aggregates.md](./03-entities-and-aggregates.md) §Part 1 |
| **도메인 이벤트 정의/발행** | [04-domain-events.md](./04-domain-events.md) |
| **Event Handler 만들기** | [04-domain-events.md](./04-domain-events.md) §5 |
| **도메인 서비스 만들기** | [06-domain-services.md](./06-domain-services.md) |
| **에러 타입 정의하기** | [05-error-system.md](./05-error-system.md) |
| **에러 테스트 작성하기** | [05-error-system.md](./05-error-system.md) §4~6 테스트 |
| **Usecase 만들기** | [07-usecases-and-cqrs.md](./07-usecases-and-cqrs.md) |
| **Adapter 만들기** | [08-ports-and-adapters.md](./08-ports-and-adapters.md) |
| **검증 메서드 확인** | [02-value-objects.md](./02-value-objects.md) §검증 시스템 |
| **솔루션 구성 파일/빌드 스크립트** | [12-solution-configuration.md](./12-solution-configuration.md) |
| **프로젝트 구성/폴더 구조** | [11-project-structure.md](./11-project-structure.md) |
| **크래시 덤프 설정/분석** | [crash-diagnostics.md](./crash-diagnostics.md) |
| **Observability 사양** | [observability-spec.md](./observability-spec.md) |
| **구조화된 로그 테스트** | [10-testing-library.md](./10-testing-library.md) §구조화된 로그 테스트 |
| **아키텍처 규칙 검증** | [10-testing-library.md](./10-testing-library.md) §아키텍처 규칙 검증 |
| **소스 생성기 테스트** | [10-testing-library.md](./10-testing-library.md) §소스 생성기 테스트 |
| **스케줄 Job 테스트** | [10-testing-library.md](./10-testing-library.md) §스케줄 Job 통합 테스트 |
| **DDD 개선 사항/로드맵 확인** | [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) |

## 문서 전체 목록

### DDD 전술적 설계 (번호순 학습 경로)

| 번호 | 문서 | 설명 |
|------|------|------|
| 01 | [01-ddd-tactical-overview.md](./01-ddd-tactical-overview.md) | DDD 전술적 설계 개요, 빌딩블록 맵, Functorium 타입 매핑 |
| 02 | [02-value-objects.md](./02-value-objects.md) | 값 객체 구현 (기반 클래스, 검증 시스템, 구현 패턴) |
| 03 | [03-entities-and-aggregates.md](./03-entities-and-aggregates.md) | Entity/Aggregate 설계 원칙과 구현 |
| 04 | [04-domain-events.md](./04-domain-events.md) | 도메인 이벤트 정의, 발행, 핸들러 구현 |
| 05 | [05-error-system.md](./05-error-system.md) | 레이어별 에러 시스템 (정의, 네이밍, 테스트) |
| 06 | [06-domain-services.md](./06-domain-services.md) | 도메인 서비스 (교차 Aggregate 순수 로직, IDomainService) |
| 07 | [07-usecases-and-cqrs.md](./07-usecases-and-cqrs.md) | Use Case 구현 (CQRS Command/Query) |
| 08 | [08-ports-and-adapters.md](./08-ports-and-adapters.md) | Port 정의, Adapter 구현, Pipeline 자동 생성 |

### 테스트

| 문서 | 설명 |
|------|------|
| [09-unit-testing.md](./09-unit-testing.md) | 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정) |
| [10-testing-library.md](./10-testing-library.md) | Functorium.Testing 라이브러리 (로그/아키텍처/소스생성기/Job 테스트) |

### 프로젝트 구성

| 문서 | 설명 |
|------|------|
| [11-project-structure.md](./11-project-structure.md) | 서비스 프로젝트 구성 (폴더, 네이밍, 의존성) |
| [12-solution-configuration.md](./12-solution-configuration.md) | 솔루션 루트 구성 파일 및 빌드 스크립트 |

### Observability

| 문서 | 설명 |
|------|------|
| [observability-spec.md](./observability-spec.md) | Observability 사양 (Field/Tag, Meter, 메시지 템플릿) |
| [observability-naming.md](./observability-naming.md) | Observability 네이밍 가이드 (코드, 필드, Logger 메서드) |
| [observability-logging.md](./observability-logging.md) | Observability 로깅 상세 |
| [observability-metrics.md](./observability-metrics.md) | Observability 메트릭 상세 |
| [observability-tracing.md](./observability-tracing.md) | Observability 트레이싱 상세 |

### 개선 사항

| 문서 | 설명 |
|------|------|
| [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) | DDD 전술적 설계 갭 분석, 개선 방향, 로드맵 |

### 기타

| 문서 | 설명 |
|------|------|
| [crash-diagnostics.md](./crash-diagnostics.md) | 크래시 덤프 핸들러 설정 및 분석 가이드 |
| [_book-writing-guide.md](./_book-writing-guide.md) | 서적 집필 가이드 |

