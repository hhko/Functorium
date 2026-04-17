---
title: "ADR-0019: Observability - 관측성 필드 네이밍 snake_case + dot 규칙"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

장애 대응 중 Trace Span에서 발견한 `requestCategory` 필드값으로 Logs를 검색했더니 결과가 0건이었다. Logs에서는 같은 데이터를 `request_category`로 기록하고 있었기 때문이다. Metrics에서는 또 `request-category`라는 세 번째 이름을 사용하고 있었다. 동일한 비즈니스 이벤트가 3-Pillar마다 서로 다른 필드명으로 기록되어, Grafana에서 Trace-to-Logs 전환이 불가능했고 대시보드의 상관 분석(correlation) 쿼리가 매번 필드명 매핑을 수작업으로 맞춰야 했다.

3-Pillar 전체에서 하나의 필드명 규칙을 강제하여 상관 분석을 즉시 가능하게 하되, OpenTelemetry 시맨틱 규약(Semantic Conventions)과 자연스럽게 호환되는 네이밍 체계가 필요했다.

## 검토한 옵션

- **옵션 1**: camelCase (`requestCategoryName`, `responseStatus`)
- **옵션 2**: kebab-case (`request-category-name`, `response-status`)
- **옵션 3**: snake_case + dot (`request.category.name`, `response.status`)
- **옵션 4**: underscore only (`request_category_name`, `response_status`)

## 결정

**옵션 3: snake_case + dot 규칙을 채택한다.**

독자적인 규칙을 발명하지 않고, OpenTelemetry 시맨틱 규약이 이미 사용하는 패턴(`http.request.method`, `db.system`, `rpc.service`)을 그대로 따른다. 업계 표준과 일치하면 도구 호환성을 별도로 검증할 필요가 없다.

모든 관측성 필드에 다음 네이밍 규칙을 적용한다:

- **네임스페이스 구분**: dot(`.`)으로 계층을 표현 (예: `request.category.name`, `response.status`, `error.code`)
- **단어 구분**: snake_case (예: `error.stack_trace`, `request.content_type`)
- **count 필드 규칙**: 메트릭 이름에는 `.count` (dot 구분), 속성(attribute)에는 `_count` (underscore 구분)

### 결과

- **긍정적**: Trace Span에서 발견한 `error.code` 필드명으로 Logs를 검색하면 즉시 결과가 나온다. 필드명 매핑 없이 Grafana의 Trace-to-Logs 전환이 동작한다. dot 계층 덕분에 Grafana와 Kibana에서 `request.*`, `error.*` 같은 트리 구조로 필드를 탐색할 수 있다. 팀 전체가 "OpenTelemetry 규약을 따른다"는 하나의 원칙만 기억하면 된다.
- **부정적**: 기존에 camelCase나 underscore로 기록하던 필드를 모두 마이그레이션해야 하며, 기존 대시보드 쿼리도 함께 갱신해야 한다. dot 구분자를 네임스페이스로 해석하지 않는 일부 도구에서는 이스케이핑이 필요할 수 있다.

### 확인

- 프레임워크에서 생성하는 모든 Trace Attribute, Metric Attribute, Structured Log Property가 snake_case + dot 규칙을 따르는지 확인한다.
- Grafana 대시보드에서 동일 필드명으로 Trace와 Logs를 상관 조회할 수 있는지 확인한다.
- count 필드가 메트릭과 속성에서 각각 `.count`와 `_count`로 올바르게 구분되는지 확인한다.

## 옵션별 장단점

### 옵션 1: camelCase

- **장점**: C#/JavaScript 개발자에게 친숙하다. `System.Text.Json`의 기본 정책(`JsonNamingPolicy.CamelCase`)과 일치하여 직렬화가 간편하다.
- **단점**: OpenTelemetry 시맨틱 규약(`http.request.method`)과 불일치하여 프레임워크 자체 필드와 사용자 정의 필드의 네이밍이 뒤섞인다. 계층 구조를 표현할 수 없어 `requestCategoryName`처럼 이름이 길어지고, Grafana에서 `request.*` 같은 트리 탐색이 불가능하다.

### 옵션 2: kebab-case

- **장점**: URL, HTTP 헤더(`Content-Type`, `Accept-Language`)와 일관성이 있어 가독성이 좋다.
- **단점**: Serilog의 `LogContext.PushProperty`가 하이픈이 포함된 속성명을 지원하지 않는다. C# 식별자로 사용할 수 없어 모든 필드명을 문자열 리터럴로 관리해야 하며, 오타 위험이 높다. OpenTelemetry 규약과 불일치한다.

### 옵션 3: snake_case + dot

- **장점**: `http.request.method`, `db.system` 등 OpenTelemetry 시맨틱 규약과 완전히 일치하여 프레임워크 필드와 사용자 정의 필드가 동일한 패턴을 따른다. dot 구분 덕분에 Grafana에서 `request.*`, `error.*` 트리 탐색이 가능하고, Elasticsearch에서 자동 필드 그룹화가 동작한다. Trace, Metrics, Logs 3-Pillar 간 동일 필드명 보장으로 상관 분석 쿼리가 즉시 동작한다.
- **단점**: C# 코드에서 `"error.stack_trace"` 같은 dot 포함 문자열 상수를 관리해야 한다. Elasticsearch의 일부 설정에서 dot을 object 중첩으로 해석하여 의도치 않은 매핑이 생길 수 있다.

### 옵션 4: underscore only

- **장점**: Prometheus 네이밍 규약(`http_request_duration_seconds`)과 일치한다. dot 관련 이슈가 없다. C# `const string`으로 표현하기 쉽다.
- **단점**: `request_category_name`에서 `request`가 네임스페이스인지 단어의 일부인지 구분할 수 없어 계층 구조가 사라진다. OpenTelemetry 시맨틱 규약의 dot 계층과 불일치하여 프레임워크 필드(`http.request.method`)와 사용자 정의 필드(`request_category_name`)의 네이밍이 혼재된다. 필드명이 길어질수록 `_` 연속으로 가독성이 급격히 떨어진다.

## 관련 정보

- 커밋: a5027a78, 419659df
- 관련 ADR: [ADR-0009 오류 분류 3-Type 체계](../0009-error-classification-three-types.md)
