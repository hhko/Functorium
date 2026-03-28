# 관측성 전략 레퍼런스

## 비즈니스 KPI → 기술 메트릭 매핑

### 퍼널 단계별 KPI 매핑

| 퍼널 단계 | 비즈니스 KPI | Functorium 메트릭 | 필드/태그 |
|-----------|-------------|-------------------|----------|
| Acquisition | API 요청 수 | `application.usecase.command.requests` | `request.layer=application`, `request.category.type=command` |
| Activation | 첫 주문 성공률 | `application.usecase.command.responses` | `response.status=success`, `request.handler.name=PlaceOrderHandler` |
| Engagement | 평균 처리 시간 | `application.usecase.command.duration` | P95 Histogram quantile |
| Retention | 에러율 추이 | `application.usecase.command.responses` | `response.status=failure`, `error.type` 기준 |

### 레이어별 핵심 지표

#### Application Layer

| 지표 | Instrument | 타입 | 설명 |
|------|-----------|------|------|
| `usecase.request.count` | `application.usecase.{cqrs}.requests` | Counter | UseCase 요청 수 |
| `usecase.response.duration` | `application.usecase.{cqrs}.duration` | Histogram | UseCase 처리 시간(초) |
| `usecase.error.count` | `application.usecase.{cqrs}.responses` | Counter | `response.status=failure` 필터 |

#### Adapter Layer

| 지표 | Instrument | 타입 | 설명 |
|------|-----------|------|------|
| `adapter.request.count` | `adapter.{category}.requests` | Counter | Adapter 호출 수 |
| `adapter.response.duration` | `adapter.{category}.duration` | Histogram | Adapter 처리 시간(초) |
| `adapter.error.count` | `adapter.{category}.responses` | Counter | `response.status=failure` 필터 |

#### DomainEvent

| 지표 | Instrument | 타입 | 설명 |
|------|-----------|------|------|
| `event.publish.count` | `adapter.event.requests` | Counter | 이벤트 발행 수 |
| `event.handler.duration` | `application.usecase.event.duration` | Histogram | 이벤트 핸들러 처리 시간(초) |

### Meter Name 규칙

```
{service.namespace}.{layer}[.{category}]
```

| 예시 | Meter Name |
|------|-----------|
| Application UseCase | `mycompany.production.application.usecase` |
| Adapter Repository | `mycompany.production.adapter.repository` |
| Adapter Event | `mycompany.production.adapter.event` |

### Instrument Name 규칙

```
{layer}.{category}[.{cqrs}].{type}
```

| 예시 | Instrument Name | 타입 |
|------|----------------|------|
| Command 요청 수 | `application.usecase.command.requests` | Counter |
| Command 응답 수 | `application.usecase.command.responses` | Counter |
| Command 처리 시간 | `application.usecase.command.duration` | Histogram |
| Repository 요청 수 | `adapter.repository.requests` | Counter |
| Event 발행 응답 | `adapter.event.responses` | Counter |

## 기준선 목표

### SLO 기준선

| 카테고리 | 지표 | 목표 | 측정 방법 |
|---------|------|------|----------|
| Command UseCase | P95 지연 | < 200ms | `histogram_quantile(0.95, application.usecase.command.duration)` |
| Query UseCase | P95 지연 | < 50ms | `histogram_quantile(0.95, application.usecase.query.duration)` |
| Repository Adapter | P95 지연 | < 100ms | `histogram_quantile(0.95, adapter.repository.duration)` |
| External API | P95 지연 | < 500ms | `histogram_quantile(0.95, adapter.external_api.duration)` |
| 전체 에러율 | 에러 비율 | < 0.1% | `rate(responses{response_status=failure}) / rate(responses)` |
| 가용성 | 성공 비율 | > 99.9% | `1 - (failure_rate)` |

### OKR 프레임워크 예시

```
Objective: 주문 처리 성능 개선
├── KR1: Command P95 지연을 800ms → 600ms로 개선
│   └── 메트릭: histogram_quantile(0.95, application.usecase.command.duration{request.handler.name="PlaceOrderHandler"})
├── KR2: Repository Adapter P95 지연을 200ms → 100ms로 개선
│   └── 메트릭: histogram_quantile(0.95, adapter.repository.duration{request.handler.name="ProductRepository"})
└── KR3: 전체 에러율을 0.5% → 0.1% 이하로 감소
    └── 메트릭: rate(responses{response_status="failure"}[5m]) / rate(responses[5m])
```

## Error 분류별 대응 전략

| error.type | 의미 | 대응 | 알림 수준 |
|-----------|------|------|----------|
| `expected` | 비즈니스 오류 (재고 부족, 유효성 실패) | 비즈니스 로직 검토, UX 개선 | P2 (추이 모니터링) |
| `exceptional` | 시스템 오류 (DB 장애, 외부 API 타임아웃) | 인프라 점검, 재시도 정책 검토 | P0/P1 (즉시 대응) |
| `aggregate` | 복합 오류 (여러 오류 집계) | Primary 오류 분석 후 개별 대응 | Primary error.type 기준 |
