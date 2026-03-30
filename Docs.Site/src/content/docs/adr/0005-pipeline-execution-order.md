---
title: "ADR-0005: 파이프라인 실행 순서"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

Functorium의 Usecase 파이프라인은 CtxEnricher, Metrics, Tracing, Logging, Validation, Caching, Exception, Transaction 등 7~8개의 횡단 관심사를 미들웨어로 구성합니다. 문제는 이들의 실행 순서가 관측 데이터의 정확성과 시스템 동작을 결정적으로 바꾼다는 점입니다.

구체적인 상황을 봅니다. Logging을 Validation보다 먼저 배치하면, 봇이 보내는 잘못된 요청까지 상세 로그에 남아 초당 수천 건의 노이즈가 쌓이고 로그 저장 비용이 급증합니다. 반대로 Metrics를 Validation 뒤에 배치하면 검증 실패 건수가 메트릭에 잡히지 않아, "왜 성공률이 100%인데 사용자 불만이 많은가?"를 설명할 수 없습니다. 여기에 Command에는 Transaction이 필요하지만 Query에는 불필요하고, Query에는 Caching이 필요하지만 Command에는 위험하다는 차이까지 고려하면, 양쪽 파이프라인의 단계와 순서를 명확히 정의해야 합니다.

## 검토한 옵션

1. CtxEnricher → Metrics → Tracing → Logging → Validation → [Caching] → Exception → [Transaction] → Custom → Handler
2. Logging 최선두 배치
3. Validation 최선두 배치

## 결정

**선택한 옵션: "CtxEnricher → Metrics → Tracing → Logging → Validation → [Caching] → Exception → [Transaction] → Custom → Handler"**. CtxEnricher가 비즈니스 컨텍스트를 가장 먼저 설정하여 이후 모든 Pillar가 활용하고, Metrics/Tracing이 Validation 앞에서 검증 실패를 포함한 모든 요청을 기록하며, Logging은 컨텍스트가 풍부해진 상태에서 의미 있는 요청을 상세 기록합니다. 관측성의 완전성과 로그 노이즈 억제 사이의 균형을 이 순서로 달성합니다.

### 결과

- Good, because Command 7단계(Transaction 포함, Caching 제외), Query 8단계(Caching 포함, Transaction 제외)로 각 파이프라인에 필요한 행위만 정확히 포함됩니다.
- Good, because Metrics/Tracing이 Validation 앞에 위치하여 검증 실패 건수와 실패 트레이스가 누락 없이 관측됩니다.
- Good, because `where` 제약 조건으로 Transaction 행위가 Query에, Caching 행위가 Command에 적용되는 것을 컴파일 타임에 차단합니다.
- Bad, because 7~8단계 미들웨어가 중첩되어 예외 발생 시 호출 스택이 깊어지고, 어느 단계에서 문제가 발생했는지 추적하는 데 시간이 걸립니다.
- Bad, because 하나의 행위 순서를 변경하면(예: Logging을 Validation 뒤로 이동) 로그 수집 범위가 달라지는 등 전체 파이프라인 동작에 연쇄적 영향을 미칩니다.

### 확인

- Command 파이프라인: CtxEnricher → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler (7단계).
- Query 파이프라인: CtxEnricher → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler (8단계).
- DI 등록 순서가 위 실행 순서와 일치하는지 확인합니다.

## 옵션별 장단점

### CtxEnricher → Metrics → Tracing → Logging → Validation → [Caching] → Exception → [Transaction] → Custom → Handler

- Good, because CtxEnricher가 최선두에서 `ctx.order.id`, `ctx.product.category` 등 비즈니스 컨텍스트를 설정하여 이후 Metrics/Tracing/Logging 모두가 풍부한 맥락으로 기록합니다.
- Good, because Metrics가 Validation 앞에 있어 "분당 검증 실패 N건" 같은 지표를 대시보드에서 확인할 수 있습니다.
- Good, because Tracing이 Validation 앞에 있어 검증 실패 요청도 분산 추적 그래프에 Span으로 남아 원인 분석이 가능합니다.
- Good, because Logging은 CtxEnricher 이후에 실행되므로 비즈니스 컨텍스트가 포함된 구조화 로그를 생성하며, 검증 실패 사유까지 기록합니다.
- Good, because Exception 행위가 Transaction 앞에 위치하여, 트랜잭션 내부 예외를 잡아 `Fin.Fail`로 변환한 뒤 정상 응답으로 반환합니다.
- Bad, because 7~8단계 중 새로운 행위를 추가할 때 앞뒤 단계와의 의존 관계를 분석해야 하므로 위치 선정에 신중한 검토가 필요합니다.

### Logging 최선두 배치

- Good, because 검증 실패, 캐시 히트, 정상 처리 등 모든 요청이 빠짐없이 상세 로깅되어 장애 시 디버깅 정보가 풍부합니다.
- Bad, because 봇이나 잘못된 클라이언트가 보내는 검증 실패 요청까지 상세 로깅되어 로그 볼륨이 폭증하고 저장 비용이 급증합니다.
- Bad, because CtxEnricher 이전에 로깅되면 `ctx.order.id` 같은 비즈니스 컨텍스트가 빈 채로 기록되어, 로그만으로는 어떤 주문/상품에 대한 요청인지 알 수 없습니다.

### Validation 최선두 배치

- Good, because 유효하지 않은 요청을 첫 번째 단계에서 즉시 차단하여 Tracing/Logging/Transaction 등 후속 단계의 처리 비용을 절감합니다.
- Bad, because 검증 실패 건이 Metrics에 카운팅되지 않고 Tracing에 Span이 남지 않아, 운영 대시보드에서 "검증 실패율" 지표 자체를 구성할 수 없습니다.
- Bad, because CtxEnricher가 아직 실행되지 않은 상태이므로 비즈니스 컨텍스트 없이 실패 응답이 반환되어, 어떤 도메인 객체에 대한 검증이 실패했는지 추적할 수 없습니다.

## 관련 정보

- 관련 커밋: `ace89d39` feat(books/pipeline): 타입 안전한 Usecase 파이프라인 제약 설계 Book 추가
- 관련 커밋: `91b57254` refactor(pipeline): where 제약 조건으로 Command/Query 파이프라인 컴파일 타임 필터링
- 관련 문서: `Docs.Site/src/content/docs/tutorials/usecase-pipeline/`
