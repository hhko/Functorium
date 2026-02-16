# 헥사고날 아키텍처 갭 분석 및 개선 계획

헥사고날 아키텍처(Ports and Adapters) 관점에서 현재 Functorium 가이드와 구현의 갭을 분석하고 개선 방향을 제시합니다.

## §0. 갭별 영향도 요약

| § | 갭 | 영향도 | 상태 |
|---|-----|--------|------|
| §1 | Cross-Layer 참조 규칙 매트릭스 | HIGH | 미완 |
| §2 | 아키텍처 테스트 커버리지 갭 | HIGH | 미완 |
| §3 | Adapter 간 교차 의존성 | HIGH | 미완 |
| §4 | Driving/Driven Adapter 구분 | MEDIUM | 미완 |
| §5 | 레이어-프로젝트 배치 의사결정 플로우차트 | MEDIUM | 미완 |
| §6 | Anti-Corruption Layer 완전성 | MEDIUM | 미완 |
| §7 | Host Composition Root 패턴 보강 | LOW | 미완 |

## §1. Cross-Layer 참조 규칙 매트릭스 — HIGH

### 현재 상태

- [01-project-structure.md](./01-project-structure.md) L71~104: 프로젝트 의존성 방향 다이어그램 + csproj 참조 예시
- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) L361~368: "안쪽 레이어는 바깥 레이어를 절대 참조하지 않습니다" 원칙

### 갭

원칙만 서술되어 있으며, 8개 프로젝트 간 구체적인 **허용/금지 참조 매트릭스**가 없습니다.

### 개선: 8×8 참조 매트릭스

| From \ To | Domain | Application | Presentation | Persistence | Infrastructure | Host |
|-----------|--------|-------------|--------------|-------------|----------------|------|
| **Domain** | — | ✗ | ✗ | ✗ | ✗ | ✗ |
| **Application** | ✓ | — | ✗ | ✗ | ✗ | ✗ |
| **Presentation** | (전이) | ✓ | — | ✗ | ✗ | ✗ |
| **Persistence** | (전이) | ✓ | ✗ | — | ✗ | ✗ |
| **Infrastructure** | (전이) | (전이) | ※ | ✗ | — | ✗ |
| **Host** | (전이) | ✓ | ✓ | ✓ | ✓ | — |

- **✓**: 직접 참조 허용
- **✗**: 참조 금지
- **(전이)**: 직접 참조 없음, 상위 참조를 통한 전이 참조
- **※**: Infrastructure → Presentation: 현재 존재하는 예외적 참조 (§3에서 상세 분석)

> **참고**: Contracts 프로젝트와 Tests 프로젝트는 이 매트릭스에서 제외합니다. Contracts는 Domain의 공개 계약이고, Tests는 SUT를 직접 참조합니다.

## §2. 아키텍처 테스트 커버리지 갭 — HIGH

### 현재 상태

`LayerDependencyArchitectureRuleTests`에서 검증 중인 규칙:

| 테스트 | 검증 내용 | 상태 |
|--------|----------|------|
| Domain !→ Application | Domain이 Application을 참조하지 않음 | ✅ 존재 |
| Domain !→ Persistence | Domain이 Persistence를 참조하지 않음 | ✅ 존재 |
| Domain !→ Infrastructure | Domain이 Infrastructure를 참조하지 않음 | ✅ 존재 |
| Application !→ Persistence | Application이 Persistence를 참조하지 않음 | ✅ 존재 |
| Application !→ Infrastructure | Application이 Infrastructure를 참조하지 않음 | ✅ 존재 |

### 갭: 누락된 테스트

| 누락 테스트 | 검증 내용 | 중요도 |
|-------------|----------|--------|
| Domain !→ Presentation | Domain이 Presentation을 참조하지 않음 | HIGH |
| Application !→ Presentation | Application이 Presentation을 참조하지 않음 | HIGH |
| Adapter 간 상호 참조 | Presentation ↛ Persistence, Persistence ↛ Presentation 등 | HIGH |

### 실증 근거

```
확인된 테스트 파일:
- ArchitectureTestBase.cs (L23-24)
  → PresentationNamespace: 정의됨, LayerDependencyArchitectureRuleTests에서 미사용
- LayerDependencyArchitectureRuleTests.cs
  → Presentation 관련 의존성 검증: 없음
  → Adapter 간 교차 참조 검증: 없음
```

### 개선 방향

누락된 테스트 케이스 추가가 필요합니다 (구현은 별도 작업):

1. Domain !→ Presentation
2. Application !→ Presentation
3. Presentation !→ Persistence
4. Persistence !→ Presentation
5. Infrastructure → Presentation 예외 문서화 또는 제거

## §3. Adapter 간 교차 의존성 — HIGH

### 현재 상태

[01-project-structure.md](./01-project-structure.md)의 의존성 다이어그램은 3개 Adapter가 모두 Application만 참조하는 것으로 표시합니다:

```
Presentation  Persistence  Infrastructure
       \         |         /
        \        |        /
         v       v       v
            Application
```

### 갭: 실제 구현과 불일치

`LayeredArch.Adapters.Infrastructure.csproj` (L18)에서 `LayeredArch.Adapters.Presentation.csproj`을 직접 참조하고 있습니다.

이로 인해:
- Infrastructure는 Application을 **직접** 참조하지 않고, Presentation을 통한 **전이** 참조로 `Application` 네임스페이스에 접근
- 코드에서 `LayeredArch.Application.AssemblyReference.Assembly`를 사용 중 (전이 참조 경유)
- 다이어그램과 실제 프로젝트 참조가 불일치

