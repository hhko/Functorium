---
title: "ADR-0006: CtxEnricher Pillar 타겟팅 전략"
status: "accepted"
date: 2026-03-24
---

## 맥락과 문제

Functorium의 CtxEnricher는 비즈니스 컨텍스트(예: `ctx.order.id`, `ctx.product.category`)를 관측성 3-Pillar(Logging, Tracing, Metrics)에 전파합니다. 문제는 Pillar마다 컨텍스트에 대한 감도가 전혀 다르다는 점입니다.

`ctx.order.id`를 Metrics 태그로 전파하면 어떤 일이 벌어지는지 봅니다. 하루 주문이 10만 건이면 10만 개의 고유 시계열이 생성됩니다. Prometheus의 카디널리티 경고가 발동하고, Grafana 대시보드는 쿼리 타임아웃으로 렌더링에 실패하며, 클라우드 모니터링 비용이 시계열 수에 비례하여 급증합니다. 반면 동일한 `ctx.order.id`는 Logging에서 특정 주문의 로그를 필터링하는 데, Tracing에서 분산 호출 그래프를 주문 단위로 추적하는 데 필수적입니다. 모든 컨텍스트를 3-Pillar에 무차별 전파하는 것이 아니라, Pillar별로 어떤 컨텍스트를 전파할지 제어하는 기본 전략과 옵트인 메커니즘이 필요합니다.

## 검토한 옵션

1. 기본 Logging+Tracing, Metrics는 [CtxTarget]으로 opt-in
2. 3-Pillar 전부 기본 전파
3. Logging만 기본 전파

## 결정

**선택한 옵션: "기본 Logging+Tracing, Metrics는 [CtxTarget]으로 opt-in"**. 아무런 설정 없이도 모든 비즈니스 컨텍스트가 Logging과 Tracing에 전파되어 개별 요청 추적이 가능합니다. Metrics에는 기본적으로 어떤 컨텍스트도 태그로 전파하지 않아 카디널리티 폭발을 원천 차단합니다. `ctx.product.category`처럼 카디널리티가 낮고 대시보드 필터에 유용한 필드만 `[CtxTarget(Metrics)]`로 명시적으로 포함합니다.

### 결과

- Good, because `ctx.order.id` 같은 고카디널리티 필드가 Metrics 태그에 기본 전파되지 않아, 시계열 수가 비즈니스 트래픽에 비례하여 증가하는 것을 원천 차단합니다.
- Good, because Logging에서는 `ctx.order.id`로 특정 주문의 로그를 필터링하고, Tracing에서는 동일 값으로 분산 호출 그래프를 추적할 수 있어 개별 요청 진단에 필요한 정보가 빠짐없이 기록됩니다.
- Good, because `[CtxTarget(Metrics)]` 어트리뷰트로 필드 단위로 Metrics 전파를 제어하여, `ctx.product.category`(카디널리티 ~50)는 대시보드 필터로 활용하면서 `ctx.order.id`(카디널리티 ~무한)는 제외하는 세밀한 정책이 가능합니다.
- Bad, because 새로운 컨텍스트 필드를 추가할 때 개발자가 Metrics 포함 여부를 판단하고 `[CtxTarget]`을 명시해야 하므로, 대시보드에 필요한 필터 태그를 설정하지 않아 운영 시 뒤늦게 발견할 수 있습니다.

### 확인

- CtxEnricher가 기본적으로 Logging과 Tracing에만 컨텍스트를 전파하는지 단위 테스트로 확인합니다.
- `[CtxTarget(Metrics)]`가 적용된 필드만 Metrics 태그에 포함되는지 스냅샷 테스트로 검증합니다.
- 3-Pillar 테스트에서 각 Pillar의 컨텍스트 전파 범위가 기대와 일치하는지 확인합니다.

## 옵션별 장단점

### 기본 Logging+Tracing, Metrics는 [CtxTarget]으로 opt-in

- Good, because 기본 설정만으로 Metrics의 카디널리티가 통제되어, Prometheus 카디널리티 경고나 Grafana 쿼리 타임아웃을 예방합니다.
- Good, because `ctx.product.category` 같은 저카디널리티 필드를 `[CtxTarget(Metrics)]`로 선택적으로 포함하여 대시보드 필터링에 활용할 수 있습니다.
- Good, because Logging과 Tracing에는 모든 비즈니스 컨텍스트가 기본 전파되어, 개별 주문/상품 단위의 요청 추적에 제약이 없습니다.
- Bad, because 개발자가 각 컨텍스트 필드의 카디널리티를 판단하고 `[CtxTarget]` 적용 여부를 결정해야 하므로, 카디널리티 개념에 대한 이해가 전제됩니다.

### 3-Pillar 전부 기본 전파

- Good, because `[CtxTarget]` 설정이 필요 없어 구현이 단순하고, 어떤 Pillar에서든 모든 비즈니스 컨텍스트를 즉시 활용할 수 있습니다.
- Bad, because `ctx.order.id`가 Metrics 태그에 포함되면 하루 주문 10만 건 기준 10만 개의 시계열이 생성되어, Prometheus 저장소 용량과 쿼리 비용이 통제 불능 상태가 됩니다.
- Bad, because 시계열 수 증가로 Grafana 대시보드의 쿼리 응답 시간이 수 초에서 타임아웃으로 악화되어, 운영 모니터링 자체가 불가능해질 수 있습니다.

### Logging만 기본 전파

- Good, because Metrics와 Tracing에 컨텍스트가 전파되지 않으므로 카디널리티 폭발 위험이 구조적으로 제거됩니다.
- Bad, because Tracing Span에 `ctx.order.id`가 없으면 Jaeger/Zipkin에서 분산 호출 그래프를 주문 단위로 필터링할 수 없어, 장애 시 특정 주문의 호출 경로를 추적하는 것이 불가능합니다.
- Bad, because Tracing에 비즈니스 컨텍스트를 추가하려면 별도의 opt-in 메커니즘을 다시 만들어야 하며, 이는 결국 선택지 1과 동일한 설계로 회귀합니다.

## 관련 정보

- 관련 커밋: `3a080788` refactor(observability): LogEnricher를 CtxEnricher로 이름 변경
- 관련 커밋: `e4aae12a` test(observability): 스냅샷 폴더 범주별 재구성 + ctx Enricher 3-Pillar 테스트 추가
- 관련 문서: `Docs.Site/src/content/docs/guides/observability/`
