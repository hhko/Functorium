# ctx.* 컨텍스트 전파 패턴 레퍼런스

## CtxPillar 선택 기준

### Pillar별 용도와 선택 가이드

| CtxPillar | 전파 대상 | 용도 | 선택 기준 |
|-----------|----------|------|----------|
| `Logging` only | Serilog LogContext | Debug/verbose 데이터 | 요청 본문, 상세 파라미터, 내부 메모 등 고빈도·고카디널리티 데이터 |
| `Default` (Logging + Tracing) | Serilog + Activity.SetTag | 식별자, 참조 키 | `customer_id`, `order_id` 등 트레이스에서 검색 가능해야 하는 식별자 |
| `All` (Logging + Tracing + MetricsTag) | Serilog + Activity + MetricsTagContext | 세그먼트 분석 차원 | `customer_tier`, `region`, `is_express` 등 Bounded 카디널리티 값 |
| `MetricsValue` | Histogram instrument 기록 | 수치 통계 집계 | `order_total_amount`, `item_count` 등 수치 필드의 분포 분석 |

### 결정 흐름

```
프로퍼티가 디버그용인가?
├── YES → Logging only: [CtxTarget(CtxPillar.Logging)]
└── NO → 트레이스에서 검색 필요?
    ├── NO → [CtxIgnore]
    └── YES → 메트릭 세그먼트로 사용?
        ├── NO → Default (기본값, 어트리뷰트 불필요)
        └── YES → 카디널리티가 Bounded인가?
            ├── YES (bool, 저카디널리티 enum) → [CtxTarget(CtxPillar.All)]
            └── NO (string, Guid, 수치) → 수치인가?
                ├── YES → [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
                └── NO → Default 유지 (MetricsTag 금지: FUNCTORIUM005 경고)
```

## 카디널리티 관리

### 카디널리티 분류표

| 카디널리티 수준 | C# 타입 | MetricsTag 허용 | MetricsValue 허용 | 예시 |
|---------------|---------|----------------|-------------------|------|
| Fixed | `bool` | 안전 | 불가 | `is_express`, `is_premium` |
| BoundedLow | `enum` | 조건부 (값 < 20개) | 불가 | `OrderStatus`, `CustomerTier` |
| Unbounded | `string`, `Guid`, `DateTime` | **금지** (FUNCTORIUM005) | 불가 | `customer_id`, `order_id` |
| Numeric | `int`, `long`, `decimal` | **경고** (FUNCTORIUM005) | 허용 | `item_count`, `total_amount` |
| NumericCount | 컬렉션 `.Count` | **경고** (FUNCTORIUM005) | 허용 | `lines_count` |

### 위험 시나리오

```
# BAD: customer_id를 MetricsTag로 사용
[CtxTarget(CtxPillar.All)]           // MetricsTag 포함
public string CustomerId { get; }    // Unbounded → 시계열 폭발

# GOOD: customer_tier를 MetricsTag로 사용
[CtxTarget(CtxPillar.All)]           // MetricsTag 포함
public CustomerTier Tier { get; }    // BoundedLow (3~5개 값)

# GOOD: customer_id는 Default(L+T)만
public string CustomerId { get; }    // 기본값 → Logging + Tracing만
```

## 코드 예시

### 인터페이스 수준 CtxPillar 지정

```csharp
// 모든 프로퍼티를 3-Pillar Tag로 전파하고, ctx.{property}로 승격
[CtxRoot]
[CtxTarget(CtxPillar.All)]
public interface IRegional
{
    string RegionCode { get; }     // → ctx.region_code (L+T+MetricsTag)
}
```

### Request/Response 프로퍼티별 CtxPillar 지정

```csharp
public sealed class PlaceOrderCommand
{
    public sealed record Request(
        string CustomerId,                           // 기본(L+T). 고카디널리티 → MetricsTag 금지
        [CtxTarget(CtxPillar.All)] bool IsExpress,   // 3-Pillar Tag. boolean → 안전
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        int ItemCount,                               // L+T + Histogram 기록. 수치 → MetricsValue OK
        List<OrderLine> Lines,                       // 기본(L+T). 컬렉션 → lines_count 전파
        [CtxTarget(CtxPillar.Logging)] string InternalNote,  // Logging 전용
        [CtxIgnore] string DebugInfo                 // 완전 제외
    ) : ICommandRequest<Response>, IRegional;

    public sealed record Response(
        string OrderId,                              // 기본(L+T)
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        decimal TotalAmount                          // L+T + Histogram 기록
    );
}
```

