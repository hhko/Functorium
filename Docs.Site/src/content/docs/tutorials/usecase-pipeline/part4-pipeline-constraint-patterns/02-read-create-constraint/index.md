---
title: "Read+Create Constraint"
---

## 개요

앞 장에서 Create 전용 제약을 적용했습니다. 이번에는 응답 상태를 읽는 능력까지 함께 요구하는 패턴을 살펴봅니다. Logging, Tracing, Metrics Pipeline은 핸들러 실행 **후** 응답의 성공/실패 상태를 **읽어야** 합니다. 또한 예외 발생 시 실패 응답을 **생성**할 수도 있어야 합니다. 이 장에서는 Read와 Create를 모두 요구하는 이중 제약 패턴을 학습합니다.

```
Pipeline 동작 흐름:

Logging Pipeline:
  response = next()
  response.IsSucc? ──Yes──→ Log("Success")    ← 읽기 필요 (IFinResponse)
                   │
                   No───→ Log("Fail: ...")     ← Error 접근 (IFinResponseWithError)

Tracing Pipeline:
  response = next()
  Tags.Add("status:" + ...)                    ← 읽기 필요 (IFinResponse)
  response is IFinResponseWithError?           ← Error 접근 (패턴 매칭)
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Read+Create 이중 제약이 필요한 Pipeline을 식별할 수 있습니다
2. `IFinResponse`(읽기)와 `IFinResponseFactory<TResponse>`(생성)의 역할 차이를 설명할 수 있습니다
3. Error 접근에 패턴 매칭(`is IFinResponseWithError`)을 사용하는 이유를 설명할 수 있습니다

## 핵심 개념

### 1. Read+Create 이중 제약

Pipeline이 응답의 상태를 읽고(Read), 필요 시 실패 응답을 생성(Create)하는 경우에 해당합니다.

```csharp
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

이중 제약이 부여하는 능력을 정리하면 다음과 같습니다.

| 능력 | 인터페이스 | 용도 |
|------|-----------|------|
| Read | `IFinResponse` | `response.IsSucc`, `response.IsFail` |
| Create | `IFinResponseFactory<TResponse>` | `TResponse.CreateFail(error)` |
| Error 접근 | `IFinResponseWithError` (패턴 매칭) | `response is IFinResponseWithError fail` |

### 2. Logging Pipeline

Logging Pipeline은 응답 상태에 따라 다른 로그를 기록합니다:

```csharp
public sealed class SimpleLoggingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Logs { get; } = [];

    public TResponse LogAndReturn(TResponse response)
    {
        if (response.IsSucc)
        {
            Logs.Add("Success");
        }
        else
        {
            if (response is IFinResponseWithError fail)
                Logs.Add($"Fail: {fail.Error}");
            else
                Logs.Add("Fail: unknown");
        }
        return response;
    }
}
```

- `response.IsSucc` / `response.IsFail`: `IFinResponse` 제약 덕분에 직접 접근 가능
- `response is IFinResponseWithError fail`: 패턴 매칭으로 Error 정보 접근

### 3. Tracing Pipeline

Tracing Pipeline은 응답 상태를 태그로 기록합니다:

```csharp
public sealed class SimpleTracingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Tags { get; } = [];

    public TResponse TraceAndReturn(TResponse response)
    {
        Tags.Add($"status:{(response.IsSucc ? "ok" : "error")}");

        if (response is IFinResponseWithError fail)
            Tags.Add($"error.message:{fail.Error}");

        return response;
    }
}
```

### 4. Error 접근은 패턴 매칭으로

`IFinResponseWithError`는 제약 조건에 포함하지 않습니다. 대신 **패턴 매칭**으로 런타임에 확인합니다:

```csharp
// 제약에 IFinResponseWithError를 추가하지 않음
// 성공 응답도 이 Pipeline을 통과해야 하므로

if (response is IFinResponseWithError fail)
{
    // Fail인 경우에만 Error 접근
    var error = fail.Error;
}
```

이렇게 하는 이유:
- `IFinResponseWithError`는 `FinResponse<A>.Fail`에서만 구현됨
- 성공 응답(`Succ`)은 이 인터페이스를 구현하지 않음
- 제약에 추가하면 성공 응답이 Pipeline을 통과할 수 없음

## FAQ

### Q1: `IFinResponseWithError`를 제약 조건에 추가하지 않고 패턴 매칭으로 접근하는 이유는 무엇인가요?
**A**: `IFinResponseWithError`는 **Fail 케이스에서만** 구현됩니다. 이를 제약에 추가하면 성공 응답(`Succ`)은 이 인터페이스를 구현하지 않으므로 Pipeline을 통과할 수 없게 됩니다. 패턴 매칭(`is IFinResponseWithError`)으로 런타임에 확인하면 성공/실패 응답 모두 처리할 수 있습니다.

### Q2: Logging Pipeline과 Tracing Pipeline은 같은 이중 제약을 사용하는데, 어떤 차이가 있나요?
**A**: 제약 조건은 동일하지만 **사용 목적**이 다릅니다. Logging Pipeline은 텍스트 로그를 기록하고, Tracing Pipeline은 분산 추적(OpenTelemetry) 태그를 설정합니다. 제약이 같다는 것은 응답에 대한 **필요 능력**이 동일하다는 의미이지, Pipeline의 동작이 같다는 의미는 아닙니다.

### Q3: Read+Create 이중 제약에서 Create 능력은 언제 사용되나요?
**A**: Logging이나 Tracing Pipeline 자체에서 `CreateFail`을 직접 호출하는 경우는 드뭅니다. 하지만 `next()` 호출 시 예외가 발생하면 catch 블록에서 `TResponse.CreateFail(Error.New(ex))`로 실패 응답을 생성해야 합니다. 이 **예외 대응**을 위해 Create 능력이 필요합니다.

## 프로젝트 구조

```
02-Read-Create-Constraint/
├── ReadCreateConstraint/
│   ├── ReadCreateConstraint.csproj
│   ├── SimpleLoggingPipeline.cs
│   └── Program.cs
├── ReadCreateConstraint.Tests.Unit/
│   ├── ReadCreateConstraint.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── ReadCreateConstraintTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project ReadCreateConstraint

# 테스트 실행
dotnet test --project ReadCreateConstraint.Tests.Unit
```

---

동일한 이중 제약을 사용하면서 `where` 제약 조건으로 Command/Query를 컴파일 타임에 분기하는 Transaction과 Caching Pipeline을 살펴봅니다.

→ [4.3장: Transaction/Caching Pipeline](../03-Transaction-Caching-Pipeline/)

