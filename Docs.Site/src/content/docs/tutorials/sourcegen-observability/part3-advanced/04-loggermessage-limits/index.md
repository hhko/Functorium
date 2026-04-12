---
title: "LoggerMessage.Define Limits"
---

## Overview

There is a trade-off between high-performance logging and flexibility. .NET's `LoggerMessage.Define` provides zero-allocation logging but supports a maximum of 6 type parameters. ObservablePortGenerator uses the high-performance path when the total of 4 base fields (layer, category, handler, method) plus method parameters and collection Count fields is 6 or fewer, and automatically falls back to `logger.LogDebug()` when exceeded. Thanks to this branching logic, developers get the optimal logging strategy applied without being conscious of parameter counts.

## Learning Objectives

### Core Learning Objectives
1. **Understanding LoggerMessage.Define's 6-parameter limit**
   - The generic type parameter ceiling imposed by the .NET runtime
2. **High-performance logging vs fallback strategy**
   - Performance differences between the zero-allocation path and the regular logging path
3. **Parameter count calculation logic**
   - How base fields, method parameters, and collection Counts are summed

---

## Introducing LoggerMessage.Define

### High-Performance Logging

.NET's `LoggerMessage.Define` provides zero-allocation logging.

```csharp
// Using LoggerMessage.Define (high-performance)
private static readonly Action<ILogger, string, int, Exception?> _logUserCreated =
    LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(1, "UserCreated"),
        "User created: {Name}, Age: {Age}");

// Call
_logUserCreated(logger, "John", 25, null);
```

### Differences from Regular Logging

| Characteristic | LoggerMessage.Define | logger.LogDebug() |
|---------------|----------------------|-------------------|
| Memory allocation | Zero allocation | params array allocation |
| Boxing | None | Value type boxing |
| Template parsing | Compile time | Runtime per call |
| Performance | Optimized | Has overhead |

---

## The 6-Parameter Limit

### .NET Constraint

`LoggerMessage.Define` supports a **maximum of 6** generic type parameters.

```csharp
// ✅ Supported
LoggerMessage.Define<T1>(...)
LoggerMessage.Define<T1, T2>(...)
LoggerMessage.Define<T1, T2, T3>(...)
LoggerMessage.Define<T1, T2, T3, T4>(...)
LoggerMessage.Define<T1, T2, T3, T4, T5>(...)
LoggerMessage.Define<T1, T2, T3, T4, T5, T6>(...)

// ❌ Not supported
LoggerMessage.Define<T1, T2, T3, T4, T5, T6, T7>(...)  // 7 or more
```

---

## Observable Logging Field Calculation

### Base Fields (4)

Observable logs 4 fields by default.

```csharp
// Base fields
1. requestLayer           // "adapter"
2. requestCategory        // "repository"
3. requestHandler         // "UserRepository"
4. requestHandlerMethod   // "GetUser"
```

### Additional Field Calculation

```
Total fields = Base fields(4) + Request parameter count + Collection Count count

Examples:
GetValue()                              -> 4          ✅ LoggerMessage.Define
GetFile(int ms)                         -> 5 (4+1)   ✅ LoggerMessage.Define
GetData(int id, string name)            -> 6 (4+2)   ✅ LoggerMessage.Define
GetResult(int a, int b, int c)          -> 7 (4+3)   ❌ logger.LogDebug()
ProcessItems(List<T> items)             -> 6 (4+1+1) ✅ LoggerMessage.Define
ProcessData(int id, List<T> data, string name)
                                        -> 8 (4+3+1) ❌ logger.LogDebug()
```

---

## Code Generation Strategy

### Parameter Count Calculation

```csharp
// ObservablePortGenerator.cs

// ===== LoggerMessage.Define constraint check =====
// .NET's LoggerMessage.Define<T1, T2, ..., T6> supports a maximum of 6 type parameters.

// Log parameter count calculation:
// - Base 4: requestLayer, requestCategory, requestHandler, requestHandlerMethod
// - Method parameters: 1 per parameter
// - Collection parameters: additional 1 Count field (arrays/lists etc.)

int baseFieldCount = 4;  // requestLayer, requestCategory, requestHandler, requestHandlerMethod
int parameterCount = method.Parameters.Count;
int collectionCount = CountCollectionParameters(method);

int totalRequestFields = baseFieldCount + parameterCount + collectionCount;
```

