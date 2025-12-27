# LoggerMessage.Define 제한

## 학습 목표

- LoggerMessage.Define의 6개 파라미터 제한 이해
- 고성능 로깅 vs 폴백 전략
- 파라미터 수 계산 로직

---

## LoggerMessage.Define 소개

### 고성능 로깅

.NET의 `LoggerMessage.Define`은 **제로 할당** 로깅을 제공합니다.

```csharp
// LoggerMessage.Define 사용 (고성능)
private static readonly Action<ILogger, string, int, Exception?> _logUserCreated =
    LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(1, "UserCreated"),
        "User created: {Name}, Age: {Age}");

// 호출
_logUserCreated(logger, "John", 25, null);
```

### 일반 로깅과의 차이

| 특성 | LoggerMessage.Define | logger.LogDebug() |
|------|----------------------|-------------------|
| 메모리 할당 | 제로 할당 | params 배열 할당 |
| 박싱 | 없음 | 값 타입 박싱 |
| 템플릿 파싱 | 컴파일 타임 | 런타임 매 호출 |
| 성능 | 최적화됨 | 오버헤드 있음 |

---

## 6개 파라미터 제한

### .NET 제약

`LoggerMessage.Define`은 **최대 6개**의 제네릭 타입 파라미터만 지원합니다.

```csharp
// ✅ 지원
LoggerMessage.Define<T1>(...)
LoggerMessage.Define<T1, T2>(...)
LoggerMessage.Define<T1, T2, T3>(...)
LoggerMessage.Define<T1, T2, T3, T4>(...)
LoggerMessage.Define<T1, T2, T3, T4, T5>(...)
LoggerMessage.Define<T1, T2, T3, T4, T5, T6>(...)

// ❌ 지원 안 됨
LoggerMessage.Define<T1, T2, T3, T4, T5, T6, T7>(...)  // 7개 이상
```

---

## Pipeline 로깅 필드 계산

### 기본 필드 (4개)

Pipeline은 기본적으로 4개의 필드를 로깅합니다.

```csharp
// 기본 필드
1. ClassName      // "UserRepository"
2. MethodName     // "GetUser"
3. TraceId        // "abc123..."
4. SpanId         // "def456..."
```

### 추가 필드 계산

```
총 필드 수 = 기본 필드(4) + Request 파라미터 수 + 컬렉션 Count 수

예시:
GetValue()                              → 4개        ✅ LoggerMessage.Define
GetFile(int ms)                         → 5개 (4+1)  ✅ LoggerMessage.Define
GetData(int id, string name)            → 6개 (4+2)  ✅ LoggerMessage.Define
GetResult(int a, int b, int c)          → 7개 (4+3)  ❌ logger.LogDebug()
ProcessItems(List<T> items)             → 6개 (4+1+1)✅ LoggerMessage.Define
ProcessData(int id, List<T> data, string name)
                                        → 8개 (4+3+1)❌ logger.LogDebug()
```

---

## 코드 생성 전략

### 파라미터 수 계산

```csharp
// AdapterPipelineGenerator.cs

// ===== LoggerMessage.Define 제약 검사 =====
// .NET의 LoggerMessage.Define<T1, T2, ..., T6>은 최대 6개의 타입 파라미터만 지원합니다.

// 로그 파라미터 수 계산:
// - 기본 4개: ClassName, MethodName, TraceId, SpanId
// - 메서드 파라미터: 각 파라미터당 1개
// - 컬렉션 파라미터: 추가로 Count 필드 1개 (배열/리스트 등)

int baseFieldCount = 4;  // ClassName, MethodName, TraceId, SpanId
int parameterCount = method.Parameters.Count;
int collectionCount = CountCollectionParameters(method);

int totalRequestFields = baseFieldCount + parameterCount + collectionCount;
```

### 컬렉션 파라미터 카운팅

```csharp
private static int CountCollectionParameters(MethodInfo method)
{
    int count = 0;
    foreach (var param in method.Parameters)
    {
        if (CollectionTypeHelper.IsCollectionType(param.Type))
        {
            count++;  // Count 필드 추가
        }
    }
    return count;
}
```

---

## 생성 코드 분기

### 고성능 경로 (≤ 6개)

```csharp
if (totalRequestFields <= 6)
{
    // ✅ 고성능 경로: LoggerMessage.Define 사용
    sb.AppendLine($"    private static readonly Action<ILogger, {typeParams}, Exception?> _log{method.Name}Request =");
    sb.AppendLine($"        LoggerMessage.Define<{typeParams}>(");
    sb.AppendLine($"            LogLevel.Debug,");
    sb.AppendLine($"            new EventId({eventId}, \"{method.Name}Request\"),");
    sb.AppendLine($"            \"{logTemplate}\");");
}
```

### 폴백 경로 (> 6개)

```csharp
else
{
    // ⚠️ 폴백 경로: logger.LogDebug() 직접 사용
    // LoggerMessage.Define의 제약으로 인해 일반 로깅 메서드 사용
    sb.Append("        logger.LogDebug(")
      .Append($"\"{logTemplate}\", ")
      .AppendLine($"{paramValues});");
}
```

---

## 생성 결과 비교

### LoggerMessage.Define 사용 (≤ 6개)

