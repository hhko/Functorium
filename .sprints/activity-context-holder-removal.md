# Observability 컨텍스트 전파 단순화

**작성일**: 2026-01-09
**상태**: ✅ 완료

## 개요

LanguageExt 5.0.0-beta-77 환경에서 `Activity.Current`(AsyncLocal 기반)가 IO/FinT 모나드 실행 중에도 올바르게 유지되는지 검증하고, 불필요한 코드를 제거한 기술 노트입니다.

### 제거된 항목
1. **ActivityContextHolder** - 불필요한 AsyncLocal 수동 관리
2. **IContextPropagator / ActivityContextPropagator** - 사용되지 않는 인터페이스 및 구현체

---

## 1. 배경

### 기존 가정
`ActivityContextHolder`는 다음 가정 하에 작성되었습니다:

> LanguageExt의 IO/FinT 모나드가 내부적으로 `Task.Factory.StartNew(LongRunning)`을 사용하여
> `ExecutionContext`를 캡처하지 않아 `Activity.Current`(AsyncLocal)가 손실된다.

### 기존 구현 (3-tier fallback)
```csharp
// OpenTelemetrySpanFactory.DetermineParentContext()
internal static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
{
    // 1. Activity.Current
    if (Activity.Current != null)
        return Activity.Current.Context;

    // 2. ActivityContextHolder.GetCurrentActivity() ← 이 부분이 필요한가?
    Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
    if (traverseActivity != null)
        return traverseActivity.Context;

    // 3. parentContext
    if (parentContext is ObservabilityContext otelContext)
        return otelContext.ActivityContext;

    return default;
}
```

---

## 2. 검증 프로젝트

### AsyncLocalVerification
**경로**: `Tutorials/AsyncLocalVerification/`

LanguageExt 5.0.0-beta-77에서 AsyncLocal 동작을 검증하기 위한 독립 콘솔 프로젝트를 생성했습니다.

### 테스트 시나리오 (11개)

| 테스트 | 설명 | 결과 |
|--------|------|------|
| Test 1 | Basic async/await | ✅ PASS |
| Test 2 | FinT<IO>.Run().RunAsync() without Fork | ✅ PASS |
| Test 3 | IO.liftAsync with nested RunAsync | ✅ PASS |
| Test 4 | IO.Fork() | ✅ PASS |
| Test 5 | Custom AsyncLocal comparison | ✅ PASS |
| Test 6 | ConfigureAwait(false) | ✅ PASS |
| Test 7 | ThreadPool.QueueUserWorkItem | ✅ PASS |
| Test 8 | TraverseSerial-like pattern | ✅ PASS |
| Test 9 | Deep nesting with real async | ✅ PASS |
| Test 10 | ExecutionContext.SuppressFlow() | ✅ PASS (예상과 다름) |
| Test 11 | ThreadPool.UnsafeQueueUserWorkItem() | ❌ FAIL (예상됨) |

### 핵심 발견

```
=== 검증 결과 ===
- Activity.Current는 LanguageExt IO/FinT 실행에서 손실되지 않음
- Fork()를 포함한 모든 시나리오에서 PASS
- ExecutionContext.SuppressFlow()도 예상과 달리 PASS (Task.Run 내부에서 유지됨)
- ThreadPool.UnsafeQueueUserWorkItem()만 유일하게 실패
- LanguageExt는 UnsafeQueueUserWorkItem을 사용하지 않음
```

### SuppressFlow 테스트 분석

**예상**: `ExecutionContext.SuppressFlow()` 호출 후 `Task.Run()`을 실행하면 ExecutionContext가 전파되지 않아 Activity.Current가 null이 될 것으로 예상

**실제 결과**: Activity.Current가 유지됨 (PASS)

```csharp
// Test 10 코드
var afc = ExecutionContext.SuppressFlow();
try
{
    var task = Task.Run(() =>
    {
        // 예상: Activity.Current == null
        // 실제: Activity.Current == "Test10" (유지됨)
        capturedInTask = Activity.Current?.DisplayName ?? "null";
    });
    task.Wait();
}
finally
{
    afc.Undo();
}
```

**분석**: `Task.Run()`이 내부적으로 ExecutionContext를 캡처하는 시점이 `SuppressFlow()` 호출 전일 수 있거나, .NET 런타임의 최적화로 인해 동일 스레드에서 실행될 때 컨텍스트가 유지될 수 있음. 중요한 점은 LanguageExt가 이러한 패턴을 사용하지 않으므로 실제 영향이 없다는 것.

