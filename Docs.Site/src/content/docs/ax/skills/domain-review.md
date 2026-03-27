---
title: "Domain Review"
description: "에릭 에반스 DDD 관점의 코드 리뷰와 개선 제안"
---

> 워크플로 어느 시점에서든 실행 가능한 리뷰 스킬입니다.

## 선행 조건

- 리뷰 대상 소스 코드가 존재해야 합니다.
- 특정 Aggregate, 레이어, 또는 전체 코드베이스를 지정할 수 있습니다.
- 선행 문서가 없어도 기존 코드에서 직접 리뷰를 수행합니다.

## 배경

DDD 전술적 설계 원칙을 일관되게 적용하는 것은 어렵습니다. Aggregate 경계가 모호해지고, 도메인 이벤트의 소유권이 흐려지고, Value Object 대신 원시 타입이 사용되고, Application 레이어에 비즈니스 로직이 침투하는 문제들은 코드 리뷰에서 반복적으로 발견됩니다.

`/domain-review` 스킬은 에릭 에반스의 DDD 관점에서 기존 코드를 체계적으로 리뷰합니다. Aggregate 경계, 이벤트 소유권, Value Object 활용, 레이어 침범, Functorium 패턴 준수를 체크리스트 기반으로 검토하고, 구체적인 개선 방향을 제시합니다.

## 스킬 개요

### 리뷰 체크리스트

| 카테고리 | 검토 항목 | 설명 |
|---------|----------|------|
| Aggregate 경계 | 트랜잭션 일관성 | 하나의 트랜잭션에서 하나의 Aggregate만 수정하는가 |
| Aggregate 경계 | ID 참조 | Aggregate 간 참조는 ID로만 하는가 (직접 참조 금지) |
| Aggregate 경계 | 불변식 범위 | Aggregate가 보호하는 불변식이 명확한가 |
| 이벤트 소유권 | 발행 위치 | 도메인 이벤트가 Aggregate 커맨드 메서드에서 발행되는가 |
| 이벤트 소유권 | 네이밍 | 이벤트가 과거형(~Event)으로 명명되는가 |
| Value Object | 원시 타입 강박 | 원시 타입(string, int, decimal) 대신 VO를 사용하는가 |
| Value Object | 검증 위치 | 검증 로직이 VO 내부에 캡슐화되는가 |
| 레이어 침범 | Domain → Adapter | 도메인이 인프라 기술에 의존하지 않는가 |
| 레이어 침범 | Application → Domain | Application에 비즈니스 로직이 없는가 |
| Functorium 패턴 | `Fin<T>` 반환 | 커맨드 메서드가 `Fin<Unit>` 또는 `Fin<T>`를 반환하는가 |
| Functorium 패턴 | sealed class | Aggregate, VO, Event가 `sealed`인가 |
| Functorium 패턴 | private 생성자 | VO와 Entity의 생성자가 `private`인가 |

## 사용 방법

### 기본 호출

```text
/domain-review Product Aggregate를 DDD 관점에서 리뷰해줘.
```

### 대화형 모드

인자 없이 `/domain-review`만 호출하면, 스킬이 리뷰 범위를 대화형으로 확인합니다.

### 출력 형식

리뷰 결과는 체크리스트 테이블 형태로 출력됩니다:

```text
| 항목 | 상태 | 위반 사항 | 개선 제안 |
|------|------|----------|----------|
| Aggregate 경계 | PASS | — | — |
| 이벤트 소유권 | WARN | ... | ... |
```

## 예제 1: 초급 — 단일 Aggregate 리뷰

가장 기본적인 리뷰입니다. 하나의 Aggregate Root를 대상으로 불변식, 커맨드 메서드, 이벤트 발행, Value Object 활용을 점검합니다.

### 프롬프트

```text
/domain-review Product Aggregate를 DDD 관점에서 리뷰해줘.
```

### 출력 예시

스킬은 다음과 같은 체크리스트 테이블을 출력합니다:

