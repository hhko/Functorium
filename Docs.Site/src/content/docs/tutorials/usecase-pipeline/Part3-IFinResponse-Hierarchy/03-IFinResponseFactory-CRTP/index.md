---
title: "IFinResponseFactory CRTP"
---

## 개요

**요구사항 R2**: Pipeline이 실패 응답을 직접 생성할 수 있어야 합니다. 1장과 2장에서 Pipeline이 응답을 **읽을 수** 있게 되었지만, Validation Pipeline처럼 **실패 응답을 생성**해야 하는 경우는 어떻게 할까요? 이 장에서는 **CRTP(Curiously Recurring Template Pattern)와** C# 11의 **static abstract** 메서드를 사용하여, Pipeline에서 리플렉션 없이 `TResponse.CreateFail(error)`을 호출할 수 있는 팩토리 인터페이스를 설계합니다.

```
IFinResponseFactory<TSelf>      ← CRTP 팩토리 (이번 장)
├── static abstract CreateFail(Error error) → TSelf
```

## 핵심 개념

### 1. CRTP (Curiously Recurring Template Pattern)

CRTP는 자기 자신을 타입 파라미터로 전달하는 패턴입니다. `TSelf`가 자기 자신의 타입을 참조하므로, `CreateFail`의 반환 타입이 정확한 구현 타입이 됩니다.

```csharp
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
```

### 2. C# 11 static abstract 메서드

`static abstract`는 인터페이스에서 **정적 메서드의 구현을 강제**합니다. 이를 통해 제네릭 제약에서 `T.Method()` 형태의 호출이 가능해집니다.

```csharp
public record FactoryResponse<A> : IFinResponseFactory<FactoryResponse<A>>
{
    // static abstract 구현
    public static FactoryResponse<A> CreateFail(Error error) => new(error);
}
```

### 3. Pipeline에서의 활용

`where TResponse : IFinResponseFactory<TResponse>` 제약을 사용하면, Pipeline에서 `TResponse.CreateFail(error)`을 **직접 호출**할 수 있습니다. 리플렉션이 필요 없습니다.

주목할 점은 CRTP 제약 덕분에 `TResponse.CreateFail`이 정확한 구현 타입을 반환한다는 것입니다.

```csharp
public static TResponse ValidateAndCreate<TResponse>(
    bool isValid,
    Func<TResponse> onSuccess,
    string errorMessage)
    where TResponse : IFinResponseFactory<TResponse>
{
    if (!isValid)
    {
        // static abstract 호출 - 리플렉션 없음!
        return TResponse.CreateFail(Error.New(errorMessage));
    }
    return onSuccess();
}
```

### 4. 왜 CRTP가 필요한가?

일반 인터페이스로는 `static abstract` 메서드의 반환 타입을 **자기 자신**으로 지정할 수 없습니다. CRTP의 `TSelf` 제약이 있어야 `CreateFail`이 정확한 구현 타입을 반환합니다.

```csharp
// CRTP 없이: 반환 타입이 모호
public interface IFactory
{
    static abstract ??? CreateFail(Error error);  // 반환 타입을 지정할 수 없음
}

// CRTP: 반환 타입이 정확
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);  // TSelf = 구현 타입
}
```

## FAQ

### Q1: CRTP 없이 일반 인터페이스로 팩토리를 정의할 수 없나요?
**A**: 일반 인터페이스에서 `static abstract` 메서드의 반환 타입을 자기 자신으로 지정할 방법이 없습니다. `IFactory.CreateFail(Error)` 형태로 정의하면 반환 타입이 `IFactory`이므로, 구현 타입으로의 다운캐스팅이 필요합니다. CRTP의 `TSelf` 제약이 있어야 `CreateFail`이 **정확한 구현 타입**을 반환합니다.

### Q2: `static abstract`는 C# 11 이전에는 어떻게 대체했나요?
**A**: C# 11 이전에는 `static abstract`가 없었으므로, 팩토리 패턴을 구현하려면 **별도의 팩토리 클래스**를 DI로 주입하거나, 리플렉션으로 정적 메서드를 호출해야 했습니다. `static abstract`의 등장으로 인터페이스 수준에서 팩토리 계약을 정의할 수 있게 되었고, 이것이 리플렉션 제거의 핵심 기술입니다.

### Q3: `TResponse.CreateFail(error)` 호출이 리플렉션 없이 가능한 원리는 무엇인가요?
**A**: `where TResponse : IFinResponseFactory<TResponse>` 제약 덕분에 컴파일러가 `TResponse`에 `CreateFail` 정적 메서드가 존재함을 **컴파일 타임에 확인**합니다. JIT 컴파일러는 구체 타입에 따라 직접 호출 코드를 생성하므로, 리플렉션이나 가상 디스패치 없이 실행됩니다.

### Q4: `CreateFail`만 정의하고 `CreateSucc`는 왜 팩토리에 포함하지 않나요?
**A**: Pipeline에서 응답을 **생성**하는 경우는 대부분 **실패 응답**입니다(Validation 실패, 예외 발생). 성공 응답은 Handler가 직접 반환하므로 Pipeline에서 생성할 필요가 없습니다. 최소 인터페이스 원칙에 따라 실제로 필요한 `CreateFail`만 정의합니다.

실패 응답을 생성할 수 있게 되었지만, 에러의 내용은 아직 알 수 없습니다. 다음 장에서는 **요구사항 R3**(에러 접근)을 해결합니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. CRTP 패턴이 무엇이며 왜 필요한지 설명할 수 있다
2. C# 11 `static abstract` 메서드를 인터페이스에 정의하고 구현할 수 있다
3. Pipeline에서 `TResponse.CreateFail(error)`을 호출하는 제약 조건을 작성할 수 있다
4. CRTP 팩토리가 리플렉션을 제거하는 원리를 이해할 수 있다

## 프로젝트 구조

```
03-IFinResponseFactory-CRTP/
├── FinResponseFactoryCrtp/
│   ├── FinResponseFactoryCrtp.csproj
│   ├── IFinResponseFactory.cs
│   ├── FactoryResponse.cs
│   ├── ValidationPipelineExample.cs
│   └── Program.cs
├── FinResponseFactoryCrtp.Tests.Unit/
│   ├── FinResponseFactoryCrtp.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseFactoryCrtpTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinResponseFactoryCrtp

# 테스트 실행
dotnet test --project FinResponseFactoryCrtp.Tests.Unit
```

