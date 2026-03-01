# 14장: Create-Only 제약 (Validation/Exception)

## 개요

Validation Pipeline과 Exception Pipeline은 요청 처리 중 실패가 발생했을 때 **실패 응답을 생성만** 하면 됩니다. 기존 응답을 읽거나 검사할 필요가 없습니다. 이 장에서는 이러한 "생성만 하면 되는" Pipeline에 최소 제약 조건인 `IFinResponseFactory<TResponse>`만 적용하는 패턴을 학습합니다.

```
Pipeline 동작 흐름:

Validation Pipeline:
  isValid? ──No──→ TResponse.CreateFail(error)  ← 생성만 필요
           │
           Yes──→ next() 호출

Exception Pipeline:
  try { next() } catch (ex) → TResponse.CreateFail(error)  ← 생성만 필요
```

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

| 동작 | 필요한 인터페이스 | Validation | Exception |
|------|-------------------|:----------:|:---------:|
| IsSucc/IsFail 읽기 | IFinResponse | - | - |
| CreateFail 생성 | IFinResponseFactory | O | O |
| Error 접근 | IFinResponseWithError | - | - |

Validation/Exception Pipeline은 기존 응답을 **검사하지 않습니다**. 실패 조건(유효성 검사 실패, 예외 발생)을 직접 판단하고, 실패 시 새로운 응답을 **생성만** 합니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Create-Only 제약(`IFinResponseFactory<TResponse>`)이 적용되는 Pipeline을 식별할 수 있다
2. `TResponse.CreateFail(error)`가 static abstract 호출임을 이해하고, 리플렉션이 불필요한 이유를 설명할 수 있다
3. Validation Pipeline과 Exception Pipeline이 응답 읽기(IFinResponse)를 필요로 하지 않는 이유를 설명할 수 있다

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

[← 이전: 13장 FinResponse\<A\> Discriminated Union](../../Part3-IFinResponse-Hierarchy/05-FinResponse-Discriminated-Union/README.md) | [다음: 15장 Read+Create 제약 →](../02-Read-Create-Constraint/README.md)
