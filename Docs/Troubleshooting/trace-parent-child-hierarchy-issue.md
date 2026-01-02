# Trace Parent-Child 계층 구조 문제 분석

## 문제 요약

Usecase에서 호출하는 Adapter(Repository)의 Span이 Usecase의 자식으로 연결되지 않고, HTTP 요청 레벨의 형제로 생성되는 문제.

## 현상

### 기대한 계층 구조

```
HttpRequestIn (SpanId: 3f1d7ca156583969)          ← ROOT
└── GetAllProductsQuery.Handle (SpanId: eec50f4cfd5ccb87, ParentSpanId: 3f1d7ca156583969)
    └── InMemoryProductRepository.GetAll (ParentSpanId: eec50f4cfd5ccb87) ← Usecase의 자식
```

### 실제 계층 구조

```
HttpRequestIn (SpanId: 3f1d7ca156583969)          ← ROOT
├── GetAllProductsQuery.Handle (SpanId: eec50f4cfd5ccb87, ParentSpanId: 3f1d7ca156583969)
└── InMemoryProductRepository.GetAll (SpanId: 91e174521c6215ae, ParentSpanId: 3f1d7ca156583969) ← 문제!
```

## 디버깅 로그 분석

### 핵심 로그

```
[DEBUG UsecaseTracingPipeline] Activity.Current after StartActivity:
  application usecase.query GetAllProductsQuery.Handle (Id: 00-...-eec50f4cfd5ccb87-01)

[DEBUG GetAllProductsQuery.Usecase.Handle] Activity.Current:
  application usecase.query GetAllProductsQuery.Handle (Id: 00-...-eec50f4cfd5ccb87-01)

[DEBUG OpenTelemetrySpanFactory.CreateChildSpan] Activity.Current:
  application usecase.query GetAllProductsQuery.Handle (Id: 00-...-eec50f4cfd5ccb87-01)  ← 올바른 값!

[DEBUG DetermineParentContext] 1. ActivityContextHolder.GetCurrentActivity(): null
[DEBUG DetermineParentContext] 2. parentContext: ObservabilityContext
[DEBUG DetermineParentContext] -> Using ObservabilityContext (TraceId: ..., SpanId: 3f1d7ca156583969)  ← 잘못된 값 선택!
```

### 문제 원인

1. `Activity.Current`는 올바르게 Usecase Activity(`eec50f4cfd5ccb87`)를 가리킴
2. 그러나 `DetermineParentContext`의 우선순위에서 `parentContext`(ObservabilityContext)가 먼저 매칭됨
3. `IObservabilityContext`는 Scoped로 등록되어 HTTP 요청 시작 시점의 Activity(`3f1d7ca156583969`)를 캡처
4. 결과적으로 Adapter Span이 HTTP 요청 레벨을 부모로 사용

## 현재 우선순위 (문제 있음)

```csharp
private static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
{
    // 1. AsyncLocal에 저장된 Traverse Activity
    Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
    if (traverseActivity != null)
        return traverseActivity.Context;

    // 2. 명시적으로 전달된 parentContext ← 여기서 HTTP 요청 레벨 선택됨
    if (parentContext is ObservabilityContext otelContext)
        return otelContext.ActivityContext;

    // 3. Activity.Current ← 올바른 값이 있지만 도달하지 못함
    Activity? currentActivity = Activity.Current;
    if (currentActivity != null)
        return currentActivity.Context;

    return default;
}
```

## 해결 방안 비교

### 방안 1: UsecaseTracingPipeline에서 ActivityContextHolder에 등록

```csharp
// UsecaseTracingPipeline.Handle()
using Activity? activity = _activitySource.StartActivity(...);

// Activity 생성 후 ActivityContextHolder에 등록
using var _ = ActivityContextHolder.EnterActivity(activity);

TResponse response = await next(request, cancellationToken);
```

**장점:**
- 기존 `DetermineParentContext` 우선순위 유지
- Usecase → Adapter 계층 간 명시적 컨텍스트 전파
- `ActivityContextHolder`의 원래 설계 의도에 부합 (FinT AsyncLocal 문제 우회)

**단점:**
- 모든 Pipeline마다 `ActivityContextHolder` 의존성 추가 필요
- 암묵적인 전역 상태(AsyncLocal) 사용 증가
- Usecase Pipeline 외에 다른 계층에서도 동일한 패턴 필요할 수 있음

**영향 범위:**
- `UsecaseTracingPipeline.cs` 수정
- 필요시 다른 Pipeline도 동일하게 수정

---

### 방안 2: DetermineParentContext 우선순위 변경 (권장)

```csharp
private static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
{
    // 1. Activity.Current - 가장 가까운 부모 (표준 OpenTelemetry 동작)
    Activity? currentActivity = Activity.Current;
    if (currentActivity != null)
        return currentActivity.Context;

    // 2. AsyncLocal - FinT 비동기 컨텍스트 복원 문제 우회
    Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
    if (traverseActivity != null)
        return traverseActivity.Context;

    // 3. 명시적 parentContext - 외부에서 주입된 컨텍스트
    if (parentContext is ObservabilityContext otelContext)
        return otelContext.ActivityContext;

    return default;
}
```

