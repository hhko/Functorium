# 헥사고날 아키텍처 갭 분석 및 개선 계획

헥사고날 아키텍처(Ports and Adapters) 관점에서 현재 Functorium 가이드와 구현의 갭을 분석하고 개선 방향을 제시합니다.

## §0. 갭별 영향도 요약

| § | 갭 | 영향도 | 상태 |
|---|-----|--------|------|
| §1 | Cross-Layer 참조 규칙 매트릭스 | HIGH | ✅ 완료 |
| §2 | 아키텍처 테스트 커버리지 갭 | HIGH | ✅ 완료 |
| §3 | Adapter 간 교차 의존성 | HIGH | ✅ 완료 |
| §4 | Driving/Driven Adapter 구분 | MEDIUM | ✅ 완료 |
| §5 | 레이어-프로젝트 배치 의사결정 플로우차트 | MEDIUM | ✅ 완료 |
| §6 | Anti-Corruption Layer 완전성 | MEDIUM | ✅ 완료 |
| §7 | Host Composition Root 패턴 보강 | LOW | ✅ 완료 |

## §1. Cross-Layer 참조 규칙 매트릭스 — HIGH ✅ 완료

### 완료 현황

- [01-project-structure.md](./01-project-structure.md)에 6×6 참조 규칙 매트릭스 추가 완료
- Infrastructure → Presentation 전이 참조 위반 수정 (§3 참조)
- `LayerDependencyArchitectureRuleTests`에 누락 테스트 추가 (6개 테스트)

### 검증

```bash
dotnet test --project Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit \
  --filter "FullyQualifiedName~LayerDependencyArchitectureRuleTests"
```

### 참조 매트릭스

| From \ To | Domain | Application | Presentation | Persistence | Infrastructure | Host |
|-----------|--------|-------------|--------------|-------------|----------------|------|
| **Domain** | — | ✗ | ✗ | ✗ | ✗ | ✗ |
| **Application** | ✓ | — | ✗ | ✗ | ✗ | ✗ |
| **Presentation** | (전이) | ✓ | — | ✗ | ✗ | ✗ |
| **Persistence** | (전이) | ✓ | ✗ | — | ✗ | ✗ |
| **Infrastructure** | (전이) | ✓ | ✗ | ✗ | — | ✗ |
| **Host** | (전이) | ✓ | ✓ | ✓ | ✓ | — |

- **✓**: 직접 참조 허용
- **✗**: 참조 금지
- **(전이)**: 직접 참조 없음, 상위 참조를 통한 전이 참조

> **참고**: Contracts 프로젝트와 Tests 프로젝트는 이 매트릭스에서 제외합니다. Contracts는 Domain의 공개 계약이고, Tests는 SUT를 직접 참조합니다.

## §2. 아키텍처 테스트 커버리지 갭 — HIGH ✅ 완료

### 완료 현황

§1 작업에서 Cross-Layer 참조 규칙 매트릭스를 추가하면서, 누락된 아키텍처 테스트도 함께 구현 완료했습니다.

### 현재 테스트 목록

`LayerDependencyArchitectureRuleTests` — 6개 테스트:

| 테스트 메서드 | 검증 내용 | 상태 |
|--------------|----------|------|
| `DomainLayer_ShouldNotDependOn_ApplicationLayer` | Domain !→ Application | ✅ |
| `DomainLayer_ShouldNotDependOn_AdapterLayer` | Domain !→ Presentation, Persistence, Infrastructure | ✅ |
| `ApplicationLayer_ShouldNotDependOn_AdapterLayer` | Application !→ Presentation, Persistence, Infrastructure | ✅ |
| `PresentationAdapter_ShouldNotDependOn_OtherAdapters` | Presentation !→ Persistence, Infrastructure | ✅ |
| `PersistenceAdapter_ShouldNotDependOn_OtherAdapters` | Persistence !→ Presentation, Infrastructure | ✅ |
| `InfrastructureAdapter_ShouldNotDependOn_OtherAdapters` | Infrastructure !→ Presentation, Persistence | ✅ |

### 검증

```bash
dotnet test --project Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit \
  --filter "FullyQualifiedName~LayerDependencyArchitectureRuleTests"
```

## §3. Adapter 간 교차 의존성 — HIGH ✅ 완료

### 해결 내용

`LayeredArch.Adapters.Infrastructure.csproj`에서 `Presentation` 참조를 제거하고 `Application` 직접 참조로 변경했습니다.

```
변경 전: Infrastructure → Presentation → Application → Domain
변경 후: Infrastructure → Application → Domain
```

**변경 파일:** `LayeredArch.Adapters.Infrastructure.csproj`
- `Presentation` ProjectReference 제거
- `Application` ProjectReference 추가

**검증:** `InfrastructureAdapter_ShouldNotDependOn_OtherAdapters` 아키텍처 테스트로 재발 방지

### 결정: Adapter 간 상호 참조 금지

Adapter 간 참조는 금지합니다. 각 Adapter는 Application만 직접 참조하며, §1 매트릭스에 반영 완료.

## §4. Driving/Driven Adapter 구분 — MEDIUM ✅ 완료

