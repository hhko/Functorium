# 아키텍처 개선 계획

가이드 전반에 걸쳐 아키텍처 관련으로 개선이 필요한 갭을 분석한 결과입니다.

## 갭별 영향도 요약

| 갭 | 영향도 | 설명 |
|----|--------|------|
| §1. 레이어-프로젝트 매핑 의사결정 트리 부재 | **높음** | 새 코드 배치 시 매번 판단 필요, 팀 전체에 영향 |
| §2. Port 배치 전략 혼재 | **높음** | 경계 사례에서 잘못된 배치 유발, 의존성 방향 위반 위험 |
| §3. Host Composition Root 패턴 가이드 부족 | **중간** | 환경별 구성 누락 가능, 운영 단계에서 영향 |
| §4. Cross-Layer 참조 규칙 명시 부재 | **높음** | 의존성 위반 시 감지 어려움, 아키텍처 부식 위험 |
| §5. Adapter 하위 프로젝트 의존성 규칙 | **중간** | Adapter 간 결합 발생 가능, 규모 커질수록 영향 증가 |

## §1. 레이어-프로젝트 매핑 의사결정 트리 부재

### 현재 상태

3-Layer(Domain/Application/Adapter) → 8-Project 매핑 정보가 여러 문서에 흩어져 있습니다.

| 문서 | 내용 | 분량 |
|------|------|------|
| [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) §5 (L324~) | 레이어별 빌딩블록 배치 | 간략 (~30줄) |
| [01-project-structure.md](./01-project-structure.md) (L1~863) | 폴더 구조 중심 | 상세 (863줄) |

### 갭

"이 코드는 어느 프로젝트에 넣어야 하나?" 라는 질문에 답하는 **의사결정 플로우차트**가 없습니다.

특히 Adapter 3분할 기준이 서술적으로만 설명되어 있어, 새로운 구현체를 배치할 때 판단이 어렵습니다.

### 개선 방향

- "이 코드는 어느 프로젝트에?" 의사결정 플로우차트 작성
- Adapter 3분할 배치 기준 명확화:
  - **Presentation**: HTTP 입출력 (Endpoint, Request/Response DTO)
  - **Persistence**: 데이터 저장/조회 (Repository 구현, DbContext, Configuration)
  - **Infrastructure**: 횡단 관심사 (Mediator, Validator, OpenTelemetry, Pipeline) + 외부 API

## §2. Port 배치 전략 혼재

### 현재 상태

- [01-project-structure.md](./01-project-structure.md) L333~338: Domain Port 위치 결정 기준 (Aggregate 전용 vs 교차 Aggregate)
- [01-project-structure.md](./01-project-structure.md) L385~392: Domain Port vs Application Port 차이 테이블

### 갭

Domain Port와 Application Port의 **판단 기준**이 "도메인 타입만 사용하면 Domain, 외부 DTO나 기술적 관심사를 포함하면 Application" (L834)으로 요약되어 있으나, 경계 사례에 대한 가이드가 부족합니다.

### 개선 방향

- Repository Interface(Domain) vs Adapter Port(Application) 배치 의사결정 기준 강화
- 경계 사례 예시 추가:
  - 도메인 타입을 반환하지만 외부 시스템을 호출하는 Port → ?
  - 읽기 전용 조회이지만 인프라 의존성이 있는 Port → ?

## §3. Host Composition Root 패턴 가이드 부족

### 현재 상태

- [01-project-structure.md](./01-project-structure.md) L510~538: Host 프로젝트 역할과 Program.cs 등록 순서

### 갭

현재 Host 섹션에서 다루는 내용:
- 서비스 등록 순서 (Presentation → Persistence → Infrastructure)
- 미들웨어 순서 (Infrastructure → Persistence → Presentation)

다루지 않는 내용:
- 환경별 구성 분기 (Development/Staging/Production)
- Health Check 등록 패턴
- 미들웨어 파이프라인 구성 상세 (예외 처리, CORS, 인증/인가)
- 로깅/Observability 초기화 순서

### 개선 방향

- Host 프로젝트 패턴 보강: 환경별 구성, 미들웨어 파이프라인 등
- 서비스 등록 순서의 **이유** 설명 추가

## §4. Cross-Layer 참조 규칙 명시 부재

### 현재 상태

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) L347~354: 의존성 규칙 (안쪽 → 바깥 참조 금지 원칙)
- [01-project-structure.md](./01-project-structure.md) L71~104: 프로젝트 의존성 방향 다이어그램 + csproj 참조 예시

### 갭

"안쪽 레이어는 바깥 레이어를 절대 참조하지 않습니다"라는 원칙만 서술되어 있으며, 구체적인 **허용/금지 참조 매트릭스**가 없습니다.

### 개선 방향

- 8개 프로젝트 간 허용/금지 참조 매트릭스 작성
- ArchUnit 테스트 규칙과 연계 (현재 [14-testing-library.md](./14-testing-library.md)에 아키텍처 규칙 검증 존재)
- 위반 시나리오 예시 추가

## §5. Adapter 하위 프로젝트 의존성 규칙

### 현재 상태

- [01-project-structure.md](./01-project-structure.md) L394~508: Adapter 레이어 3분할 구조
- [01-project-structure.md](./01-project-structure.md) L71~104: 의존성 다이어그램에서 3개 Adapter → Application 참조만 표시

### 갭

Presentation / Persistence / Infrastructure 간 **상호 참조 규칙**이 암묵적입니다. 다이어그램에서 3개 Adapter가 모두 Application만 참조하는 것으로 표시되어 있으나, Adapter 간 상호 참조가 허용되는지/금지되는지 명시적으로 서술되지 않았습니다.

### 개선 방향

- Adapter 하위 프로젝트 간 의존성 방향 명시
  - Presentation ↛ Persistence (금지)
  - Persistence ↛ Presentation (금지)
  - Infrastructure ↔ 다른 Adapter (규칙 정의 필요)
- 위반 시 발생하는 문제점 설명

## 참고 문서

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [12-ports-and-adapters.md](./12-ports-and-adapters.md) — Port와 Adapter
- [01-project-structure.md](./01-project-structure.md) — 서비스 프로젝트 구성
- [02-solution-configuration.md](./02-solution-configuration.md) — 솔루션 구성
- [14-testing-library.md](./14-testing-library.md) — 테스트 라이브러리 (아키텍처 규칙 검증)
- [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) — DDD 전술적 설계 갭 분석
