---
title: "ADR-0016: 관측성 필드 네이밍 snake_case + dot 규칙"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

OpenTelemetry 기반 관측성 시스템에서 Trace, Metrics, Logs 3-Pillar 간에 동일한 비즈니스 이벤트를 서로 다른 필드명으로 기록하면 상관 분석(correlation)이 불가능하다. 예를 들어 Trace에서는 `requestCategory`, Metrics에서는 `request-category`, Logs에서는 `request_category`로 기록하면 대시보드에서 이들을 연결할 수 없다.

3-Pillar 전체에서 일관된 필드 네이밍 규칙이 필요하며, OpenTelemetry 시맨틱 규약(Semantic Conventions)과의 호환성도 고려해야 한다.

## 검토한 옵션

- **옵션 1**: camelCase (`requestCategoryName`, `responseStatus`)
- **옵션 2**: kebab-case (`request-category-name`, `response-status`)
- **옵션 3**: snake_case + dot (`request.category.name`, `response.status`)
- **옵션 4**: underscore only (`request_category_name`, `response_status`)

## 결정

**옵션 3: snake_case + dot 규칙을 채택한다.**

모든 관측성 필드에 다음 네이밍 규칙을 적용한다:

- **네임스페이스 구분**: dot(`.`)으로 계층을 표현 (예: `request.category.name`, `response.status`, `error.code`)
- **단어 구분**: snake_case (예: `error.stack_trace`, `request.content_type`)
- **count 필드 규칙**: 메트릭 이름에는 `.count` (dot 구분), 속성(attribute)에는 `_count` (underscore 구분)

이 규칙은 OpenTelemetry 시맨틱 규약의 `http.request.method`, `db.system`, `rpc.service` 등과 동일한 패턴을 따른다.

### 결과

- **긍정적**: 3-Pillar 간 동일 필드명으로 상관 분석이 가능하다. OpenTelemetry 시맨틱 규약과 자연스럽게 호환된다. dot으로 계층 구조가 드러나 Grafana, Kibana 등에서 필드를 트리 구조로 탐색할 수 있다. 팀 전체가 하나의 규칙만 기억하면 된다.
- **부정적**: 기존에 다른 네이밍 규칙을 사용하던 필드를 마이그레이션해야 한다. dot 구분자를 지원하지 않는 일부 도구에서는 이스케이핑이 필요할 수 있다.

### 확인

- 프레임워크에서 생성하는 모든 Trace Attribute, Metric Attribute, Structured Log Property가 snake_case + dot 규칙을 따르는지 확인한다.
- Grafana 대시보드에서 동일 필드명으로 Trace와 Logs를 상관 조회할 수 있는지 확인한다.
- count 필드가 메트릭과 속성에서 각각 `.count`와 `_count`로 올바르게 구분되는지 확인한다.

## 옵션별 장단점

### 옵션 1: camelCase

- **장점**: C#/JavaScript 개발자에게 친숙하다. JSON 직렬화와 자연스럽게 호환된다.
- **단점**: OpenTelemetry 시맨틱 규약과 불일치한다. 계층 구조를 표현할 수 없어 `requestCategoryName`처럼 이름이 길어진다. 대시보드 도구에서 자동 그룹화가 불가능하다.

### 옵션 2: kebab-case

- **장점**: URL, HTTP 헤더와 일관성이 있다. 가독성이 좋다.
- **단점**: Serilog 등 주요 .NET 로깅 라이브러리에서 속성명에 하이픈을 지원하지 않는다. C# 식별자로 사용할 수 없어 문자열 리터럴로만 참조해야 한다. OpenTelemetry 규약과 불일치한다.

### 옵션 3: snake_case + dot

- **장점**: OpenTelemetry 시맨틱 규약과 완전히 일치한다. dot으로 계층 구조가 명확히 드러나 도구의 자동 그룹화/트리 탐색을 활용할 수 있다. Prometheus, Grafana, Elasticsearch 등 주요 관측성 도구와 호환된다. 3-Pillar 간 일관성이 보장된다.
- **단점**: C# 코드에서 dot이 포함된 문자열 상수를 관리해야 한다. 일부 도구에서 dot을 네임스페이스 구분자가 아닌 문자 그대로 해석할 수 있다.

### 옵션 4: underscore only

- **장점**: Prometheus 네이밍 규약과 일치한다. dot 관련 이슈가 없다. C# 상수로 표현하기 쉽다.
- **단점**: 계층 구조를 표현할 수 없어 `request_category_name`에서 `request`가 네임스페이스인지 단어의 일부인지 구분이 불가능하다. OpenTelemetry 시맨틱 규약의 dot 계층과 불일치한다. 필드명이 길어질수록 가독성이 떨어진다.

## 관련 정보

- 커밋: a5027a78, 419659df
- 관련 ADR: [ADR-0009 오류 분류 3-Type 체계](./0009-error-classification-three-types.md)
