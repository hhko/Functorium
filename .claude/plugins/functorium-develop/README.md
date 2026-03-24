# functorium-develop

Functorium 프레임워크 기반 DDD 개발 가이드 플러그인.

## 설치

### 로컬 설치 (프로젝트 전용)
```bash
claude plugins install .claude/plugins/functorium-develop
```

## 스킬

| 스킬 | 설명 | 트리거 예시 |
|------|------|------------|
| `domain-develop` | 도메인 레이어 개발 (VO, Aggregate, Spec) | "도메인 구현", "Aggregate 만들어줘" |
| `application-develop` | 애플리케이션 레이어 (CQRS, Usecase, Port) | "유스케이스 구현", "Command 만들어줘" |
| `adapter-develop` | 어댑터 레이어 (Repository, Endpoint, DI) | "Repository 구현", "엔드포인트 만들어줘", "CtxEnricher 설정" |
| `test-develop` | 테스트 전략 (단위, 통합, 아키텍처 규칙) | "테스트 작성", "통합 테스트", "ctx 스냅샷 테스트" |
| `domain-review` | DDD 코드 리뷰 | "DDD 리뷰", "아키텍처 리뷰" |

## 에이전트

| 에이전트 | 전문 영역 |
|---------|-----------|
| `domain-architect` | 유비쿼터스 언어, Aggregate 경계, 타입 전략 |
| `application-architect` | CQRS 설계, 포트 식별, FinT 합성, CtxEnricher 3-Pillar 설계 |
| `adapter-engineer` | Repository, Endpoint, DI 등록, CtxEnricherPipeline 통합 |
| `test-engineer` | 단위/통합/아키텍처 테스트, ctx 3-Pillar 스냅샷 테스트 |

## 워크플로우

각 스킬은 4단계 문서를 생성합니다:

1. **00-business-requirements.md** — 요구사항 + 유비쿼터스 언어
2. **01-type-design-decisions.md** — 불변식/전략 → 타입 매핑
3. **02-code-design.md** — 전략 → C#/Functorium 패턴
4. **03-implementation-results.md** — 코드 + 테스트 검증

## 사용 예시

```
# 도메인 개발
도메인 구현해줘. 상품(Product) Aggregate를 설계하고 싶어.

# 애플리케이션 개발
상품 생성 Command Usecase를 만들어줘.

# 어댑터 개발
상품 Repository를 EF Core로 구현해줘.

# 테스트 작성
상품 도메인 단위 테스트를 작성해줘.

# 코드 리뷰
현재 도메인 코드를 DDD 관점에서 리뷰해줘.
```