### 개선 방향

1. **규칙 정의**: Adapter 간 참조를 허용할지 금지할지 명시적 결정
2. **현재 참조 이유 문서화**: Infrastructure → Presentation 참조가 필요한 이유 기록
3. **대안 검토**: Infrastructure가 Application을 직접 참조하도록 변경 가능 여부 평가
4. **다이어그램 업데이트**: 결정된 규칙에 맞게 의존성 다이어그램 수정

## §4. Driving/Driven Adapter 구분 — MEDIUM

### 현재 상태

[12-ports-and-adapters.md](./12-ports-and-adapters.md)에서 "Hexagonal Architecture"를 언급하지만, **Driving/Driven** 용어를 명시적으로 사용하지 않습니다.

### 갭

3분할 Adapter 구조의 아키텍처적 근거가 헥사고날 용어로 명시되지 않았습니다.

### 매핑

| Adapter | 헥사고날 역할 | 방향 | Port 위치 |
|---------|-------------|------|-----------|
| Presentation | Driving (Primary) | Outside → Inside | 없음 (Mediator 직접 호출) |
| Persistence | Driven (Secondary) | Inside → Outside | Domain (`IRepository`) |
| Infrastructure | Driven (Secondary) | Inside → Outside | Application (`IAdapter`) |

### 개선 방향

- Driving/Driven 용어를 가이드에 명시하여 아키텍처적 근거 강화
- Presentation이 Port 없이 Mediator를 직접 호출하는 설계 결정의 이유 문서화

## §5. 레이어-프로젝트 배치 의사결정 플로우차트 — MEDIUM

### 현재 상태

"이 코드는 어느 프로젝트에 넣어야 하나?"에 답하는 정보가 여러 문서에 흩어져 있습니다:

| 문서 | 내용 |
|------|------|
| [01-project-structure.md](./01-project-structure.md) | 폴더 구조 중심 상세 설명 (863줄) |
| [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) §5 | 레이어별 빌딩블록 배치 (간략 ~30줄) |

### 갭

통합된 **의사결정 플로우차트**가 없어, 새로운 구현체를 배치할 때 매번 판단이 필요합니다. 특히 Adapter 3분할 기준이 서술적으로만 설명되어 있습니다.

### 개선 방향

의사결정 플로우차트 작성:

```
새 코드 작성
├─ 비즈니스 규칙인가? ─── Yes → Domain Layer
│  ├─ Value Object / Entity / Aggregate → Domain
│  ├─ 교차 Aggregate 순수 로직 → Domain Service
│  └─ Repository 인터페이스 → Domain Port
│
├─ 유스케이스 조율인가? ─── Yes → Application Layer
│  ├─ Command / Query → Application
│  ├─ Event Handler → Application
│  └─ 외부 시스템 Port 인터페이스 → Application Port
│
└─ 기술적 구현인가? ─── Yes → Adapter Layer
   ├─ HTTP 입출력 (Endpoint, Request/Response DTO) → Presentation
   ├─ 데이터 저장/조회 (Repository 구현, DbContext) → Persistence
   └─ 외부 API / 횡단 관심사 (Mediator, Validator) → Infrastructure
```

## §6. Anti-Corruption Layer 완전성 — MEDIUM

### 현재 상태

구현 완료된 ACL:

| 외부 시스템 | ACL 구현 | 위치 |
|-----------|---------|------|
| Database | Persistence Model (POCO) + Mapper | Persistence Adapter |
| External API | `ExternalPricingApiService` | Infrastructure Adapter |

### 갭

다른 유형의 외부 시스템 경계에 대한 ACL 체크리스트가 없습니다:

- Messaging (Message Broker 메시지 ↔ Domain Event 변환)
- File System (파일 형식 ↔ Domain 타입 변환)
- Cache (캐시 직렬화 ↔ Domain 타입 변환)
- 외부 인증/인가 시스템

### 개선 방향

- 외부 시스템 유형별 ACL 체크리스트 작성
- 새 외부 시스템 연동 시 ACL 구현 가이드

## §7. Host Composition Root 패턴 보강 — LOW

### 현재 상태

[01-project-structure.md](./01-project-structure.md) L530~544에서 Host 프로젝트의 역할과 `Program.cs` 등록 순서를 다룹니다:

- 서비스 등록 순서: Presentation → Persistence → Infrastructure → Application → Mediator → DomainEventPublishing
- 미들웨어 순서: Infrastructure → Persistence → Presentation

### 갭

- 등록 순서의 **이유** 미기술 (왜 Presentation이 먼저인지)
- 환경별 구성 분기 (Development/Staging/Production) 가이드 없음
- Health Check 등록 패턴 미기술
- 미들웨어 파이프라인 구성 상세 (예외 처리, CORS, 인증/인가)

### 개선 방향

- 서비스 등록 순서 근거 설명
- 환경별 구성 분기 가이드
- 운영 관련 미들웨어 구성 패턴

## 참고 문서

- [01-project-structure.md](./01-project-structure.md) — 서비스 프로젝트 구성
- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [12-ports-and-adapters.md](./12-ports-and-adapters.md) — Port와 Adapter
- [14-testing-library.md](./14-testing-library.md) — 테스트 라이브러리 (아키텍처 규칙 검증)
- [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) — DDD 전술적 설계 갭 분석
