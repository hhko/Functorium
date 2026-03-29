---
title: "ADR-0005: 파이프라인 실행 순서"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

Functorium의 Usecase 파이프라인은 CtxEnricher, Metrics, Tracing, Logging, Validation, Caching, Exception, Transaction 등 7~8개의 횡단 관심사(cross-cutting concern) 행위를 미들웨어로 구성합니다. 이들의 실행 순서가 동작의 정확성과 관측 가능성에 직접적인 영향을 미칩니다.

예를 들어, Logging이 Validation보다 먼저 실행되면 검증 실패 요청도 로그에 남아 노이즈가 증가합니다. 반대로 Metrics가 Validation 뒤에 있으면 검증 실패 건수를 메트릭으로 수집할 수 없습니다. Command와 Query는 필요한 행위가 다르므로(예: Query에는 Transaction이 불필요하지만 Caching이 필요) 각각의 파이프라인 단계를 명확히 정의해야 합니다.

## 검토한 옵션

1. CtxEnricher → Metrics → Tracing → Logging → Validation → [Caching] → Exception → [Transaction] → Custom → Handler
2. Logging 최선두 배치
3. Validation 최선두 배치

## 결정

**선택한 옵션: "CtxEnricher → Metrics → Tracing → Logging → Validation → [Caching] → Exception → [Transaction] → Custom → Handler"**, 관측성 컨텍스트를 가장 먼저 설정하고, 메트릭과 트레이스가 검증 실패를 포함한 모든 요청을 기록하며, 로깅은 의미 있는 요청만 상세 기록하는 균형을 달성하기 때문입니다.

### 결과

- Good, because Command 7단계, Query 8단계(Caching 포함)로 역할이 명확히 분리됩니다.
- Good, because Metrics/Tracing이 Validation 앞에 있어 검증 실패도 관측 가능합니다.
- Good, because `where` 제약 조건으로 Command/Query 파이프라인이 컴파일 타임에 필터링됩니다.
- Bad, because 파이프라인 단계가 많아 디버깅 시 호출 스택이 깊어집니다.
- Bad, because 순서 변경 시 전체 행위에 영향을 미쳐 신중한 검토가 필요합니다.

### 확인

- Command 파이프라인: CtxEnricher → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler (7단계).
- Query 파이프라인: CtxEnricher → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler (8단계).
- DI 등록 순서가 위 실행 순서와 일치하는지 확인합니다.

## 옵션별 장단점

### CtxEnricher → Metrics → Tracing → Logging → Validation → [Caching] → Exception → [Transaction] → Custom → Handler

- Good, because CtxEnricher가 최선두에서 비즈니스 컨텍스트를 설정하여 이후 모든 Pillar가 활용합니다.
- Good, because Metrics가 Validation 앞에 있어 검증 실패 건수를 카운팅합니다.
- Good, because Tracing이 Validation 앞에 있어 실패 요청도 추적 가능합니다.
- Good, because Logging이 Validation 뒤가 아닌 앞에 있어 검증 실패 사유를 로깅합니다.
- Good, because Exception이 Transaction 앞에 있어 예외를 잡아 정상 응답으로 변환합니다.
- Bad, because 단계 수가 많아 새로운 행위 추가 시 위치 선정에 고민이 필요합니다.

### Logging 최선두 배치

- Good, because 모든 요청이 상세 로깅되어 디버깅에 유리합니다.
- Bad, because 검증 실패 요청까지 상세 로깅되어 로그 노이즈가 증가합니다.
- Bad, because CtxEnricher 이전에 로깅되면 비즈니스 컨텍스트가 누락됩니다.

### Validation 최선두 배치

- Good, because 유효하지 않은 요청을 즉시 차단하여 후속 처리 비용을 절감합니다.
- Bad, because 검증 실패 건이 Metrics/Tracing에 기록되지 않아 관측이 불가능합니다.
- Bad, because CtxEnricher 이전에 실행되어 비즈니스 맥락 없이 실패 응답이 반환됩니다.

## 관련 정보

- 관련 커밋: `ace89d39` feat(books/pipeline): 타입 안전한 Usecase 파이프라인 제약 설계 Book 추가
- 관련 커밋: `91b57254` refactor(pipeline): where 제약 조건으로 Command/Query 파이프라인 컴파일 타임 필터링
- 관련 문서: `Docs.Site/src/content/docs/tutorials/usecase-pipeline/`
