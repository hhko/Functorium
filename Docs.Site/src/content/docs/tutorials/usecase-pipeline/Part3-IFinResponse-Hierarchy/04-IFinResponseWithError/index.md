---
title: "IFinResponseWithError"
---

## 개요

**요구사항 R3**: Pipeline이 실패 시 에러 정보에 접근할 수 있어야 합니다. 지금까지 `IFinResponse`로 성공/실패를 확인하고, `IFinResponseFactory`로 실패 응답을 생성할 수 있게 되었지만, Pipeline에서 **에러 정보에 접근**하려면 어떻게 해야 할까요? 이 장에서는 `IFinResponseWithError` 인터페이스를 도입하여, **Fail 케이스에서만** 에러에 접근할 수 있는 타입 안전한 패턴을 설계합니다.

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

## FAQ

### Q1: `Error` 속성을 `IFinResponse`에 직접 추가하면 왜 안 되나요?
**A**: `IFinResponse`에 `Error`를 추가하면 **성공(Succ) 케이스에서도** `Error`에 접근할 수 있게 됩니다. Succ에는 에러가 없으므로 `null` 반환이나 예외 발생 등 런타임 위험이 생깁니다. 별도 인터페이스로 분리하면 Fail에서만 구현하여 **타입 시스템이 안전성을 보장**합니다.

### Q2: `is IFinResponseWithError` 패턴 매칭은 리플렉션과 다른가요?
**A**: 완전히 다릅니다. `is` 패턴 매칭은 CLR의 타입 시스템이 수행하는 **네이티브 타입 검사**로, 리플렉션(`GetType().GetProperty()`)보다 수십 배 빠릅니다. 또한 `fail.Error` 접근은 컴파일 타임에 검증되므로 프로퍼티 이름 오타 위험도 없습니다.

### Q3: Fail에서만 `IFinResponseWithError`를 구현하는 패턴은 다른 곳에서도 사용되나요?
**A**: 네. 이 패턴은 **케이스별 인터페이스 구현**이라는 일반적인 설계 기법입니다. 예를 들어 HTTP 응답에서 에러 본문은 4xx/5xx 응답에만 존재하므로, 에러 본문 인터페이스를 에러 응답 케이스에서만 구현하는 것과 동일한 원리입니다.

에러 접근까지 해결했지만, 아직 각 인터페이스가 분리되어 있습니다. 다음 장에서는 **요구사항 R4**까지 포함하여 모든 인터페이스를 하나의 타입으로 통합합니다.

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