**장점:**
- 가장 간단한 수정 (한 곳만 변경)
- OpenTelemetry의 기본 동작과 일치
- 추가 의존성 없음
- 기존 코드 변경 최소화

**단점:**
- `parentContext` 매개변수의 의미가 약해짐 (폴백으로만 사용)
- FinT의 AsyncLocal 복원 문제가 있는 경우 `Activity.Current`가 null일 수 있음

**영향 범위:**
- `OpenTelemetrySpanFactory.cs`의 `DetermineParentContext` 메서드만 수정

**우선순위 의미:**
| 우선순위 | 소스 | 용도 |
|---------|------|------|
| 1 | `Activity.Current` | 현재 실행 컨텍스트의 가장 가까운 부모 (동기적 흐름) |
| 2 | `ActivityContextHolder` | FinT/IO 모나드의 AsyncLocal 복원 문제 우회용 |
| 3 | `parentContext` | 명시적으로 전달된 외부 컨텍스트 (HTTP 요청 레벨) |

---

### 방안 3: IObservabilityContextAccessor 패턴

```csharp
// 새로운 인터페이스
public interface IObservabilityContextAccessor
{
    IObservabilityContext? Context { get; }
    void SetContext(IObservabilityContext? context);
}

// Scoped 구현
public class ObservabilityContextAccessor : IObservabilityContextAccessor
{
    public IObservabilityContext? Context { get; private set; }
    public void SetContext(IObservabilityContext? context) => Context = context;
}

// UsecaseTracingPipeline에서 사용
public class UsecaseTracingPipeline<TRequest, TResponse>
{
    private readonly IObservabilityContextAccessor _contextAccessor;

    public async ValueTask<TResponse> Handle(...)
    {
        using Activity? activity = _activitySource.StartActivity(...);

        // 컨텍스트 업데이트
        _contextAccessor.SetContext(new ObservabilityContext(activity.Context));

        TResponse response = await next(request, cancellationToken);
        return response;
    }
}
```

**장점:**
- 명시적인 컨텍스트 전파
- DI를 통한 깔끔한 의존성 관리
- 테스트 용이성 향상

**단점:**
- 새로운 인터페이스와 구현체 필요
- 기존 `IObservabilityContext` 등록 방식 변경 필요
- 구현 복잡도 증가
- DI 등록 순서에 주의 필요

**영향 범위:**
- 새 인터페이스/클래스 추가
- `Program.cs` DI 등록 변경
- `UsecaseTracingPipeline` 수정
- Source Generator 템플릿 수정 (IObservabilityContext → IObservabilityContextAccessor)

## 권장 사항

### 1차 권장: 방안 2

**이유:**
1. **최소 변경 원칙**: 한 곳(`DetermineParentContext`)만 수정
2. **OpenTelemetry 표준**: `Activity.Current`는 OpenTelemetry가 자동 관리하는 표준 메커니즘
3. **기존 코드 호환성**: `ActivityContextHolder`와 `parentContext`는 폴백으로 유지

### 2차 권장: 방안 1 (방안 2 실패 시)

FinT/IO 모나드 실행 중 `Activity.Current`가 null로 복원되는 경우 방안 1 적용.

## 검증 방법

### 1. 빌드 및 실행

```bash
cd Tutorials/Cqrs04Endpoint/Src/Cqrs04Endpoint.WebApi
dotnet build
dotnet run
```

### 2. API 호출

```bash
curl http://localhost:5000/api/products
```

### 3. 콘솔 출력 확인

```
Activity.TraceId:            0816f34d577acd3737bb462f5e61175b
Activity.SpanId:             [NEW_SPAN_ID]
Activity.ParentSpanId:       eec50f4cfd5ccb87  ← Usecase의 SpanId여야 함
Activity.DisplayName:        adapter Repository InMemoryProductRepository.GetAll
```

### 4. 계층 구조 확인

Jaeger 또는 Zipkin에서 트레이스 확인:
- `InMemoryProductRepository.GetAll`이 `GetAllProductsQuery.Handle`의 자식으로 표시되어야 함

## 관련 파일

| 파일 | 역할 |
|------|------|
| `OpenTelemetrySpanFactory.cs` | Adapter Span 생성, 부모 컨텍스트 결정 |
| `UsecaseTracingPipeline.cs` | Usecase Activity 생성 |
| `ActivityContextHolder.cs` | AsyncLocal 기반 Activity 컨텍스트 저장 |
| `ObservabilityContext.cs` | HTTP 요청 레벨 Activity 컨텍스트 래퍼 |
| `InMemoryProductRepositoryPipeline.g.cs` | Source Generator가 생성한 Adapter Pipeline |

## 디버깅 코드 제거

문제 해결 후 다음 파일에서 `Console.WriteLine` 디버깅 코드 제거:

1. `UsecaseTracingPipeline.cs`
2. `OpenTelemetrySpanFactory.cs`
3. `GetAllProductsQuery.cs`

## 참고 자료

- [OpenTelemetry .NET Activity API](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
- [System.Diagnostics.Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity)
- [AsyncLocal and ExecutionContext](https://learn.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1)