### 완료 현황

Driving/Driven 용어를 가이드에 명시하고, Presentation Adapter에 Port가 없는 설계 결정의 근거를 문서화했습니다.

### 변경 파일

| 파일 | 변경 내용 |
|------|----------|
| [12-ports-and-adapters.md](./12-ports-and-adapters.md) | "Driving vs Driven Adapter 구분" 섹션 추가, "Presentation Adapter에 Port가 없는 이유" 섹션 추가, Adapter 유형 표에 `헥사고날 역할` 컬럼 추가 |
| [01-project-structure.md](./01-project-structure.md) | 3분할 원칙 표에 `헥사고날 역할` 컬럼 추가 + 크로스 레퍼런스 |

### 문서화된 설계 결정

- **Driving/Driven 매핑**: Presentation = Driving, Persistence/Infrastructure = Driven
- **Presentation에 Port가 없는 이유**: Mediator가 Port 역할 대신, Command/Query가 계약, 불필요한 간접 계층 제거, Driven과의 비대칭은 의도적

## §5. 레이어-프로젝트 배치 의사결정 플로우차트 — MEDIUM ✅ 완료

### 완료 현황

"이 코드는 어느 프로젝트에 넣어야 하나?"에 답하는 3단계 의사결정 가이드를 [01-project-structure.md](./01-project-structure.md)에 통합했습니다.

### 변경 파일

| 파일 | 변경 내용 |
|------|----------|
| [01-project-structure.md](./01-project-structure.md) | "코드 배치 의사결정 가이드" 섹션 추가 (목차 + 본문) |

### 통합된 내용

- **Step 1. 레이어 결정** — 비즈니스 규칙 / 유스케이스 조율 / 기술적 구현 3분기
- **Step 2. 프로젝트 및 폴더 결정** — 코드 유형별 프로젝트·폴더 매핑 표 (16개 항목)
- **Step 3. Port 배치 판단** — 도메인 타입 시그니처 기준 Domain/Application 분기

## §6. Anti-Corruption Layer 완전성 — MEDIUM ✅ 완료

### 완료 현황

Database, External API 외 나머지 외부 시스템 유형(Message Broker, File System, Cache, 외부 인증/인가)에 대한 통합 ACL 가이드를 작성했습니다.

### 변경 파일

| 파일 | 변경 내용 |
|------|----------|
| [12-ports-and-adapters.md](./12-ports-and-adapters.md) | §2.2에 "외부 시스템 유형별 ACL 체크리스트" 서브섹션 추가 (공통 원칙, 유형별 매핑 표, 적용 판단 기준) |
| [12-ports-and-adapters.md](./12-ports-and-adapters.md) | §2.5에 "Messaging ACL: 메시지 스키마 변환이 필요한 경우" 노트 추가 |
| [12-ports-and-adapters.md](./12-ports-and-adapters.md) | "각 경계에서의 변환 책임" 표에 Application ↔ Messaging 행 추가 |

### 통합된 내용

- **ACL 공통 원칙** — Port는 도메인 타입만, Adapter 내부에 `internal` 모델/Mapper 정의
- **시스템 유형별 매핑 표** — 6개 유형(Database, External API, Message Broker, File System, Cache, 외부 인증/인가)별 Adapter 프로젝트, 내부 변환 타입, Mapper 패턴
- **ACL 적용 판단 기준** — 외부 스키마 독립 변경 가능 여부에 따른 ACL 필수/선택 분기

## §7. Host Composition Root 패턴 보강 — LOW ✅ 완료

### 완료 현황

- [01-project-structure.md](./01-project-structure.md)에 3개 서브섹션 추가 완료
- [12-ports-and-adapters.md](./12-ports-and-adapters.md) §4.5에 크로스 레퍼런스 추가

### 변경 파일

| 파일 | 변경 내용 |
|------|-----------|
| `01-project-structure.md` | 등록 순서 근거, 환경별 구성 분기, 미들웨어 파이프라인 확장 포인트 서브섹션 삽입 |
| `12-ports-and-adapters.md` | §4.5 핵심 포인트 뒤에 크로스 레퍼런스 추가 |

### 통합된 내용

- **등록 순서 근거**: 서비스 등록(Presentation → Persistence → Infrastructure)과 미들웨어(Infrastructure → Persistence → Presentation) 순서의 이유를 표로 정리
- **환경별 구성 분기**: appsettings 오버라이드, 코드 분기, Options 패턴 3가지 방법과 사용 원칙
- **미들웨어 파이프라인 확장 포인트**: 운영 요구사항 추가 시 삽입 위치(예외 처리 → 관찰성 → 보안 → 데이터 → Health Check → 엔드포인트)

## 참고 문서

- [01-project-structure.md](./01-project-structure.md) — 서비스 프로젝트 구성
- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [12-ports-and-adapters.md](./12-ports-and-adapters.md) — Port와 Adapter
- [14-testing-library.md](./14-testing-library.md) — 테스트 라이브러리 (아키텍처 규칙 검증)
- [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) — DDD 전술적 설계 갭 분석
