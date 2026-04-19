---
name: domain-develop
description: "에릭 에반스 DDD 관점에서 Functorium 프레임워크 기반 도메인 레이어 코드, 단위 테스트, 문서를 생성합니다. 도메인 모델링, Value Object, Entity, Aggregate, Union 타입, Specification, Domain Service 구현이 필요할 때 사용합니다. '도메인 구현', 'Aggregate 만들어줘', 'Value Object 추가', '엔티티 설계', '도메인 모델링' 등의 요청에 반응합니다."
---

## 선행 조건

`project-spec` 스킬에서 생성한 `00-project-spec.md`가 있으면 읽어 Aggregate 후보와 비즈니스 규칙을 확인합니다.
`architecture-design` 스킬에서 생성한 `01-architecture-design.md`가 있으면 읽어 폴더 구조와 네이밍 규칙을 확인합니다.
없으면 사용자에게 직접 질문합니다.

## 후속 스킬

```
project-spec → architecture-design → **domain-develop** → application-develop → adapter-develop → test-develop
```

## 개요

Functorium 프레임워크 기반 도메인 레이어 개발 가이드입니다.
사용자와 대화를 통해 4단계 문서를 생성하고 구현합니다.

## 워크플로우

### Phase 1: 비즈니스 요구사항 수집

사용자에게 다음을 질문합니다:
- 바운디드 컨텍스트 이름은?
- 핵심 개념/엔티티는? (유비쿼터스 언어 정의)
- 각 개념의 비즈니스 규칙은?
- 금지 상태(Forbidden States)는?
- 정상 시나리오와 거부 시나리오는?

**출력:** `{context}/domain/00-business-requirements.md`

출력 문서에 포함할 내용:
- 유비쿼터스 언어 테이블 (한글 | 영문 | 정의)
- 비즈니스 규칙 (주제별 분류)
- 정상/거부 시나리오
- 금지 상태

### Phase 2: 타입 설계 결정

비즈니스 규칙의 불변식을 분류하여 Functorium 타입으로 매핑합니다:

| 불변식 유형 | Functorium 타입 |
|-------------|-----------------|
| 단일값 불변식 | `SimpleValueObject<T>` |
| 비교/연산 불변식 | `ComparableSimpleValueObject<T>` |
| 열거형 상태 | SmartEnum (`SimpleValueObject<string>` + `HashMap`) |
| 상태 전이 | SmartEnum + `AllowedTransitions` |
| 배타적 상태 조합 (케이스별 다른 데이터) | `UnionValueObject` + `[UnionType]` |
| 배타적 상태 전이 (타입 안전 전이) | `UnionValueObject<TSelf>` + `TransitionFrom` |
| 생명주기 관리 | `AggregateRoot<TId>` |
| 자식 엔티티 | `Entity<TId>` |
| 조건부 쿼리 | `ExpressionSpecification<T>` |
| 교차 Aggregate 규칙 | `IDomainService` |

상세 패턴은 `references/pattern-catalog.md`를 읽어 참고합니다.
타입 전략 매핑 상세는 `references/type-strategy.md`를 읽어 참고합니다.

**출력:** `{context}/domain/01-type-design-decisions.md`

### Phase 3: 코드 설계

타입 결정을 C#/Functorium 패턴으로 변환합니다:

- **VO**: private 생성자, `Create()` + `Validate()` 팩토리, `CreateFromValidated()`, `implicit operator`
- **Union VO**: `abstract partial record : UnionValueObject` + `[UnionType]` + `sealed record` 케이스들
- **Stateful Union VO**: `abstract partial record : UnionValueObject<TSelf>` + `TransitionFrom<TSource, TTarget>()`
- **AggregateRoot**: `[GenerateEntityId]`, 중첩 DomainEvent/DomainError record, `Create()` + `AddDomainEvent()`
- **Specification**: `ExpressionSpecification<T>`의 `ToExpression()` 구현
- **DomainService**: `IDomainService` 구현, `Fin<T>` 반환, 상태 비보유

**출력:** `{context}/domain/02-code-design.md`

### 관찰 가능성 어트리뷰트
Request/Response/DomainEvent 프로퍼티에 Ctx Enricher 어트리뷰트를 적용하여
Logging/Tracing/Metrics에 비즈니스 컨텍스트를 자동 전파합니다:
- `[CtxRoot]` — 필드를 ctx.{field} 루트 레벨로 승격
- `[CtxTarget(CtxPillar.All)]` — 모든 Pillar(Logging+Tracing+MetricsTag)에 전파
- `[CtxIgnore]` — 모든 Pillar에서 제외

### Phase 4: 구현

실제 .cs 파일 생성 + 단위 테스트:

- 각 VO에 대한 Create 성공/실패 테스트
- AggregateRoot에 대한 상태 변경 + 이벤트 발행 테스트
- Specification 테스트
- Shouldly 어설션, AAA 패턴

단위 테스트 규칙은 `Docs.Site/src/content/docs/guides/testing/15a-unit-testing.md`를 준수합니다.

**출력:** `{context}/domain/03-implementation-results.md` + 소스 코드

### Domain Service 벌크 패턴
여러 Aggregate를 조율하는 벌크 연산은 Domain Service에서 처리합니다:
- Domain Service가 각 Aggregate의 상태를 변경하고 개별 이벤트를 클리어
- 단일 벌크 이벤트를 생성하여 반환
- Use Case에서 `IDomainEventCollector.TrackEvent(bulkEvent)` 호출

IRepository 벌크 메서드:
- `CreateRange(IReadOnlyList<TAggregate>)` → `FinT<IO, int>`
- `GetByIds(IReadOnlyList<TId>)` → `FinT<IO, Seq<TAggregate>>`
- `UpdateRange(IReadOnlyList<TAggregate>)` → `FinT<IO, int>`
- `DeleteRange(IReadOnlyList<TId>)` → `FinT<IO, int>`

IRepository Specification 메서드:
- `Exists(Specification<TAggregate>)` → `FinT<IO, bool>`
- `Count(Specification<TAggregate>)` → `FinT<IO, int>`
- `DeleteBy(Specification<TAggregate>)` → `FinT<IO, int>`

## 핵심 원칙

- 도메인 이벤트는 반드시 Aggregate Root에서 발행 (`AddDomainEvent`)
- Value Object는 Always-valid (`Create` 팩토리 + `Fin<T>` 반환)
- 예외 대신 `Fin<T>` / `FinT<IO, T>` 사용
- 이벤트 이름은 과거형 (`CreatedEvent`, `UpdatedEvent`)
- EntityId는 Ulid 기반 (`[GenerateEntityId]`)
- DomainError 코드는 `DomainErrors.{Type}.{ErrorName}` 형식으로 자동 생성
- Custom 에러는 `sealed record : DomainErrorType.Custom`으로 정의
- `CreateFromValidated`는 ORM/Repository 복원용 (검증 없음)

## References

- 패턴 카탈로그: `references/pattern-catalog.md`를 읽으세요
- 타입 전략: `references/type-strategy.md`를 읽으세요
