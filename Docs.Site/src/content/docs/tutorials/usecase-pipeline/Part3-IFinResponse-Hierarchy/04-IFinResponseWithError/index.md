---
title: "IFinResponseWithError"
---

## 개요

지금까지 `IFinResponse`로 성공/실패를 확인하고, `IFinResponseFactory`로 실패 응답을 생성할 수 있게 되었습니다. 하지만 Pipeline에서 **에러 정보에 접근**하려면 어떻게 해야 할까요? 이 장에서는 `IFinResponseWithError` 인터페이스를 도입하여, **Fail 케이스에서만** 에러에 접근할 수 있는 타입 안전한 패턴을 설계합니다.

```
IFinResponseWithError           ← 에러 접근 인터페이스 (이번 장)
├── Error: Error                  Fail에서만 구현
```

## 핵심 개념

### 1. IFinResponseWithError 인터페이스

`IFinResponseWithError`는 `Error` 속성을 제공하는 별도의 인터페이스입니다. **Fail 케이스에서만** 이 인터페이스를 구현하여, Succ 케이스에서 에러에 접근하는 것을 원천적으로 방지합니다.

```csharp
public interface IFinResponseWithError
{
    Error Error { get; }
}
```

### 2. Fail에서만 구현

Discriminated Union의 `Fail` 레코드만 `IFinResponseWithError`를 구현합니다. `Succ`는 이 인터페이스를 구현하지 않으므로, 에러 접근이 타입 시스템에 의해 차단됩니다.

```csharp
public abstract record ErrorAccessResponse<A> : IFinResponse
{
    public sealed record Succ(A Value) : ErrorAccessResponse<A>
    {
        // IFinResponseWithError를 구현하지 않음!
    }

    public sealed record Fail(Error Error) : ErrorAccessResponse<A>, IFinResponseWithError
    {
        // Fail만 IFinResponseWithError 구현
    }
}
```

### 3. 패턴 매칭으로 에러 접근

Pipeline에서는 `is IFinResponseWithError` 패턴 매칭을 사용하여 에러에 안전하게 접근합니다. Succ인 경우 패턴 매칭이 실패하므로, 에러 접근이 자연스럽게 방지됩니다.

```csharp
public static string LogResponse<TResponse>(TResponse response)
    where TResponse : IFinResponse
{
    if (response.IsSucc)
        return "Success";

    // 패턴 매칭으로 에러 접근 - Fail에서만 IFinResponseWithError 구현
    if (response is IFinResponseWithError failResponse)
        return $"Fail: {failResponse.Error}";

    return "Fail: unknown error";
}
```

### 4. 왜 별도 인터페이스인가?

`Error` 속성을 `IFinResponse`에 직접 추가하면, Succ 케이스에서도 `Error`에 접근할 수 있게 되어 **런타임 예외** 위험이 생깁니다. 별도 인터페이스로 분리하면 **컴파일 타임에 안전성**을 보장할 수 있습니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `IFinResponseWithError`가 Fail에서만 구현되는 이유를 설명할 수 있다
2. 패턴 매칭으로 에러에 안전하게 접근하는 코드를 작성할 수 있다
3. Succ에서 에러 접근이 타입 시스템에 의해 방지되는 원리를 이해할 수 있다
4. 인터페이스 분리가 타입 안전성을 강화하는 방식을 설명할 수 있다

## 프로젝트 구조

```
04-IFinResponseWithError/
├── FinResponseWithError/
│   ├── FinResponseWithError.csproj
│   ├── Interfaces.cs
│   ├── ErrorAccessResponse.cs
│   ├── LoggingPipelineExample.cs
│   └── Program.cs
├── FinResponseWithError.Tests.Unit/
│   ├── FinResponseWithError.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseWithErrorTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinResponseWithError

# 테스트 실행
dotnet test --project FinResponseWithError.Tests.Unit
```

