---
title: "ADR-0007: CtxEnricher Pillar 타겟팅 전략"
status: "accepted"
date: 2026-03-24
---

## 맥락과 문제

Functorium의 CtxEnricher는 비즈니스 컨텍스트(예: `ctx.order.id`, `ctx.product.category`)를 관측성 3-Pillar(Logging, Tracing, Metrics)에 전파합니다. 그러나 모든 컨텍스트를 3-Pillar 전부에 무조건 전파하면, 특히 Metrics에서 높은 카디널리티(high cardinality) 태그가 시계열 폭발(cardinality explosion)을 일으켜 모니터링 시스템의 비용과 성능에 심각한 영향을 줍니다.

예를 들어 `ctx.order.id`를 Metrics 태그로 전파하면 주문 건수만큼 고유한 시계열이 생성되어 Prometheus/Grafana 등의 저장소 부하가 급증합니다. 반면 Logging과 Tracing에서는 개별 건 추적에 필수적인 정보입니다. 어떤 Pillar에 어떤 컨텍스트를 전파할지에 대한 기본 전략과 옵트인 메커니즘이 필요합니다.

## 검토한 옵션

1. 기본 Logging+Tracing, Metrics는 [CtxTarget]으로 opt-in
2. 3-Pillar 전부 기본 전파
3. Logging만 기본 전파

## 결정

**선택한 옵션: "기본 Logging+Tracing, Metrics는 [CtxTarget]으로 opt-in"**, 카디널리티 폭발을 원천 차단하면서도 낮은 카디널리티 컨텍스트(예: `ctx.product.category`)는 `[CtxTarget(Metrics)]`로 명시적으로 Metrics에 포함할 수 있기 때문입니다.

### 결과

- Good, because 고카디널리티 컨텍스트가 Metrics에 기본 전파되지 않아 시계열 폭발을 방지합니다.
- Good, because Logging과 Tracing에는 모든 컨텍스트가 전파되어 개별 요청 추적이 가능합니다.
- Good, because `[CtxTarget]` 어트리뷰트로 Metrics 전파를 컨텍스트 필드 단위로 세밀하게 제어합니다.
- Bad, because Metrics에 포함할 컨텍스트를 개발자가 명시적으로 선택해야 하므로 설정 누락 가능성이 있습니다.

### 확인

- CtxEnricher가 기본적으로 Logging과 Tracing에만 컨텍스트를 전파하는지 단위 테스트로 확인합니다.
- `[CtxTarget(Metrics)]`가 적용된 필드만 Metrics 태그에 포함되는지 스냅샷 테스트로 검증합니다.
- 3-Pillar 테스트에서 각 Pillar의 컨텍스트 전파 범위가 기대와 일치하는지 확인합니다.

## 옵션별 장단점

### 기본 Logging+Tracing, Metrics는 [CtxTarget]으로 opt-in

- Good, because 카디널리티 폭발을 기본 설정에서 방지합니다.
- Good, because 낮은 카디널리티 컨텍스트는 `[CtxTarget]`으로 Metrics에 선택적으로 포함 가능합니다.
- Good, because 개별 요청 추적(Logging/Tracing)에는 모든 컨텍스트가 사용 가능합니다.
- Bad, because Metrics에 포함할 컨텍스트를 개발자가 판단하고 명시해야 합니다.

### 3-Pillar 전부 기본 전파

- Good, because 설정이 단순하며 모든 컨텍스트가 어디서든 사용 가능합니다.
- Bad, because 고카디널리티 태그로 인해 Metrics 저장소에 시계열 폭발이 발생합니다.
- Bad, because 모니터링 시스템 비용이 급증하고 쿼리 성능이 저하됩니다.

### Logging만 기본 전파

- Good, because 카디널리티 폭발 위험이 전혀 없습니다.
- Bad, because Tracing Span에 비즈니스 컨텍스트가 없어 분산 추적 시 맥락이 손실됩니다.
- Bad, because Tracing에서 비즈니스 컨텍스트를 보려면 별도 설정이 필요합니다.

## 관련 정보

- 관련 커밋: `3a080788` refactor(observability): LogEnricher를 CtxEnricher로 이름 변경
- 관련 커밋: `e4aae12a` test(observability): 스냅샷 폴더 범주별 재구성 + ctx Enricher 3-Pillar 테스트 추가
- 관련 문서: `Docs.Site/src/content/docs/guides/observability/`