---

## 3. ASP.NET Core 통합 테스트

### ActivityCurrentIntegrationTests
**경로**: `Tutorials/Cqrs04Endpoint/Tests/Cqrs04Endpoint.WebApi.Tests.Unit/ActivityCurrentIntegrationTests.cs`

실제 HTTP 요청을 통해 Activity 계층이 올바르게 형성되는지 검증합니다.

```csharp
[Fact]
public async Task ActivityCurrent_IsPreservedThroughPipeline_RealHttpRequest()
{
    // 실제 HTTP 요청 → Usecase → Adapter 흐름에서
    // Activity.Current가 올바르게 유지되는지 검증
}
```

### 검증된 Trace 계층 구조

```
Microsoft.AspNetCore.Hosting.HttpRequestIn (ROOT)
└── application usecase.command CreateProductCommand.Handle
    ├── adapter Repository InMemoryProductRepository.ExistsByName
    └── adapter Repository InMemoryProductRepository.Create
```

**결과**: 모든 Activity가 동일한 TraceId를 공유하고 올바른 부모-자식 관계를 형성함

---

## 4. 코드 변경 사항

### 삭제된 파일

| 파일 | 이유 |
|------|------|
| `Src/Functorium/Adapters/Observabilities/Context/ActivityContextHolder.cs` | .NET ExecutionContext가 자동으로 AsyncLocal 전파 |
| `Src/Functorium/Applications/Observabilities/Context/IContextPropagator.cs` | DI에 등록되었으나 실제 사용되지 않음 |
| `Src/Functorium/Adapters/Observabilities/Context/ActivityContextPropagator.cs` | DI에 등록되었으나 실제 사용되지 않음 |

### 수정된 파일

#### OpenTelemetrySpanFactory.cs
```diff
- // 3-tier fallback
- // 1. Activity.Current
- // 2. ActivityContextHolder.GetCurrentActivity()
- // 3. parentContext

+ // 2-tier fallback
+ // 1. Activity.Current
+ // 2. parentContext
```

**변경 전**:
```csharp
internal static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
{
    if (Activity.Current != null)
        return Activity.Current.Context;

    Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
    if (traverseActivity != null)
        return traverseActivity.Context;

    if (parentContext is ObservabilityContext otelContext)
        return otelContext.ActivityContext;

    return default;
}
```

**변경 후**:
```csharp
internal static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
{
    // .NET의 ExecutionContext가 AsyncLocal을 자동으로 전파하므로
    // Activity.Current가 올바르게 유지됩니다.
    if (Activity.Current != null)
        return Activity.Current.Context;

    if (parentContext is ObservabilityContext otelContext)
        return otelContext.ActivityContext;

    return default;
}
```

#### FinTUtilites.cs (TraverseSerial)
```diff
- using (ActivityContextHolder.EnterActivity(activity))
- {
-     Fin<B> finResult = await f(item).Run().RunAsync();
- }

+ // .NET의 ExecutionContext가 Activity.Current를 자동으로 전파하므로
+ // 추가적인 AsyncLocal 관리가 필요하지 않습니다.
+ Fin<B> finResult = await f(item).Run().RunAsync();
```

#### OpenTelemetryBuilder.cs (DI 등록 제거)
```diff
- // IContextPropagator 등록 (Singleton)
- _services.AddSingleton<IContextPropagator, ActivityContextPropagator>();
```

IContextPropagator는 DI에 등록되었으나 어디에서도 주입받거나 사용되지 않아 제거되었습니다.

#### 테스트 파일
- `OpenTelemetrySpanFactoryTests.cs` - ActivityContextHolder 관련 테스트 제거
- `TraceHierarchyUsecaseTests.cs` - ActivityContextHolder 관련 테스트 제거

---

## 5. 기술적 근거

### .NET ExecutionContext 동작

```csharp
// Activity.Current의 내부 구현 (System.Diagnostics.Activity)
private static readonly AsyncLocal<Activity?> s_current = new();

public static Activity? Current
{
    get => s_current.Value;
    set => s_current.Value = value;
}
```

### ExecutionContext 전파 규칙

