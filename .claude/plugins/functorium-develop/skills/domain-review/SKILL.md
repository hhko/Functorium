---
name: domain-review
description: "에릭 에반스 DDD 관점에서 기존 코드를 리뷰하고 개선 방향을 제시합니다. 'DDD 리뷰', '도메인 코드 검토', '아키텍처 리뷰', '코드 품질 검토' 등의 요청에 반응합니다."
---

# Domain Review Skill

에릭 에반스 DDD 관점에서 기존 코드를 리뷰하고 개선 방향을 제시하는 스킬입니다.

## 리뷰 체크리스트

### Aggregate 경계

- [ ] 하나의 트랜잭션에서 하나의 Aggregate만 변경하는가?
- [ ] Aggregate 경계가 일관성 경계와 일치하는가?
- [ ] 교차 Aggregate 규칙이 Domain Service에 있는가?

### 도메인 이벤트 소유권

- [ ] 이벤트가 Aggregate Root에서만 발행되는가?
- [ ] 인프라(Repository, Publisher)가 이벤트를 생성하지 않는가?
- [ ] 이벤트 이름이 과거형인가? (예: `CreatedEvent`, `UpdatedEvent`)

### Value Object

- [ ] 원시 타입 대신 Value Object를 사용하는가?
- [ ] Always-valid 패턴을 따르는가? (`Create` + `Fin<T>`)
- [ ] 불변성이 보장되는가? (`private set`)

### 레이어 침범

- [ ] Domain Layer가 Infrastructure에 의존하지 않는가?
- [ ] Application Layer가 도메인 로직을 포함하지 않는가?
- [ ] 예외 대신 `Fin<T>` / `FinT<IO, T>`를 사용하는가?

### Functorium 패턴 준수

- [ ] `[GenerateEntityId]` 사용하는가?
- [ ] DomainEvent가 중첩 sealed record인가?
- [ ] `DomainErrorType.Custom`이 중첩 sealed record인가?
- [ ] `IObservablePort`를 구현하는가? (어댑터 레이어)

## 출력 형식

리뷰 결과를 테이블로 제시:

| 항목 | 상태 | 위반 사항 | 개선 제안 |
|------|------|----------|----------|
| Aggregate 경계 | PASS/FAIL | ... | ... |
| 이벤트 소유권 | PASS/FAIL | ... | ... |
| Value Object | PASS/FAIL | ... | ... |
| 레이어 침범 | PASS/FAIL | ... | ... |
| Functorium 패턴 | PASS/FAIL | ... | ... |
