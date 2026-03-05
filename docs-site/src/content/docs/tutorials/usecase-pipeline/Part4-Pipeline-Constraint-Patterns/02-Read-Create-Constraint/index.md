---
title: "Read+Create 제약"
---

## 개요

Logging, Tracing, Metrics Pipeline은 핸들러 실행 **후** 응답의 성공/실패 상태를 **읽어야** 합니다. 또한 예외 발생 시 실패 응답을 **생성**할 수도 있어야 합니다. 이 장에서는 Read와 Create를 모두 요구하는 이중 제약 패턴을 학습합니다.

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

## 핵심 개념

### 1. Read+Create 이중 제약

Pipeline이 응답의 상태를 읽고(Read), 필요 시 실패 응답을 생성(Create)하는 경우에 해당합니다.

```csharp
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

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

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Read+Create 이중 제약이 필요한 Pipeline을 식별할 수 있다
2. `IFinResponse`(읽기)와 `IFinResponseFactory<TResponse>`(생성)의 역할 차이를 설명할 수 있다
3. Error 접근에 패턴 매칭(`is IFinResponseWithError`)을 사용하는 이유를 설명할 수 있다

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

