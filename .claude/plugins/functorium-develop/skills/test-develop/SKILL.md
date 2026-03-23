---
name: test-develop
description: "Functorium 프레임워크 기반 단위 테스트, 통합 테스트, 아키텍처 규칙 테스트를 작성합니다. '테스트 작성', '단위 테스트 추가', '통합 테스트', '아키텍처 규칙 테스트' 등의 요청에 반응합니다."
---

# Test Develop Skill

Functorium 프레임워크 기반 테스트를 작성하는 스킬입니다.

## 워크플로우

### Phase 1: 테스트 전략 결정

사용자에게 질문:
- 어떤 레이어의 코드를 테스트할까요? (Domain, Application, Adapter)
- 단위 테스트? 통합 테스트? 아키텍처 규칙?

### Phase 2: 단위 테스트

**도메인 레이어:**
- Value Object: Create 성공/실패, 에러 코드 검증, Normalize 검증
- AggregateRoot: 상태 변경 + DomainEvent 발행 검증, 멱등성
- Specification: 조건 매칭 검증
- DomainService: 교차 Aggregate 규칙 검증

**애플리케이션 레이어:**
- Command Usecase: NSubstitute mock, `FinTFactory.Succ`/`FinTFactory.Fail`
- Query Usecase: 동일 패턴
- Validator: 검증 성공/실패 케이스

상세 패턴은 references 파일을 읽으세요.

### Phase 3: 통합 테스트

- `HostTestFixture<Program>` 기반
- `HttpClient.PostAsJsonAsync` → StatusCode 검증
- 엔드 투 엔드 시나리오

### Phase 4: 아키텍처 규칙

- ArchUnitNET 기반 규칙
- `ClassValidator`, `InterfaceValidator`
- 레이어 의존성 방향 검증

## 핵심 규칙

- **네이밍:** `T1_T2_T3` (Method_Expected_Scenario)
- **변수:** `sut` (System Under Test), `actual` (결과)
- **어설션:** Shouldly (`.ShouldBe`, `.ShouldBeTrue`, `.ShouldContain`)
- **Mock:** NSubstitute (`Substitute.For<T>`)
- **FinT 반환값:** `FinTFactory.Succ(value)`, `FinTFactory.Fail<T>(error)`
- **실행:** `dotnet test --solution` (MTP)

## References

- 단위 테스트: `references/unit-test-patterns.md`
- 통합 테스트: `references/integration-patterns.md`
- 아키텍처 규칙: `references/architecture-rules.md`