### Collection Parameter Counting

```csharp
private static int CountCollectionParameters(MethodInfo method)
{
    int count = 0;
    foreach (var param in method.Parameters)
    {
        if (CollectionTypeHelper.IsCollectionType(param.Type))
        {
            count++;  // Count field added
        }
    }
    return count;
}
```

---

## Generated Code Branching

### High-Performance Path (<= 6)

```csharp
if (totalRequestFields <= 6)
{
    // ✅ High-performance path: use LoggerMessage.Define
    sb.AppendLine($"    private static readonly Action<ILogger, {typeParams}, Exception?> _logAdapterRequestDebug_{classInfo.ClassName}_{method.Name} =");
    sb.AppendLine($"        LoggerMessage.Define<{typeParams}>(");
    sb.AppendLine($"            LogLevel.Debug,");
    sb.AppendLine($"            ObservabilityNaming.EventIds.Adapter.AdapterRequest,");
    sb.AppendLine($"            \"{logTemplate}\");");
}
```

### Fallback Path (> 6)

```csharp
else
{
    // ⚠️ Fallback path: use logger.LogDebug() directly
    // Uses regular logging method due to LoggerMessage.Define constraint
    sb.Append("        logger.LogDebug(")
      .Append($"\"{logTemplate}\", ")
      .AppendLine($"{paramValues});");
}
```

---

## Generated Result Comparison

### Using LoggerMessage.Define (<= 6)

```csharp
// Original: GetData(int id, string name) - 6 fields

// Generated delegate field
private static readonly Action<ILogger, string, string, string, string, int, string, Exception?> _logAdapterRequestDebug_DataRepository_GetData =
    LoggerMessage.Define<string, string, string, string, int, string>(
        LogLevel.Debug,
        ObservabilityNaming.EventIds.Adapter.AdapterRequest,
        "{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {request.params.id} {request.params.name}");

// Generated call code (extension method form)
_logger.LogAdapterRequestDebug_DataRepository_GetData(layer, category, handler, method, id, name, null);
```

### logger.LogDebug() Fallback (> 6)

```csharp
// Original: GetResult(int a, int b, int c) - 7 fields

// Generated call code (no delegate)
logger.LogDebug(
    "{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {request.params.a} {request.params.b} {request.params.c}",
    layer, category, handler, method, a, b, c);
```

---

## Response Logging Fields

### Base Response Fields

```csharp
// Base fields (6)
1. requestLayer           // "adapter"
2. requestCategory        // "repository"
3. requestHandler         // "UserRepository"
4. requestHandlerMethod   // "GetUser"
5. status                 // "success" or "failure"
6. elapsed                // 0.0123 (in seconds)

// Additional field when returning collection
7. response.count         // Result size (List, array, etc.)
```

### Response Field Calculation

```csharp
// Response field calculation
int baseResponseFields = 6;  // requestLayer, requestCategory, requestHandler, requestHandlerMethod, status, elapsed
bool isCollectionReturn = CollectionTypeHelper.IsCollectionType(actualReturnType);

int totalResponseFields = baseResponseFields + (isCollectionReturn ? 1 : 0);
// Collection return: 7 -> fallback needed
```

---

## Test Scenarios

### 2-Parameter Test (LoggerMessage.Define)

```csharp
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithTwoParameters()
{
    string input = """
        [GenerateObservablePort]
        public class DataRepository : IObservablePort
        {
            public virtual FinT<IO, string> GetData(int id, string name)
                => FinT<IO, string>.Succ($"{id}:{name}");
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify LoggerMessage.Define is used
    actual.ShouldContain("LoggerMessage.Define<");
    actual.ShouldNotContain("logger.LogDebug(");

    return Verify(actual);
}
```

