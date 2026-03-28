---
name: observability-develop
description: "Functorium 프레임워크의 관측성 전략을 설계합니다. 비즈니스 KPI→기술 메트릭 매핑, 대시보드 설계, 알림 패턴, 분산 추적 분석, ctx.* 컨텍스트 전파 전략을 수립합니다. '관측성 설계', '대시보드 설계', '메트릭 분석', '알림 설정', '성능 분석' 등의 요청에 반응합니다."
---

## 선행 조건

`adapter-develop` 스킬에서 Observable Port가 구현된 후 수행합니다.
`application-develop` 스킬에서 생성한 `application/03-implementation-results.md`를 읽어 UseCase 목록과 ctx.* 필드 설계를 확인합니다.

## 후속 스킬

```
project-spec → architecture-design → domain-develop → application-develop → adapter-develop → **observability-develop** → test-develop
```

# Observability Develop Skill

Functorium 프레임워크 기반 관측성 전략을 설계하는 스킬입니다.
Observable Port/Pipeline이 자동 수집하는 3-Pillar 데이터를 비즈니스 KPI와 연결하고,
대시보드·알림·분산 추적 분석 전략을 수립합니다.

## 워크플로우

### Phase 1: 관측성 전략

사용자에게 질문:
- 핵심 비즈니스 KPI는 무엇인가요? (주문 전환율, 평균 처리 시간, 에러율 등)
- 운영 환경의 SLO/SLA가 있나요? (P95 < 200ms, 가용성 99.9% 등)
- 모니터링 백엔드는? (Prometheus + Grafana, Datadog, Azure Monitor 등)

**비즈니스 KPI → 기술 메트릭 매핑:**
- 비즈니스 퍼널(acquisition → activation → engagement → retention)에서 각 단계의 KPI 식별
- 각 KPI를 Functorium 필드 체계(`request.*`, `response.*`, `error.*`)로 매핑
- 기준선 목표 설정: Commands P95 < 200ms, Queries P95 < 50ms, Error rate < 0.1%

**ctx.* 컨텍스트 전파 전략:**
- CtxPillar 선택 기준에 따라 각 프로퍼티의 Pillar 타겟 결정
- 카디널리티 관리: MetricsTag에 Unbounded 값(`customer_id`, `order_id`) 금지
- 세그먼트 분석 차원 식별: `customer_tier`, `region`, `is_express` 등 Bounded 값

상세 패턴은 references 파일을 읽으세요.

### Phase 2: 대시보드 설계

**L1 스코어카드** (전체 건강 상태):
- 6개 건강 지표: 요청 수, 성공률, P95 지연, 에러율, 가용성, 처리량
- `rate(application_usecase_command_requests_total[5m])` 기반 요약

**L2 드릴다운** (핸들러별 상세):
- `request.layer` × `request.category.name` × `request.handler.name` 차원으로 분석
- 핸들러별 P95/P99 지연, 에러율, 요청 분포
- DomainEvent 발행 → Handler 체인 지연 시각화

**ctx.* 세그먼트 대시보드:**
- MetricsTag로 지정된 ctx.* 필드(`customer_tier`, `is_express` 등)로 세그먼트 분석
- 고객 등급별, 기능 플래그별 지연·에러율 비교

상세 패턴은 references 파일을 읽으세요.

### Phase 3: 알림 설계

**우선순위 분류:**
- P0 (Critical): `error.type = "exceptional"`, DB 연결 실패, Adapter 타임아웃 연쇄
- P1 (Warning): 핵심 Handler P95 > 1s, 에러율 > 5%, Validation 에러 급증
- P2 (Info): P95 > 500ms, 새로운 `error.code` 등장, DomainEvent Handler 지연 급증

**알림 위생:**
- 실행 가능한 알림만 유지 (알림 → 조치 매핑 필수)
- 오탐(false positive) 관리: 최소 5분 지속 조건
- 소유권 명시: 각 알림의 담당 팀/개인 지정
- 에스컬레이션 경로: P0 → 즉시 호출, P1 → 15분 내 대응, P2 → 업무 시간 내 확인

상세 패턴은 references 파일을 읽으세요.

### Phase 4: 분석 + 조치

**분산 추적 분석:**
- Span Name 패턴: `{layer} {category}[.{cqrs}] {handler}.{method}`
- 느린 트레이스 식별: `response.elapsed` > P95 기준
- 병목 Span 분석: Application → Adapter 호출 체인에서 가장 느린 Adapter 식별

**가설 → 실험 루프:**
1. 대시보드/알림에서 이상 징후 감지
2. 분산 추적으로 병목 Span 식별
3. ctx.* 세그먼트로 영향 범위 축소
4. 가설 수립 → 수정 → 기준선 대비 개선 검증

**리뷰 템플릿:**
- 관측성 설계 결과를 `{context}/observability/` 폴더에 문서화
- KPI-메트릭 매핑 테이블, 대시보드 명세, 알림 규칙, ctx.* 전파 전략

**출력:** `{context}/observability/` 폴더에 관측성 전략 문서

## 핵심 규칙

- **Functorium 필드 체계 준수:** `request.layer`, `request.category.name`, `request.handler.name`, `response.status`, `response.elapsed`, `error.type`, `error.code`
- **Meter Name 패턴:** `{service.namespace}.{layer}[.{category}]`
- **Instrument Name 패턴:** `{layer}.{category}[.{cqrs}].{type}` (점 구분, 소문자, 복수형)
- **ctx.* 카디널리티:** MetricsTag는 Bounded 값만 허용 (bool, 저카디널리티 enum)
- **Error 분류:** `expected` (비즈니스 오류), `exceptional` (시스템 오류), `aggregate` (복합 오류)
- **Histogram 사용:** `response.elapsed`는 Metrics Tag가 아닌 Histogram instrument로 기록

## References

- 관측성 전략: `references/observability-strategy.md`
- 대시보드 패턴: `references/dashboard-patterns.md`
- 알림 패턴: `references/alerting-patterns.md`
- ctx 전파 패턴: `references/ctx-enrichment-patterns.md`
