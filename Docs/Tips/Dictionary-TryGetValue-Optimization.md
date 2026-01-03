# Dictionary TryGetValue 최적화

## 개요

`Dictionary<TKey, TValue>`에서 키 존재 여부를 확인하고 값을 조회할 때 `TryGetValue` 패턴을 사용하여 불필요한 중복 조회를 방지하는 방법을 설명합니다.

## 문제

`ContainsKey`와 인덱서를 조합하면 동일한 키를 두 번 조회합니다:

```csharp
// 문제: 키를 2번 조회
if (dictionary.ContainsKey(key))        // 1번째 조회
{
    var value = dictionary[key];        // 2번째 조회
    // value 사용
}
```

키 존재 여부만 확인하는 경우에도 동일한 문제가 발생합니다:

```csharp
// 문제: Double-checked locking에서 총 4번 조회 가능
if (dictionary.ContainsKey(key))        // 1번째 조회
    return;

lock (_lock)
{
    if (dictionary.ContainsKey(key))    // 2번째 조회
        return;

    // 초기화 로직
}
```

## 해결 방법

`TryGetValue`를 사용하면 단일 조회로 존재 여부와 값을 동시에 확인합니다:

```csharp
// 해결: 키를 1번만 조회
if (dictionary.TryGetValue(key, out var value))
{
    // value 사용
}
```

키 존재 여부만 확인하는 경우:

```csharp
// 해결: out 매개변수를 _ (discard)로 처리
if (dictionary.TryGetValue(key, out _))
    return;

lock (_lock)
{
    if (dictionary.TryGetValue(key, out _))
        return;

    // 초기화 로직
}
```

## 성능 비교

### 내부 동작 비교

| 패턴 | 해시 계산 | 버킷 조회 | 총 연산 |
|------|----------|----------|--------|
| `ContainsKey` + `[key]` | 2회 | 2회 | 4 |
| `TryGetValue` | 1회 | 1회 | 2 |

### 벤치마크 예시

```
|              Method |     Mean | Allocated |
|-------------------- |---------:|----------:|
| ContainsKey_Index   | 25.43 ns |       0 B |
| TryGetValue         | 14.21 ns |       0 B |
```

## 적용 시나리오

### 값 조회 패턴

```csharp
// 변경 전
if (_cache.ContainsKey(id))
{
    return _cache[id];
}

// 변경 후
if (_cache.TryGetValue(id, out var cachedValue))
{
    return cachedValue;
}
```

### 존재 여부 확인 패턴

```csharp
// 변경 전
if (_initialized.ContainsKey(category))
    return;

// 변경 후
if (_initialized.TryGetValue(category, out _))
    return;
```

### GetOrAdd 패턴

```csharp
// 변경 전
if (!_factories.ContainsKey(type))
{
    _factories[type] = CreateFactory(type);
}
return _factories[type];

// 변경 후
if (!_factories.TryGetValue(type, out var factory))
{
    factory = CreateFactory(type);
    _factories[type] = factory;
}
return factory;
```

### Double-Checked Locking 패턴

```csharp
// 변경 전
private void EnsureInitialized(string key)
{
    if (_data.ContainsKey(key))
        return;

    lock (_lock)
    {
        if (_data.ContainsKey(key))
            return;

        _data[key] = Initialize(key);
    }
}

// 변경 후
private void EnsureInitialized(string key)
{
    if (_data.TryGetValue(key, out _))
        return;

    lock (_lock)
    {
        if (_data.TryGetValue(key, out _))
            return;

        _data[key] = Initialize(key);
    }
}
```

## ConcurrentDictionary 대안

읽기 집약적 워크로드에서는 `ConcurrentDictionary`의 `GetOrAdd`를 고려합니다:

```csharp
// ConcurrentDictionary: 락 없는 읽기 + 원자적 추가
private readonly ConcurrentDictionary<string, MetricsSet> _metrics = new();

public MetricsSet GetMetrics(string category)
{
    return _metrics.GetOrAdd(category, key => CreateMetrics(key));
}
```

| 특성 | Dictionary + lock | ConcurrentDictionary |
|------|------------------|---------------------|
| 읽기 성능 | 락 필요 없음 (외부 락 사용 시) | 락 없음 |
| 쓰기 성능 | 전체 락 | 세분화된 락 |
| 메모리 | 낮음 | 높음 (세분화된 락 구조) |
| 적합 시나리오 | 쓰기 빈번 | 읽기 집약적 |

## 코드 분석 도구

### IDE0020 / IDE0038 (C# 패턴 매칭)

Visual Studio와 Roslyn 분석기가 이 패턴을 감지합니다:

```xml
<!-- .editorconfig -->
dotnet_style_prefer_is_null_check_over_reference_equality_method = true
```

### CA1854 (Dictionary 조회 최적화)

.NET 분석기가 `ContainsKey` + 인덱서 패턴을 감지합니다:

```xml
<!-- .editorconfig -->
dotnet_diagnostic.CA1854.severity = suggestion
```

## 참고 자료

- [Microsoft Learn - Dictionary.TryGetValue](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2.trygetvalue)
- [CA1854: Prefer Dictionary.TryGetValue](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1854)
- [ConcurrentDictionary Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items)

## 결론

`TryGetValue` 패턴은 Dictionary 조회에서 불필요한 중복 연산을 제거합니다. 특히 고빈도 조회가 발생하는 코드에서는 반드시 `ContainsKey` + 인덱서 대신 `TryGetValue`를 사용하세요.