### 3-Parameter Test (logger.LogDebug Fallback)

```csharp
[Fact]
public Task Should_Generate_LogDebugFallback_WithThreeParameters()
{
    string input = """
        [GenerateObservablePort]
        public class DataRepository : IObservablePort
        {
            public virtual FinT<IO, string> GetData(int id, string name, bool isActive)
                => FinT<IO, string>.Succ($"{id}:{name}:{isActive}");
        }
        """;

    string? actual = _sut.Generate(input);

    // Base 4 + parameters 3 = 7 -> fallback
    actual.ShouldContain("logger.LogDebug(");

    return Verify(actual);
}
```

### 0-Parameter Test

```csharp
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithZeroParameters()
{
    string input = """
        [GenerateObservablePort]
        public class DataRepository : IObservablePort
        {
            public virtual FinT<IO, int> GetValue()
                => FinT<IO, int>.Succ(42);
        }
        """;

    string? actual = _sut.Generate(input);

    // Only base 4 -> LoggerMessage.Define used
    actual.ShouldContain("LoggerMessage.Define<string, string, string, string>");

    return Verify(actual);
}
```

---

## Field Count Reference Table

### Request Logging

| Method Signature | Base | Parameters | Collection Count | Total | Used |
|-----------------|------|------------|------------------|-------|------|
| `GetValue()` | 4 | 0 | 0 | 4 | Define |
| `GetData(int id)` | 4 | 1 | 0 | 5 | Define |
| `GetData(int id, string name)` | 4 | 2 | 0 | 6 | Define |
| `GetData(int a, int b, int c)` | 4 | 3 | 0 | 7 | LogDebug |
| `Process(List<T> items)` | 4 | 1 | 1 | 6 | Define |
| `Process(List<T> a, int b)` | 4 | 2 | 1 | 7 | LogDebug |

### Response Logging

| Return Type | Base | Count | Total | Used |
|------------|------|-------|-------|------|
| `int` | 6 | 0 | 6 | Define |
| `string` | 6 | 0 | 6 | Define |
| `List<T>` | 6 | 1 | 7 | LogDebug |
| `T[]` | 6 | 1 | 7 | LogDebug |

---

## Summary at a Glance

ObservablePortGenerator automatically calculates the total logging parameters and selects the optimal path. When the sum of the 4 base fields plus method parameters and collection Count fields is 6 or fewer, it uses the zero-allocation `LoggerMessage.Define` path; when exceeded, it uses the `logger.LogDebug()` fallback path. Response logging follows the same principle, where fallback occurs when a collection return's Count field is added to the base 6 fields.

---

## FAQ

### Q1: What .NET constraint gives rise to `LoggerMessage.Define`'s 6-parameter limit?
**A**: `LoggerMessage.Define` generates delegates of the form `Action<ILogger, T1, ..., T6, Exception?>`. While the .NET runtime's `Action<>` generic delegate supports up to 16 type parameters, the `LoggerMessage` class provides overloads only up to 6 for the balance of performance and API complexity.

### Q2: How significant is the performance difference of the fallback path (`logger.LogDebug()`) in practice?
**A**: `LoggerMessage.Define` performs log template parsing only once at compile time with no value type boxing, while `logger.LogDebug()` allocates a `params object[]` array and boxes value types on every call. In high-throughput systems with tens of thousands of logs per second, the difference is significant, but for most adapter calls where I/O latency is dominant, the practical impact is minimal.

### Q3: Why are Count fields for collection parameters included in the total field count?
**A**: When a `List<string> items` parameter exists, two fields are added to the logging message: `{request.params.items}` and `{request.params.items.count}`. Since the Count field also occupies one type parameter of `LoggerMessage.Define`, the base 4 + parameter 1 + Count 1 = 6, reaching the boundary value.

---

We have now covered all the core code generation logic of ObservablePortGenerator. The next section covers how to set up a unit test environment to verify this generator.

-> [05. Unit Test Setup](../05-Unit-Testing-Setup/)
