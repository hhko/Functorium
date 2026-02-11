# Functorium 가이드 문서

이 폴더는 Functorium 프레임워크 사용을 위한 Claude Code 가이드 문서를 포함합니다.

## 문서 목록

### 프로젝트 구성

| 문서 | 설명 |
|------|------|
| [service-project-structure-guide.md](./service-project-structure-guide.md) | 서비스 프로젝트 구성 (폴더, 네이밍, 의존성) |

### 도메인 레이어

| 문서 | 설명 |
|------|------|
| [domain-modeling-overview.md](./domain-modeling-overview.md) | 도메인 모델링 개요 |
| [valueobject-guide.md](./valueobject-guide.md) | 값 객체 구현 및 검증 패턴 |
| [entity-guide.md](./entity-guide.md) | Entity 및 Aggregate Root 구현 |

### 애플리케이션 레이어

| 문서 | 설명 |
|------|------|
| [usecase-implementation-guide.md](./usecase-implementation-guide.md) | 유스케이스 구현 (CQRS Command/Query) |

### 어댑터 레이어

| 문서 | 설명 |
|------|------|
| [adapter-guide.md](./adapter-guide.md) | Adapter 구현 가이드 (설계 원칙 + 단계별 활동) |

### 에러 시스템

| 문서 | 설명 |
|------|------|
| [error-guide.md](./error-guide.md) | 레이어별 에러 시스템 (정의, 네이밍) |

### Observability

| 문서 | 설명 |
|------|------|
| [observability-spec.md](./observability-spec.md) | Observability 사양 (Field/Tag, Meter, 메시지 템플릿) |
| [observability-field-naming-guide.md](./observability-field-naming-guide.md) | Observability 필드 이름 규칙 |
| [observability-naming-guide.md](./observability-naming-guide.md) | Observability 네이밍 가이드 |
| [logger-method-naming-guide.md](./logger-method-naming-guide.md) | Logger 메서드 네이밍 가이드 |
| [observability-logging.md](./observability-logging.md) | Observability 로깅 상세 |
| [observability-metrics.md](./observability-metrics.md) | Observability 메트릭 상세 |
| [observability-tracing.md](./observability-tracing.md) | Observability 트레이싱 상세 |

### 테스트

| 문서 | 설명 |
|------|------|
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정) |
| [error-testing-guide.md](./error-testing-guide.md) | 에러 테스트 패턴 |
| [testing-library-guide.md](./testing-library-guide.md) | Functorium.Testing 라이브러리 (로그/아키텍처/소스생성기/Job 테스트) |

### 기타

| 문서 | 설명 |
|------|------|
| [_book-writing-guide.md](./_book-writing-guide.md) | 서적 집필 가이드 |

## 문서 구조

```
service-project-structure-guide.md (프로젝트 구성)
│
├── Domain Layer
│   ├── domain-modeling-overview.md (개요)
│   ├── valueobject-guide.md (값 객체)
│   └── entity-guide.md (Entity/Aggregate)
│
├── Application Layer
│   ├── usecase-implementation-guide.md (Command/Query)
│   └── error-guide.md (에러 정의)
│
├── Adapter Layer
│   └── adapter-guide.md (설계 + 구현 활동)
│
├── Observability
│   ├── observability-spec.md (사양)
│   ├── observability-field-naming-guide.md (필드 이름 규칙)
│   ├── observability-naming-guide.md (네이밍 가이드)
│   ├── logger-method-naming-guide.md (Logger 메서드 네이밍)
│   ├── observability-logging.md (로깅)
│   ├── observability-metrics.md (메트릭)
│   └── observability-tracing.md (트레이싱)
│
├── Testing
│   ├── unit-testing-guide.md (단위 테스트)
│   ├── error-testing-guide.md (에러 테스트)
│   └── testing-library-guide.md (테스트 라이브러리)
│
└── 기타
    └── _book-writing-guide.md (서적 집필)
```

## 빠른 참조

- **값 객체 만들기**: [valueobject-guide.md](./valueobject-guide.md)
- **Entity 만들기**: [entity-guide.md](./entity-guide.md)
- **Usecase 만들기**: [usecase-implementation-guide.md](./usecase-implementation-guide.md)
- **Event Handler 만들기**: [usecase-implementation-guide.md#event-handler-구현](./usecase-implementation-guide.md#event-handler-구현)
- **Adapter 만들기**: [adapter-guide.md](./adapter-guide.md)
- **검증 메서드**: [valueobject-guide.md#validationrulest-시작점](./valueobject-guide.md#validationrulest-시작점)
- **에러 타입**: [error-guide.md](./error-guide.md)
- **에러 테스트**: [error-testing-guide.md](./error-testing-guide.md)
- **프로젝트 구성/폴더 구조**: [service-project-structure-guide.md](./service-project-structure-guide.md)
- **Observability 사양**: [observability-spec.md](./observability-spec.md)
- **구조화된 로그 테스트**: [testing-library-guide.md#구조화된-로그-테스트](./testing-library-guide.md#구조화된-로그-테스트)
- **아키텍처 규칙 검증**: [testing-library-guide.md#아키텍처-규칙-검증](./testing-library-guide.md#아키텍처-규칙-검증)
- **소스 생성기 테스트**: [testing-library-guide.md#소스-생성기-테스트](./testing-library-guide.md#소스-생성기-테스트)
- **스케줄 Job 테스트**: [testing-library-guide.md#스케줄-job-통합-테스트](./testing-library-guide.md#스케줄-job-통합-테스트)
