---
title: "Create-Only Constraint"
---

## 개요

Part 3에서 설계한 IFinResponse 계층을 이제 실전 Pipeline에 적용합니다. Validation Pipeline과 Exception Pipeline은 요청 처리 중 실패가 발생했을 때 **실패 응답을 생성만** 하면 됩니다. 기존 응답을 읽거나 검사할 필요가 없습니다. 이 장에서는 이러한 "생성만 하면 되는" Pipeline에 최소 제약 조건인 `IFinResponseFactory<TResponse>`만 적용하는 패턴을 학습합니다.

```
Pipeline 동작 흐름:

Validation Pipeline:
  isValid? ──No──→ TResponse.CreateFail(error)  ← 생성만 필요
           │
           Yes──→ next() 호출

Exception Pipeline:
  try { next() } catch (ex) → TResponse.CreateFail(error)  ← 생성만 필요
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Create-Only 제약(`IFinResponseFactory<TResponse>`)이 적용되는 Pipeline을 식별할 수 있습니다
2. `TResponse.CreateFail(error)`가 static abstract 호출임을 이해하고, 리플렉션이 불필요한 이유를 설명할 수 있습니다
3. Validation Pipeline과 Exception Pipeline이 응답 읽기(IFinResponse)를 필요로 하지 않는 이유를 설명할 수 있습니다

## 핵심 개념

### 1. Create-Only 제약이란?

Pipeline이 응답 객체의 `IsSucc`/`IsFail`을 읽지 않고, **실패 시 새로운 응답을 생성하기만** 하는 경우에 해당합니다.

```csharp
// 필요한 능력: CreateFail만
where TResponse : IFinResponseFactory<TResponse>
```

이 제약 하나로 Pipeline 내부에서 다음을 호출할 수 있습니다:

```csharp
TResponse.CreateFail(Error.New("Validation failed"));
```

### 2. Validation Pipeline

Validation Pipeline은 요청의 유효성을 검사한 후:
- **유효**: 다음 Pipeline(또는 Handler)에 요청을 전달합니다.
- **무효**: `TResponse.CreateFail(error)`로 실패 응답을 생성하여 즉시 반환합니다.

```csharp
public sealed class SimpleValidationPipeline<TResponse>
    where TResponse : IFinResponseFactory<TResponse>
{
    public TResponse Validate(bool isValid, Func<TResponse> onSuccess)
    {
        if (!isValid)
            return TResponse.CreateFail(Error.New("Validation failed"));

        return onSuccess();
    }
}
```

`TResponse.CreateFail()`은 `IFinResponseFactory<TSelf>`의 **static abstract** 메서드입니다. 리플렉션 없이 컴파일 타임에 해석됩니다.

### 3. Exception Pipeline

Exception Pipeline은 try-catch로 예외를 잡아 실패 응답으로 변환합니다:

```csharp
public sealed class SimpleExceptionPipeline<TResponse>
    where TResponse : IFinResponseFactory<TResponse>
{
    public TResponse Execute(Func<TResponse> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            return TResponse.CreateFail(Error.New(ex));
        }
    }
}
```

### 4. 왜 IFinResponse(Read)가 필요 없는가?

두 Pipeline이 실제로 어떤 능력을 사용하는지 정리하면 다음과 같습니다.

| 동작 | 필요한 인터페이스 | Validation | Exception |
|------|-------------------|:----------:|:---------:|
| IsSucc/IsFail 읽기 | IFinResponse | - | - |
| CreateFail 생성 | IFinResponseFactory | O | O |
| Error 접근 | IFinResponseWithError | - | - |

Validation/Exception Pipeline은 기존 응답을 **검사하지 않습니다**. 실패 조건(유효성 검사 실패, 예외 발생)을 직접 판단하고, 실패 시 새로운 응답을 **생성만** 합니다.

## FAQ

### Q1: Validation Pipeline이 응답을 읽지 않아도 되는 이유는 무엇인가요?
**A**: Validation Pipeline은 **Handler 실행 전에** 요청의 유효성을 검사합니다. 아직 응답이 생성되지 않았으므로 읽을 응답이 없습니다. 유효하지 않으면 `TResponse.CreateFail(error)`로 실패 응답을 직접 생성하여 반환하고, 유효하면 `next()`로 다음 단계에 위임합니다.

### Q2: `TResponse.CreateFail(error)` 호출이 `new TResponse(error)` 생성자 호출과 다른 점은 무엇인가요?
**A**: 생성자 호출(`new TResponse()`)은 제네릭 제약에서 `new()` 제약만 가능하며, 파라미터가 있는 생성자를 호출할 수 없습니다. `static abstract` 메서드인 `CreateFail`은 `Error` 파라미터를 받아 정확한 타입의 실패 인스턴스를 생성할 수 있습니다.

### Q3: Exception Pipeline에서 예외를 `Error`로 변환하는 이유는 무엇인가요?
**A**: 예외(Exception)를 `Error.New(ex)` 형태로 변환하면, Pipeline 외부에서는 예외가 아닌 `FinResponse.Fail`로 일관되게 처리됩니다. 이를 통해 상위 레이어에서 try-catch 없이 `IsSucc`/`IsFail`로 모든 실패를 **동일한 방식으로** 처리할 수 있습니다.

## 프로젝트 구조

```
01-Create-Only-Constraint/
├── CreateOnlyConstraint/
│   ├── CreateOnlyConstraint.csproj
│   ├── SimpleValidationPipeline.cs
│   └── Program.cs
├── CreateOnlyConstraint.Tests.Unit/
│   ├── CreateOnlyConstraint.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── CreateOnlyConstraintTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project CreateOnlyConstraint

# 테스트 실행
dotnet test --project CreateOnlyConstraint.Tests.Unit
```

---

응답의 성공/실패 상태를 읽으면서 실패 응답도 생성해야 하는 Logging, Tracing, Metrics Pipeline에 Read+Create 이중 제약을 적용합니다.

→ [4.2장: Read+Create 제약](../02-Read-Create-Constraint/)

