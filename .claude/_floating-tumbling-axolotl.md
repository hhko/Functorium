# Plan: Factory 패턴 문서를 03-entities-and-aggregates.md로 재배치 및 보강

## Context

`ddd-tactical-improvements.md §8 (Factories)` 에 기술된 팩토리 패턴 가이드가 실질적으로 Entity/Aggregate 생성의 핵심 패턴임에도 별도 "개선 사항" 문서에 위치해 있음. 이 내용을 `03-entities-and-aggregates.md §8` 에 통합하여 Entity 구현 가이드의 완결성을 높이고, 참조 분산을 제거함.

## 현재 상태 분석

### `03-entities-and-aggregates.md §8` (현재)
- Create/CreateFromValidated 기본 설명 있음 (lines 1020-1088)
- 마지막에 `ddd-tactical-improvements.md §8`로의 참조 링크 존재 (line 1088)
- Apply 패턴, Port 조율 패턴, EFCore 통합 내용 **없음**

### `ddd-tactical-improvements.md §8` (이동 대상)
- 패턴 1: 정적 Create() 팩토리 메서드 → 이미 03 문서에 있으므로 **이동 불필요**
- 패턴 2: CreateFromValidated() ORM 복원 → 이미 03 문서에 있으므로 **이동 불필요**
- 패턴 3: Apply 패턴 (Usecase 내 병렬 검증) → **이동 대상**
- 패턴 4: 교차 Aggregate 조율 (Port 사용) → **이동 대상**
- Create vs CreateFromValidated 비교 테이블 → **이동 대상** (03 문서 보강)
- 적용 사례 4개 → **이동 대상**
- EFCore 통합에서의 팩토리 패턴 → **이동 대상**
- 설계 결정 가이드라인 테이블 → **이동 대상**

## 변경 내용

### 1. `03-entities-and-aggregates.md` — §8 보강

**현재 §8 구조:**
```
§8. Aggregate Root 구현 패턴
  - Value Object vs Entity의 역할 차이
  - Create / CreateFromValidated 패턴 (기본)
  - 팩토리 메서드를 통한 생성
  - > 참조: ddd-tactical-improvements.md §8
  - 불변식을 보호하는 커맨드 메서드
  - ...
```

**변경 후 §8 구조:**
```
§8. Aggregate Root 구현 패턴
  - Value Object vs Entity의 역할 차이
  - Create / CreateFromValidated 패턴 (기존 유지)
  - + Create vs CreateFromValidated 비교 테이블 (improvements에서 이동)
  - 팩토리 메서드를 통한 생성 (기존 유지)
  - + Apply 패턴: Usecase 내 병렬 VO 검증 (improvements에서 이동) ← NEW
  - + 교차 Aggregate 조율: Port 사용 (improvements에서 이동) ← NEW
  - + 적용 사례 (improvements에서 이동) ← NEW
  - + EFCore 통합에서의 팩토리 패턴 (improvements에서 이동) ← NEW
  - + 설계 결정 가이드라인 (improvements에서 이동) ← NEW
  - 불변식을 보호하는 커맨드 메서드 (기존 유지)
  - ...
```

**구체적 변경:**

(a) `Create / CreateFromValidated 패턴` 섹션 뒤에 **Create vs CreateFromValidated 비교 테이블** 추가:
```markdown
**Create vs CreateFromValidated 비교:**

| 항목 | `Create()` | `CreateFromValidated()` |
|------|-----------|------------------------|
| 용도 | 새 Aggregate 생성 | ORM/Repository 복원 |
| ID 생성 | `XxxId.New()` 자동 발급 | 외부에서 전달 |
| 검증 | VO가 이미 검증됨 | 검증 스킵 (DB 데이터 신뢰) |
| 이벤트 발행 | `AddDomainEvent()` 호출 | 이벤트 없음 |
| Audit 필드 | 자동 설정 (`DateTime.UtcNow`) | 외부에서 전달 |
```

(b) `팩토리 메서드를 통한 생성` 섹션 뒤, `불변식을 보호하는 커맨드 메서드` 섹션 앞에 **3개 섹션 삽입**:

- **Apply 패턴: Usecase 내 병렬 VO 검증** — `ddd-tactical-improvements.md` 패턴 3 내용
- **교차 Aggregate 조율: Usecase에서 Port 사용** — `ddd-tactical-improvements.md` 패턴 4 내용
- **팩토리 패턴 적용 사례** — 4개 사례 (Product, Order, ORM 복원, Customer)
- **EFCore 통합에서의 팩토리 패턴** — VO 매핑 전략, Value Converter, OwnsOne/Many, EntityId Converter
- **팩토리 패턴 설계 결정 가이드** — 시나리오별 권장 방식 테이블

(c) 기존 참조 링크 제거:
```
> **참조:** 팩토리 패턴의 전체 가이드(Apply 패턴, Port 조율, EFCore 통합 등)는 [ddd-tactical-improvements.md §8](...) 참조
```
→ 삭제

### 2. `ddd-tactical-improvements.md` — §8 축소

현재 §8의 상세 내용을 제거하고, `03-entities-and-aggregates.md`로의 참조로 대체:

```markdown
## 8. Factories ✅

### 구현 완료

Functorium은 별도의 Factory 클래스 대신 **정적 팩토리 메서드 패턴**을 채택합니다. 모든 Aggregate Root, Entity, Value Object에 일관된 생성 패턴이 구현되어 있습니다.

**참조:** [03-entities-and-aggregates.md §8](./03-entities-and-aggregates.md#8-aggregate-root-구현-패턴)

**DDD 원칙 충족:**
- **캡슐화**: `private` 생성자로 직접 인스턴스화 차단, 팩토리 메서드만 공개
- **불변식 보호**: `Create()`에서 검증된 VO만 수용, primitive 직접 전달 불가
- **재구성 분리**: `Create()` (새 생성) vs `CreateFromValidated()` (복원) 명확 구분
- **이벤트 일관성**: 새 생성 시에만 도메인 이벤트 발행, 복원 시 이벤트 없음
- **레이어 책임**: Aggregate는 자기 생성만 담당, 외부 조율은 Usecase 책임
```

### 3. `README.md` — 참조 업데이트

```
| **팩토리 패턴 (Create/CreateFromValidated)** | [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) §8 |
```
→
```
| **팩토리 패턴 (Create/CreateFromValidated)** | [03-entities-and-aggregates.md](./03-entities-and-aggregates.md) §8 |
```

## 변경 파일 요약

| # | 파일 | 변경 |
|---|------|------|
| 1 | `.claude/guides/03-entities-and-aggregates.md` | §8에 팩토리 패턴 상세 내용 통합 (Apply, Port 조율, 사례, EFCore, 가이드라인) |
| 2 | `.claude/guides/ddd-tactical-improvements.md` | §8을 참조 링크로 축소 |
| 3 | `.claude/guides/README.md` | 팩토리 패턴 참조 경로 업데이트 |

## Verification

- 변경 후 `03-entities-and-aggregates.md` 내에서 §8 팩토리 패턴 관련 내용이 자기 완결적인지 확인
- `ddd-tactical-improvements.md`에서 §8 참조 링크가 정확한지 확인
- `README.md` 참조 경로 정확성 확인
