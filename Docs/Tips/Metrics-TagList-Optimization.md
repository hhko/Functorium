# Metrics TagList 최적화

## 개요

`System.Diagnostics.Metrics`에서 메트릭 태그를 전달할 때 `TagList` 구조체를 사용하여 힙 할당을 방지하고 GC 부담을 최소화하는 방법을 설명합니다.

## 문제

메트릭을 기록할 때마다 태그 배열을 생성하면 힙 메모리 할당이 발생합니다:

```csharp
// 문제: 매번 배열 객체가 힙에 할당됨
KeyValuePair<string, object?>[] tags =
[
    new("layer", "adapter"),
    new("category", "repository"),
    new("handler", "UserRepository"),
    new("method", "GetById")
];

counter.Add(1, tags);
```

고빈도 메트릭 기록 시 이러한 할당이 누적되어 GC 압박이 증가합니다.

## 해결 방법

`TagList` 구조체를 사용합니다:

```csharp
// 해결: TagList는 구조체로 스택에 할당됨
TagList tags = new()
{
    { "layer", "adapter" },
    { "category", "repository" },
    { "handler", "UserRepository" },
    { "method", "GetById" }
};

counter.Add(1, tags);
```

## TagList 구조체 특성

| 특성 | 설명 |
|------|------|
| 할당 위치 | 스택 (Stack) |
| GC 영향 | 없음 |
| 최대 태그 수 | 8개 (초과 시 내부 배열 할당) |
| 네임스페이스 | `System.Diagnostics` |

### 내부 구조

```csharp
public struct TagList : IList<KeyValuePair<string, object?>>, ...
{
    // 8개까지는 인라인 저장 (힙 할당 없음)
    private KeyValuePair<string, object?> _tag1;
    private KeyValuePair<string, object?> _tag2;
    // ... _tag8까지

    // 9개 이상일 때만 배열 할당
    private KeyValuePair<string, object?>[]? _tags;
}
```

## 성능 비교

### 힙 할당 비교

| 방식 | 태그 4개 기준 | GC Gen0 |
|------|--------------|---------|
| `KeyValuePair[]` | 96 bytes/호출 | 증가 |
| `TagList` | 0 bytes/호출 | 없음 |

### 벤치마크 예시

```
|         Method |      Mean |    Allocated |
|--------------- |----------:|-------------:|
| ArrayTags      | 45.23 ns  |       96 B   |
| TagListTags    | 38.17 ns  |        0 B   |
```

## 적용 예시

### Counter

```csharp
TagList tags = new()
{
    { "operation", "read" },
    { "status", "success" }
};

_requestCounter.Add(1, tags);
```

### Histogram

```csharp
TagList tags = new()
{
    { "operation", "query" },
    { "table", "users" }
};

_durationHistogram.Record(elapsed.TotalSeconds, tags);
```

### ObservableGauge

```csharp
TagList tags = new()
{
    { "pool", "connection" },
    { "database", "primary" }
};

// Measurement에서도 TagList 사용 가능
return new Measurement<int>(activeConnections, tags);
```

## 주의 사항

### 8개 초과 시 할당 발생

```csharp
// 9개 이상의 태그 → 내부 배열 할당 발생
TagList tags = new()
{
    { "tag1", "value1" },
    { "tag2", "value2" },
    // ... 8개까지는 OK
    { "tag9", "value9" }  // 여기서 배열 할당!
};
```

### 권장 사항

- 태그 수를 8개 이하로 유지
- 고빈도 메트릭에서 특히 중요
- OpenTelemetry 권장 사항도 태그 수 최소화

## 관련 API

| 메서드 | TagList 지원 |
|--------|-------------|
| `Counter<T>.Add(T, TagList)` | O |
| `Histogram<T>.Record(T, TagList)` | O |
| `UpDownCounter<T>.Add(T, TagList)` | O |
| `Measurement<T>(T, TagList)` | O |

## 참고 자료

- [Microsoft Learn - TagList Struct](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.taglist)
- [OpenTelemetry .NET Performance](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/metrics/README.md#performance)
- [.NET Metrics Best Practices](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation#best-practices)

## 결론

`TagList` 구조체는 메트릭 태그 전달 시 힙 할당을 제거하여 GC 부담을 최소화합니다. 고빈도 메트릭 기록이 필요한 경우 반드시 `KeyValuePair[]` 대신 `TagList`를 사용하세요.
