---
title: "Reflection vs Source Generator"
---

Compares reflection-based logging with the LoggerMessage.Define (source generator) approach.

## Run

```bash
dotnet run --project ReflectionVsSourceGen
```

---

## FAQ

### Q1: How significant is the performance impact of reflection-based logging?
**A**: Reflection queries type metadata on every call, which can result in a 100x or greater performance difference. In high-frequency call paths, GC pressure also increases, affecting overall application response time.

### Q2: Why is `LoggerMessage.Define` high-performance?
**A**: `LoggerMessage.Define` parses the log message template only once at compile time and caches it as a delegate. During logging calls, values are passed directly without string interpolation or boxing, resulting in zero allocations and high performance.

### Q3: What preparation is needed to run this project?
**A**: With the .NET SDK installed, you can run it immediately with the `dotnet run --project ReflectionVsSourceGen` command. Through the benchmark results, you can directly observe the performance difference between the reflection approach and the source generator approach.
