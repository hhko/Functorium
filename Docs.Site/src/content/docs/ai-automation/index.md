---
title: "AI 자동화"
---

## functorium-develop 플러그인

functorium-develop는 Functorium 프레임워크의 전체 개발 흐름을 AI로 자동화하는 Claude Code 플러그인입니다.

**Domain → Application → Adapter → Testing** 4개 레이어를 사용자와의 대화(Q&A)를 통해
설계 문서를 생성하고, 코드를 구현합니다.

### 4단계 문서 생성 워크플로우

각 레이어 스킬은 동일한 4단계 문서를 생성합니다:

| 단계 | 문서 | 내용 |
|------|------|------|
| Phase 1 | `00-business-requirements.md` | 요구사항, 유비쿼터스 언어, 비즈니스 규칙 |
| Phase 2 | `01-type-design-decisions.md` | 불변식 분류, 타입 전략 매핑 |
| Phase 3 | `02-code-design.md` | 전략 → C#/Functorium 패턴 변환 |
| Phase 4 | `03-implementation-results.md` | 구현 코드 + 테스트 검증 |

### 스킬

| 스킬 | 레이어 | 트리거 예시 |
|------|--------|------------|
| [domain-develop](./skills/domain-develop/) | 도메인 | "도메인 구현", "Aggregate 만들어줘" |
| [application-develop](./skills/application-develop/) | 애플리케이션 | "유스케이스 구현", "Command 만들어줘" |
| [adapter-develop](./skills/adapter-develop/) | 어댑터 | "Repository 구현", "엔드포인트 만들어줘" |
| [test-develop](./skills/test-develop/) | 테스트 | "테스트 작성", "통합 테스트" |
| [domain-review](./skills/domain-review/) | 리뷰 | "DDD 리뷰", "아키텍처 리뷰" |

### 전문 에이전트

| 에이전트 | 전문 영역 |
|---------|-----------|
| domain-architect | 유비쿼터스 언어, Aggregate 경계, 타입 전략 |
| application-architect | CQRS 설계, 포트 식별, FinT 합성 |
| adapter-engineer | Repository, Endpoint, DI 등록 |
| test-engineer | 단위/통합/아키텍처 테스트 |

[에이전트 상세](./agents/)

### 빠른 시작

```text
# 도메인 개발 (대화형)
도메인 구현해줘. 상품(Product) Aggregate를 설계하고 싶어.

# 유스케이스 개발
상품 생성 Command Usecase를 만들어줘.

# 어댑터 구현
상품 Repository를 EF Core로 구현해줘.

# 테스트 작성
상품 도메인 단위 테스트를 작성해줘.

# 코드 리뷰
현재 도메인 코드를 DDD 관점에서 리뷰해줘.
```

### 설치

[설치 가이드](./installation/)를 참조하세요.