```text
## Product Aggregate 리뷰 결과

| 항목 | 상태 | 위반 사항 | 개선 제안 |
|------|------|----------|----------|
| sealed class | PASS | Product는 sealed | — |
| private 생성자 | PASS | private 생성자 사용 | — |
| Fin<Unit> 반환 | PASS | UpdateName, UpdatePrice 모두 Fin<Unit> 반환 | — |
| 이벤트 발행 | PASS | 모든 커맨드 메서드에서 AddDomainEvent 호출 | — |
| Value Object 활용 | WARN | Description이 string 타입 | ProductDescription VO 도입 권장 |
| 불변식 범위 | PASS | "가격은 양수" 규칙이 Money VO에 인코딩됨 | — |
| ID 참조 | PASS | Category는 CategoryId로만 참조 | — |

### 요약
- 전체 7개 항목 중 6개 PASS, 1개 WARN
- 주요 개선 사항: Description에 도메인 규칙이 있다면 VO로 승격 권장
```

### 리뷰 관점

스킬은 다음 질문들을 기반으로 Aggregate를 검토합니다:

- Aggregate가 보호하는 **불변식**이 무엇인가?
- 모든 상태 변경이 **커맨드 메서드**를 통해 이루어지는가?
- 커맨드 메서드가 **도메인 이벤트**를 발행하는가?
- **원시 타입** 대신 **Value Object**를 사용하는가?
- `Fin<Unit>` 또는 `Fin<T>` 반환으로 **실패를 명시적으로 표현**하는가?

## 예제 2: 중급 — 레이어 의존성 검토

Aggregate를 넘어 레이어 간 의존성을 검토합니다. 도메인 레이어가 인프라에 의존하는 코드, Application 레이어에 침투한 비즈니스 로직, 잘못된 `using` 선언 등을 식별합니다.

### 프롬프트

```text
/domain-review 도메인 레이어가 인프라에 의존하는 코드가 있는지 검토해줘.
```

### 출력 예시

```text
## 레이어 의존성 리뷰 결과

### Domain → Adapter 의존성 검사
| 파일 | 위반 using | 개선 제안 |
|------|-----------|----------|
| 위반 없음 | — | — |

### Application → Domain 로직 침투 검사
| 파일 | 위반 코드 | 개선 제안 |
|------|----------|----------|
| CreateOrderCommand.cs:45 | `if (order.TotalAmount > customer.CreditLimit)` | Domain Service로 추출: OrderCreditCheckService |

### 정리 대상 using
| 파일 | 불필요 using | 이유 |
|------|-------------|------|
| 위반 없음 | — | — |

### 요약
- Domain → Adapter: 위반 없음
- Application → Domain 로직 침투: 1건 (신용 한도 검증 → Domain Service 추출 권장)
```

### 리뷰 관점

스킬은 다음 의존성 규칙을 검증합니다:

- **Domain** → Application, Adapter 의존 금지
- **Application** → Adapter 의존 금지
- **Application**에 비즈니스 로직(조건 분기, 계산) 금지 → Domain Service 또는 Aggregate로 이동
- **Adapter** 간 상호 의존 금지 (Presentation ↔ Persistence ↔ Infrastructure)

## 예제 3: 고급 — 전체 바운디드 컨텍스트 아키텍처 리뷰

전체 코드베이스를 대상으로 종합 아키텍처 리뷰를 수행합니다. Aggregate 경계 재설계, Domain Service 추출 후보, Value Object 승격 대상, 이벤트 흐름 일관성을 종합적으로 분석합니다.

### 프롬프트

```text
/domain-review 전체 코드를 에릭 에반스 DDD 관점에서 아키텍처 리뷰해줘.
```

### 출력 예시