### 생성되는 ctx.* 필드 매핑

| ctx 필드 | 타입 | Logging | Tracing | MetricsTag | MetricsValue |
|----------|------|---------|---------|------------|--------------|
| `ctx.region_code` | keyword | O | O | **O** | - |
| `ctx.place_order_command.request.customer_id` | keyword | O | O | - | - |
| `ctx.place_order_command.request.is_express` | boolean | O | O | **O** | - |
| `ctx.place_order_command.request.item_count` | long | O | O | - | **O** |
| `ctx.place_order_command.request.lines_count` | long | O | O | - | - |
| `ctx.place_order_command.request.internal_note` | keyword | O | - | - | - |
| `ctx.place_order_command.response.order_id` | keyword | O | O | - | - |
| `ctx.place_order_command.response.total_amount` | double | O | O | - | **O** |

### DomainEvent ctx.* 전파

```csharp
// 최상위 이벤트: ctx.{event}.{property}
public sealed record OrderPlacedEvent(
    string CustomerId,                              // → ctx.order_placed_event.customer_id (L+T)
    [CtxTarget(CtxPillar.All)] bool IsExpress       // → ctx.order_placed_event.is_express (L+T+MetricsTag)
) : IDomainEvent;

// 중첩 이벤트: ctx.{containing_type}.{event}.{property}
public sealed class Order
{
    public sealed record CreatedEvent(
        string OrderId                              // → ctx.order.created_event.order_id (L+T)
    ) : IDomainEvent;
}
```

## 세그먼트 분석 패턴

### 고객 등급별 에러율

MetricsTag로 전파된 `ctx.customer_tier` 필드를 차원으로 분석합니다.

```promql
# 고객 등급별 에러율
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (ctx_customer_tier)
/ sum(rate(application_usecase_command_responses_total[5m])) by (ctx_customer_tier) * 100
```

### 기능 플래그별 지연

MetricsTag로 전파된 `ctx.is_express` 필드를 차원으로 분석합니다.

```promql
# Express vs 일반 주문 P95 지연
histogram_quantile(0.95,
  sum(rate(application_usecase_command_duration_bucket[5m])) by (le, ctx_place_order_command_request_is_express)
)
```

### MetricsValue 분포 분석

MetricsValue로 기록된 `ctx.place_order_command.request.item_count` 필드의 분포를 분석합니다.

```promql
# 주문당 아이템 수 P50/P95
histogram_quantile(0.50, sum(rate(ctx_place_order_command_request_item_count_bucket[5m])) by (le))
histogram_quantile(0.95, sum(rate(ctx_place_order_command_request_item_count_bucket[5m])) by (le))
```

## 컴파일 타임 진단

| 진단 코드 | 심각도 | 조건 | 대응 |
|----------|-------|------|------|
| FUNCTORIUM002 | Warning | 동일 ctx 필드명에 서로 다른 OpenSearch 타입 그룹 할당 | 필드명 변경 또는 타입 통일 |
| FUNCTORIUM005 | Warning | 고카디널리티 타입 + MetricsTag | MetricsTag 제거하고 Default(L+T)로 변경 |
| FUNCTORIUM006 | Error | 비수치 타입 + MetricsValue | MetricsValue 제거 (수치 타입만 허용) |
| FUNCTORIUM007 | Warning | MetricsTag + MetricsValue 동시 지정 | 하나만 선택 (Tag = 차원, Value = 측정값) |

## 제어 어트리뷰트 요약

| 어트리뷰트 | 적용 대상 | 효과 |
|-----------|----------|------|
| `[CtxRoot]` | interface, property, parameter | `ctx.{field}` 루트 레벨로 승격 (네이밍에만 영향) |
| `[CtxIgnore]` | class, property, parameter | 모든 Pillar에서 제외 (`[CtxTarget]`보다 우선) |
| `[CtxTarget(CtxPillar)]` | interface, property, parameter | Pillar 타겟 지정 (미지정 시 Default) |