| API | ExecutionContext 전파 | 비고 |
|-----|----------------------|------|
| `Task.Run()` | ✅ 전파됨 | |
| `async/await` | ✅ 전파됨 | |
| `Task.Factory.StartNew()` | ✅ 전파됨 | |
| `ThreadPool.QueueUserWorkItem()` | ✅ 전파됨 | |
| `ConfigureAwait(false)` | ✅ 전파됨 | SyncContext만 영향 |
| `ExecutionContext.SuppressFlow()` + `Task.Run()` | ✅ 전파됨 | 테스트 결과 (아래 참조) |
| `ThreadPool.UnsafeQueueUserWorkItem()` | ❌ 전파 안됨 | 유일하게 실패 |

### SuppressFlow + Task.Run 동작 분석

일반적으로 `ExecutionContext.SuppressFlow()`는 이후 시작되는 비동기 작업으로 ExecutionContext가 전파되는 것을 억제합니다. 그러나 테스트 결과 `Task.Run()`과 함께 사용할 때 Activity.Current가 유지되었습니다.

**가능한 원인:**
1. **Task.Run 내부 최적화**: Task.Run이 동일 스레드에서 인라인 실행될 때 컨텍스트가 유지될 수 있음
2. **ExecutionContext 캡처 시점**: Task 객체 생성 시점에 이미 ExecutionContext가 캡처되었을 수 있음
3. **.NET 런타임 버전**: 런타임 버전에 따라 동작이 다를 수 있음

**결론**: `ThreadPool.UnsafeQueueUserWorkItem()`만이 확실하게 ExecutionContext를 전파하지 않으며, LanguageExt는 이 API를 사용하지 않음.

### LanguageExt 5.0.0-beta-77

LanguageExt는 `ExecutionContext`를 억제하는 API를 사용하지 않으므로:
- IO monad의 `RunAsync()` → ExecutionContext 전파됨
- FinT monad의 `Run().RunAsync()` → ExecutionContext 전파됨
- `Fork()` (Task.Factory.StartNew 사용) → ExecutionContext 전파됨

---

## 6. 결론

### 검증 결과
> **Activity.Current는 LanguageExt 5.0.0-beta-77에서 손실되지 않습니다.**

.NET의 `ExecutionContext`가 `async/await`를 통해 `AsyncLocal`을 자동으로 전파하기 때문입니다.

### 제거된 코드
- `ActivityContextHolder` 클래스 (불필요)
- 관련 fallback 로직 (불필요)
- 관련 테스트 (더 이상 유효하지 않음)

### 단순화된 아키텍처

```
Before:                              After:
┌─────────────────────┐              ┌─────────────────────┐
│ Activity.Current    │──┐           │ Activity.Current    │──┐
├─────────────────────┤  │           ├─────────────────────┤  │
│ ActivityContextHolder│──┼─→ Parent │ (제거됨)            │  ├─→ Parent
├─────────────────────┤  │           ├─────────────────────┤  │
│ parentContext       │──┘           │ parentContext       │──┘
└─────────────────────┘              └─────────────────────┘
```

### 테스트 결과
- 전체 솔루션 테스트 통과
- ASP.NET Core 통합 테스트 통과
- Trace 계층 구조 동일성 확인

---

## 7. 관련 파일

| 파일 | 설명 |
|------|------|
| [AsyncLocalVerification/Program.cs](../Tutorials/AsyncLocalVerification/Program.cs) | AsyncLocal 검증 프로젝트 |
| [ActivityCurrentIntegrationTests.cs](../Tutorials/Cqrs04Endpoint/Tests/Cqrs04Endpoint.WebApi.Tests.Unit/ActivityCurrentIntegrationTests.cs) | ASP.NET Core 통합 테스트 |
| [OpenTelemetrySpanFactory.cs](../Src/Functorium/Adapters/Observabilities/Spans/OpenTelemetrySpanFactory.cs) | 단순화된 부모 컨텍스트 결정 로직 |
| [FinTUtilites.cs](../Src/Functorium/Applications/Linq/FinTUtilites.cs) | 단순화된 TraverseSerial |
| [OpenTelemetryBuilder.cs](../Src/Functorium/Adapters/Observabilities/Builders/OpenTelemetryBuilder.cs) | DI 등록 단순화 |

---

## 8. 참고 문서

- [trace-parent-child-hierarchy-issue.md](../Docs/Troubleshooting/trace-parent-child-hierarchy-issue.md) - 원래 문제 분석 문서
- [functorium-observability-implementation.md](./functorium-observability-implementation.md) - Observability 구현 분석