```text
## 전체 아키텍처 리뷰 결과

### 1. 레이어별 위반 요약

| 레이어 | PASS | WARN | FAIL | 주요 이슈 |
|--------|------|------|------|----------|
| Domain | 12 | 2 | 0 | Description 원시 타입 2건 |
| Application | 8 | 1 | 0 | 신용 검증 로직 Application 침투 |
| Adapter | 6 | 0 | 0 | — |

### 2. Aggregate 경계 분석

| Aggregate | 불변식 | 이벤트 | 상태 | 개선 제안 |
|-----------|--------|--------|------|----------|
| Product | 3개 (이름, 가격, 삭제) | 4개 | PASS | — |
| Order | 4개 (라인, 상태, 금액, 주소) | 2개 | PASS | — |
| Customer | 2개 (이름, 이메일) | 1개 | WARN | CreditLimit을 VO로 승격 권장 |
| Inventory | 2개 (수량, 임계값) | 2개 | PASS | — |

### 3. Domain Service 추출 후보

| 현재 위치 | 로직 | 추출 대상 | 이유 |
|----------|------|----------|------|
| CreateOrderCommand | 신용 한도 검증 | OrderCreditCheckService | 교차 Aggregate 로직 |

### 4. Value Object 승격 후보

| 현재 타입 | 사용 위치 | 도메인 규칙 | 제안 VO |
|----------|----------|-----------|---------|
| string Description | Product, Order | 최대 500자, 비어있으면 안됨 | Description VO |
| decimal CreditLimit | Customer | 양수, 비교 연산 사용 | CreditLimit VO |

### 5. 이벤트 흐름 일관성

| Aggregate | 커맨드 메서드 | 이벤트 발행 | 상태 |
|-----------|-------------|-----------|------|
| Product.Create | CreatedEvent | PASS | — |
| Product.UpdateName | NameUpdatedEvent | PASS | — |
| Product.Delete | DeletedEvent | PASS | — |
| Order.Create | CreatedEvent | PASS | — |
| Order.Confirm | ConfirmedEvent | PASS | — |

### 6. 종합 의견
- 전체적으로 DDD 전술적 설계 원칙을 잘 준수하고 있음
- 원시 타입 강박(Primitive Obsession) 2건 — VO 승격 권장
- Application 레이어 로직 침투 1건 — Domain Service 추출 권장
- Aggregate 경계와 이벤트 소유권은 일관성 있음
```

### 리뷰 관점

전체 리뷰는 다음 6가지 축으로 분석합니다:

1. **레이어 의존성** — 각 레이어의 의존 방향이 올바른가
2. **Aggregate 경계** — 트랜잭션 일관성, ID 참조, 불변식 범위
3. **Domain Service 필요성** — 교차 Aggregate 로직이 올바른 위치에 있는가
4. **Value Object 활용도** — 원시 타입 강박이 남아있는 곳
5. **이벤트 흐름** — 모든 상태 변경에 이벤트가 발행되는가
6. **Functorium 패턴 준수** — sealed, private 생성자, `Fin<T>` 반환

## 참고 자료

### 워크플로

- [워크플로](../workflow/) -- 6단계 전체 흐름
- [Test Develop 스킬](./test-develop/) -- 아키텍처 규칙 테스트로 리뷰 항목 자동화

### 프레임워크 가이드

- [DDD 전술적 설계 개요](../guides/domain/04-ddd-tactical-overview/)
- [값 객체](../guides/domain/05a-value-objects/)
- [Aggregate 설계](../guides/domain/06a-aggregate-design/)
- [Entity/Aggregate 핵심](../guides/domain/06b-entity-aggregate-core/)
- [도메인 이벤트](../guides/domain/07-domain-events/)
- [에러 시스템](../guides/domain/08a-error-system/)
- [Specification](../guides/domain/10-specifications/)
- [도메인 서비스](../guides/domain/09-domain-services/)

### 관련 스킬

- [도메인 개발 스킬](./domain-develop/) -- 리뷰에서 발견된 개선 사항을 코드로 구현
- [Application 레이어 개발 스킬](./application-develop/) -- Domain Service 추출 후 Usecase 재구성
- [테스트 개발 스킬](./test-develop/) -- 아키텍처 규칙 테스트로 위반 자동 감지