```csharp
// 원본: GetData(int id, string name) - 6개 필드

// 생성된 delegate 필드
private static readonly Action<ILogger, string, string, string, string, int, string, Exception?> _logGetDataRequest =
    LoggerMessage.Define<string, string, string, string, int, string>(
        LogLevel.Debug,
        new EventId(1001, "GetDataRequest"),
        "[{ClassName}.{MethodName}] TraceId={TraceId}, SpanId={SpanId}, Request_Id={Request_Id}, Request_Name={Request_Name}");

// 생성된 호출 코드
_logGetDataRequest(logger, className, methodName, traceId, spanId, id, name, null);
```

### logger.LogDebug() 폴백 (> 6개)

```csharp
// 원본: GetResult(int a, int b, int c) - 7개 필드

// 생성된 호출 코드 (delegate 없음)
logger.LogDebug(
    "[{ClassName}.{MethodName}] TraceId={TraceId}, SpanId={SpanId}, Request_A={Request_A}, Request_B={Request_B}, Request_C={Request_C}",
    className, methodName, traceId, spanId, a, b, c);
```

---

## Response 로깅 필드

### 기본 Response 필드

```csharp
// 기본 필드 (6개)
1. ClassName      // "UserRepository"
2. MethodName     // "GetUser"
3. TraceId        // "abc123..."
4. SpanId         // "def456..."
5. ElapsedMs      // 123.45
6. Result         // 결과값 또는 에러

// 컬렉션 반환 시 추가 필드
7. ResultCount    // 결과 크기 (List, 배열 등)
```

### Response 필드 계산

```csharp
// Response용 필드 계산
int baseResponseFields = 6;  // ClassName, MethodName, TraceId, SpanId, ElapsedMs, Result/Error
bool isCollectionReturn = CollectionTypeHelper.IsCollectionType(actualReturnType);

int totalResponseFields = baseResponseFields + (isCollectionReturn ? 1 : 0);
// 컬렉션 반환: 7개 → 폴백 필요
```

---

## 테스트 시나리오

### 2개 파라미터 테스트 (LoggerMessage.Define)

```csharp
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithTwoParameters()
{
    string input = """
        [GeneratePipeline]
        public class DataRepository : IAdapter
        {
            public virtual FinT<IO, string> GetData(int id, string name)
                => FinT<IO, string>.Succ($"{id}:{name}");
        }
        """;

    string? actual = _sut.Generate(input);

    // LoggerMessage.Define 사용 확인
    actual.ShouldContain("LoggerMessage.Define<");
    actual.ShouldNotContain("logger.LogDebug(");

    return Verify(actual);
}
```

### 3개 파라미터 테스트 (logger.LogDebug 폴백)

```csharp
[Fact]
public Task Should_Generate_LogDebugFallback_WithThreeParameters()
{
    string input = """
        [GeneratePipeline]
        public class DataRepository : IAdapter
        {
            public virtual FinT<IO, string> GetData(int id, string name, bool isActive)
                => FinT<IO, string>.Succ($"{id}:{name}:{isActive}");
        }
        """;

    string? actual = _sut.Generate(input);

    // 기본 4 + 파라미터 3 = 7개 → 폴백
    actual.ShouldContain("logger.LogDebug(");

    return Verify(actual);
}
```

### 0개 파라미터 테스트

```csharp
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithZeroParameters()
{
    string input = """
        [GeneratePipeline]
        public class DataRepository : IAdapter
        {
            public virtual FinT<IO, int> GetValue()
                => FinT<IO, int>.Succ(42);
        }
        """;

    string? actual = _sut.Generate(input);

    // 기본 4개만 → LoggerMessage.Define 사용
    actual.ShouldContain("LoggerMessage.Define<string, string, string, string>");

    return Verify(actual);
}
```

---

## 필드 수 참조표

### Request 로깅

| 메서드 시그니처 | 기본 | 파라미터 | 컬렉션 Count | 총합 | 사용 |
|----------------|------|----------|--------------|------|------|
| `GetValue()` | 4 | 0 | 0 | 4 | Define |
| `GetData(int id)` | 4 | 1 | 0 | 5 | Define |
| `GetData(int id, string name)` | 4 | 2 | 0 | 6 | Define |
| `GetData(int a, int b, int c)` | 4 | 3 | 0 | 7 | LogDebug |
| `Process(List<T> items)` | 4 | 1 | 1 | 6 | Define |
| `Process(List<T> a, int b)` | 4 | 2 | 1 | 7 | LogDebug |

### Response 로깅

| 반환 타입 | 기본 | Count | 총합 | 사용 |
|----------|------|-------|------|------|
| `int` | 6 | 0 | 6 | Define |
| `string` | 6 | 0 | 6 | Define |
| `List<T>` | 6 | 1 | 7 | LogDebug |
| `T[]` | 6 | 1 | 7 | LogDebug |

---

## 요약

| 개념 | 설명 |
|------|------|
| 6개 제한 | .NET LoggerMessage.Define 최대 파라미터 수 |
| 기본 필드 | ClassName, MethodName, TraceId, SpanId (4개) |
| 컬렉션 추가 | 각 컬렉션 파라미터당 Count 필드 +1 |
| 폴백 | 6개 초과 시 logger.LogDebug() 사용 |

| 경로 | 성능 | 할당 |
|------|------|------|
| LoggerMessage.Define | 최적화 | 제로 |
| logger.LogDebug() | 일반 | 있음 |

---

## 다음 단계

다음 장에서는 테스트 전략을 학습합니다.

➡️ [08장. 테스트 전략](../08-testing-strategies/)
