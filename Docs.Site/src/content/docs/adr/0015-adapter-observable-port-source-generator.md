---
title: "ADR-0015: Adapter - Observable Port Source Generator"
status: "accepted"
date: 2026-03-20
---

## Context and Problem

Functorium applies Tracing Span creation, structured log recording, and Metrics counter/histogram collection to every port call. This requires writing an Observable decorator for each port interface, and the problem is that this work is entirely repetitive labor.

If 10 ports each have 3-5 methods, 30-50 wrapping methods must be manually written. The more serious issue is maintenance. When a `CancellationToken` parameter is added to `IPaymentPort.ChargeAsync`, the wrapping method in `ObservablePaymentPort` must also be synchronized. If this synchronization is missed, observability silently breaks, and it is only discovered in production when Tracing Spans for that port go missing.

## Considered Options

1. [GenerateObservablePort] Source Generator
2. Runtime reflection proxy (DispatchProxy)
3. Manual decorator writing
4. AOP framework (Castle.DynamicProxy, etc.)

## Decision

**Chosen option: "[GenerateObservablePort] Source Generator"**. By simply attaching a single attribute to a port interface, Tracing/Logging/Metrics wrappers are automatically generated at compile time. When port signatures change, decorators are auto-regenerated on the next build, making synchronization misses structurally impossible, with zero runtime reflection cost.

### Consequences

- <span class="adr-good">Good</span>, because adding a new port only requires a single `[GenerateObservablePort]` attribute line, and Tracing Span, structured log, and Metrics counter/histogram code are auto-generated, making observability omissions impossible.
- <span class="adr-good">Good</span>, because code is generated at compile time as C# source, resulting in zero runtime reflection cost and compatibility with Native AOT environments.
- <span class="adr-good">Good</span>, because the generated `Observable{PortName}.g.cs` files can be opened directly in the IDE for debugging and code review.
- <span class="adr-bad">Bad</span>, because specialized knowledge of the Incremental Generator API, Roslyn symbol analysis, and source text emitting is required, limiting the number of people who can maintain it.
- <span class="adr-bad">Bad</span>, because in some IDEs, IntelliSense or Go to Definition for generated code is not immediately reflected, requiring a build before navigation is available.

### Confirmation

- Verify that `[GenerateObservablePort]` attribute is applied to port interfaces.
- Verify that `Observable{PortName}` classes are generated during build.
- Verify through snapshot tests that generated decorators correctly record Tracing Spans, structured logs, and Metrics counters/histograms.

## Pros and Cons of the Options

### [GenerateObservablePort] Source Generator

- <span class="adr-good">Good</span>, because pure C# code is generated at compile time with zero runtime overhead, and no reflection intervenes in the call path.
- <span class="adr-good">Good</span>, because when port interface method signatures change, decorators are auto-regenerated on the next build, eliminating manual synchronization.
- <span class="adr-good">Good</span>, because generated `.g.cs` files are included in the project, enabling debugger step-in, code review, and snapshot testing.
- <span class="adr-bad">Bad</span>, because understanding Roslyn's Incremental Generator API and symbol model is required, making the generator itself's development barrier high.
- <span class="adr-bad">Bad</span>, because if the generator has bugs, build error messages point to generated code, requiring time to trace the root cause.

### Runtime Reflection Proxy (DispatchProxy)

- <span class="adr-good">Good</span>, because a proxy can be created with a single `DispatchProxy.Create<TInterface, TProxy>()` call, making initial implementation simple.
- <span class="adr-bad">Bad</span>, because every port method call incurs reflection-based `MethodInfo` lookup and parameter boxing, accumulating performance overhead.
- <span class="adr-bad">Bad</span>, because runtime type generation may be restricted in Native AOT environments, potentially not functioning.
- <span class="adr-bad">Bad</span>, because proxy code does not exist in source, preventing debugger step-in and making snapshot testing of correct Tracing/Logging/Metrics recording difficult.

### Manual Decorator Writing

- <span class="adr-good">Good</span>, because written in pure C# without Source Generators or reflection, anyone can understand and modify it.
- <span class="adr-bad">Bad</span>, because 10 ports x 3-5 methods = 30-50 wrapping methods must be manually written and maintained, with code volume growing linearly as ports increase.
- <span class="adr-bad">Bad</span>, because adding parameters to a port interface requires updating the decorator too, and if the compiler does not enforce this, synchronization misses occur silently.
- <span class="adr-bad">Bad</span>, because forgetting to write a decorator when adding a new port means that port's entire observability is missing, discovered only as Tracing omissions in production.

### AOP Framework (Castle.DynamicProxy, etc.)

- <span class="adr-good">Good</span>, because a single Interceptor can apply common Aspects across all ports, making initial setup convenient.
- <span class="adr-bad">Bad</span>, because IL generation at runtime to create proxies incurs app startup initialization cost, and the call path becomes opaque.
- <span class="adr-bad">Bad</span>, because additional external library dependency on Castle.DynamicProxy, etc. is introduced, and the project is affected by that library's update cycle.
- <span class="adr-bad">Bad</span>, because runtime IL generation is blocked in Native AOT environments, conflicting with Functorium's AOT compatibility goals.

## Related Information

- Related commit: `a5027a78` feat(observability): ObservablePortGenerator improvements + framework field naming unification
- Related commit: `81233196` feat(source-generator): LogEnricher source generator implementation
- Related docs: `Docs.Site/src/content/docs/tutorials/sourcegen-observability/`
