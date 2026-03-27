# functorium-develop

Functorium 프레임워크 기반 DDD 개발 가이드 플러그인.

## 설치

### 로컬 설치 (프로젝트 전용)
```bash
claude plugins install .claude/plugins/functorium-develop
```

## 워크플로우

PRD 작성부터 테스트까지 6단계로 이어지는 개발 워크플로입니다:

```
project-spec → architecture-design → domain-develop → application-develop → adapter-develop → test-develop
```

| 단계 | 스킬 | 출력 |
|------|------|------|
| 1. 요구사항 명세 | `project-spec` | `00-project-spec.md` |
| 2. 아키텍처 설계 | `architecture-design` | `01-architecture-design.md` |
| 3. 도메인 개발 | `domain-develop` | `domain/00~03` + 소스 코드 |
| 4. 애플리케이션 개발 | `application-develop` | `application/00~03` + 소스 코드 |
| 5. 어댑터 개발 | `adapter-develop` | `adapter/00~03` + 소스 코드 |
| 6. 테스트 작성 | `test-develop` | 테스트 코드 |

각 스킬은 이전 스킬의 출력을 입력으로 사용합니다.
어느 단계에서든 시작할 수 있으며, 선행 문서가 없으면 사용자에게 직접 질문합니다.

## 스킬

| 스킬 | 설명 | 트리거 예시 |
|------|------|------------|
| `project-spec` | 프로젝트 요구사항 명세 (PRD) | "PRD 작성", "프로젝트 기획", "요구사항 정의" |
| `architecture-design` | 아키텍처 설계 (구조, 네이밍, 인프라) | "아키텍처 설계", "프로젝트 구조", "솔루션 구성" |
| `domain-develop` | 도메인 레이어 개발 (VO, Aggregate, Spec) | "도메인 구현", "Aggregate 만들어줘" |
| `application-develop` | 애플리케이션 레이어 (CQRS, Usecase, Port) | "유스케이스 구현", "Command 만들어줘" |
| `adapter-develop` | 어댑터 레이어 (Repository, Endpoint, DI) | "Repository 구현", "엔드포인트 만들어줘" |
| `test-develop` | 테스트 전략 (단위, 통합, 아키텍처 규칙) | "테스트 작성", "통합 테스트" |
| `domain-review` | DDD 코드 리뷰 | "DDD 리뷰", "아키텍처 리뷰" |

## 에이전트

| 에이전트 | 전문 영역 |
|---------|-----------|
| `domain-architect` | 유비쿼터스 언어, Aggregate 경계, 타입 전략 |
| `application-architect` | CQRS 설계, 포트 식별, FinT 합성, CtxEnricher 3-Pillar 설계 |
| `adapter-engineer` | Repository, Endpoint, DI 등록, CtxEnricherPipeline 통합 |
| `test-engineer` | 단위/통합/아키텍처 테스트, ctx 3-Pillar 스냅샷 테스트 |

## 레이어별 4단계 문서

각 레이어 스킬(domain/application/adapter)은 4단계 문서를 생성합니다:

1. **00-business-requirements.md** — 요구사항 + 유비쿼터스 언어
2. **01-type-design-decisions.md** — 불변식/전략 → 타입 매핑
3. **02-code-design.md** — 전략 → C#/Functorium 패턴
4. **03-implementation-results.md** — 코드 + 테스트 검증

## 사용 예시

```
# 1. 프로젝트 시작 (PRD)
PRD 작성해줘. AI 모델 거버넌스 플랫폼을 만들고 싶어.

# 2. 아키텍처 설계
프로젝트 구조를 설계해줘.

# 3. 도메인 개발
AIModel Aggregate를 설계하고 구현해줘.

# 4. 애플리케이션 개발
모델 등록 Command Usecase를 만들어줘.

# 5. 어댑터 개발
AIModel Repository를 EF Core로 구현해줘.

# 6. 테스트 작성
AIModel 도메인 단위 테스트를 작성해줘.

# 코드 리뷰
현재 도메인 코드를 DDD 관점에서 리뷰해줘.
```
