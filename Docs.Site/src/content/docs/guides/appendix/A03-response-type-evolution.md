---
title: "FinResponse 타입 진화 기록"
---

이 문서는 Functorium의 응답 타입 시스템이 `IFinResponse`에서 `FinResponse<A>` abstract record로 진화한 과정과 기술적 이슈 해결 과정을 기록합니다.

## 요약

### 주요 명령

```csharp
// 성공 응답 생성
FinResponse.Succ(value)

// 실패 응답 생성
FinResponse.Fail<Response>(error)

// 값 접근
A value = response.ThrowIfFail();

// 성공/실패 판단
response.IsSucc / response.IsFail

// Fin -> FinResponse 변환
Fin<Response> result = await usecase.Run().RunAsync();
return result.ToFinResponse();

// 암시적 변환 (성공)
return new Response(productId, name);

// 암시적 변환 (실패)
return Error.New("상품을 찾을 수 없습니다");
```

### 주요 절차

1. Usecase의 반환 타입을 `FinResponse<Response>`로 정의
2. LINQ 기반 Usecase에서 `FinT<IO, Response>` 합성
3. `usecase.Run().RunAsync()`로 실행하여 `Fin<Response>` 획득
4. `.ToFinResponse()`로 `FinResponse<Response>`로 변환하여 반환
5. Pipeline에서 `IsSucc`/`IsFail`로 성공/실패 분기, `IFinResponseWithError`로 에러 접근

### 주요 개념

| 개념 | 설명 |
|------|------|
| `FinResponse<A>` | 성공(`Succ`)/실패(`Fail`)를 표현하는 abstract record |
| `IFinResponseFactory<TSelf>` | CRTP 패턴으로 Pipeline에서 실패 응답을 생성하는 인터페이스 |
| `IFinResponseWithError` | Pipeline에서 에러에 접근하기 위한 인터페이스 |
| `static abstract` | C# 11 기능으로 리플렉션 없이 타입별 팩토리 메서드 호출 |
| `.ToFinResponse()` | `Fin<T>` → `FinResponse<T>` 변환 확장 메서드 |

---

## 개요

### 문제 정의

기존 `IFinResponse` 기반 설계에서 세 가지 핵심 문제가 있었습니다:

| # | 문제 | 설명 |
|:-:|------|------|
| 1 | **인터페이스 강제** | Pipeline이 응답을 인식하려면 반드시 인터페이스를 정의해야 함 |
| 2 | **이중 인터페이스 복잡성** | `IResponse`를 감싸는 `IFinResponse` 인터페이스가 추가로 필요 |
| 3 | **리플렉션 의존** | Pipeline에서 성공/실패 확인 및 실패 응답 생성 시 리플렉션 필요 |

### 진화 요약

| 단계 | 접근 방식 | 리플렉션 사용 | 문제점 |
|------|----------|:------------:|--------|
| 1단계 | `IFinResponse` | 1곳 | 성공/실패 분기 리플렉션 |
| 2단계 | `Fin<T>` 직접 사용 | 3곳 | **악화** - 더 많은 리플렉션 |
| 3단계 | `IResponse<T>` + `ResponseBase<T>` | 0곳 | **해결** - 리플렉션 완전 제거 |
| 4단계 | Positional Record 문법 적용 | 0곳 | **개선** - 코드량 60% 감소 |
| 5단계 | `ToResponse` 확장 메서드 | 0곳 | **개선** - Handler 코드 단순화 |

> **현재 구조**: 위 진화 과정을 거쳐 최종적으로 `FinResponse<A>` abstract record 기반으로 안정화되었습니다. 현재 API는 `IsSucc`/`IsFail` 속성, `ThrowIfFail()` 메서드, `FinResponse.Succ(value)`/`FinResponse.Fail<A>(error)` 정적 팩토리, `IFinResponseFactory<TSelf>` CRTP 패턴을 사용합니다.

---

## 1단계: IFinResponse

### 도입 배경

LanguageExt의 `Fin<T>` 모나드를 Mediator 패턴의 Pipeline에서 사용하려면 Pipeline이 응답의 성공/실패를 인식해야 했습니다. 그러나 `Fin<T>`는 외부 라이브러리 타입이므로 Pipeline에서 직접 제약 조건으로 사용할 수 없었습니다.

### 구조

```csharp
// Handler 반환 타입
public interface ICommandRequest<TResponse>
    : ICommand<Fin<TResponse>>
    where TResponse : IResponse;

// IFinResponse 인터페이스 - Pipeline이 Fin<T>를 인식하기 위해 도입
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
    Error GetError();
}
```

### Pipeline 코드

```csharp
public async ValueTask<TResponse> Handle(...)
{
    TResponse response = await next(request, cancellationToken);

    // 리플렉션으로 IFinResponse 인터페이스 접근
    if (response is IFinResponse finResponse)
    {
        if (finResponse.IsSucc)
            LogSuccess(response);
        else
            LogError(finResponse.GetError());
    }

    return response;
}
```

### 핵심 문제

1. **인터페이스 강제**: `Fin<T>`는 sealed struct이므로 `where TResponse : Fin<T>` 불가
2. **이중 인터페이스**: `IResponse`(비즈니스 응답)와 `IFinResponse`(성공/실패 래퍼) 두 개 필요
3. **리플렉션 의존**: `Fin<T>`를 `IFinResponse`로 캐스팅 시 리플렉션 사용

---

## 2단계: Fin&lt;T&gt; 직접 사용

### 시도한 개선

`IFinResponse` 래퍼 없이 `Fin<T>`를 직접 사용하여 코드를 단순화하려 했습니다.

### Pipeline 코드

```csharp
public async ValueTask<TResponse> Handle(...)
{
    TResponse response = await next(request, cancellationToken);

    // TResponse가 Fin<T>인지 알 수 없음 - 리플렉션 3곳 필요
    bool isSucc = FinResponseUtilites.IsSucc(response);           // 리플렉션 1
    Error error = FinResponseUtilites.GetError(response);         // 리플렉션 2
    return FinResponseUtilites.CreateFail<TResponse>(error);      // 리플렉션 3
}
```

### 결과: 악화

| 항목 | IFinResponse | Fin&lt;T&gt; 직접 사용 |
|------|:-----------:|:-------------------:|
| **총 리플렉션** | **1곳** | **3곳** |

---

## 3단계: IResponse&lt;T&gt; + ResponseBase&lt;T&gt;

### 핵심 아이디어

1. **C# 11 `static abstract`**: 인터페이스에서 정적 팩토리 메서드 강제
2. **직접 속성 접근**: 인터페이스에 성공/실패 속성 정의
3. **CRTP 패턴**: `ResponseBase<TSelf>`로 보일러플레이트 최소화

### 타입 정의

```csharp
// 팩토리 메서드 포함 인터페이스
public interface IResponse<T> : IResponse where T : IResponse<T>
{
    static abstract T CreateFail(Error error);  // C# 11 static abstract
}

// 기본 구현 제공
public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>, new()
{
    public bool IsSuccess { get; init; } = true;
    public Error? Error { get; init; } = null;

    public static TSelf CreateFail(Error error)
        => new TSelf() { IsSuccess = false, Error = error };
}
```

### Pipeline 코드 (리플렉션 제거)

```csharp
public sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IResponse<TResponse>  // 제약 조건
{
    public async ValueTask<TResponse> Handle(...)
    {
        if (errors.Length > 0)
        {
            // static abstract 호출 - 리플렉션 없음
            return TResponse.CreateFail(Error.Many(errors));
        }

        TResponse response = await next(request, cancellationToken);

        // 직접 속성 접근 - 리플렉션 없음
        if (response.IsSuccess)
            LogSuccess(response);
        else
            LogError(response.Error!);

        return response;
    }
}
```

### 결과: 세 가지 문제 모두 해결

| 문제 | Before | After |
|------|--------|-------|
| 인터페이스 강제 | `IFinResponse` 래퍼 필요 | `IResponse<T>` 단일 인터페이스 |
| 이중 인터페이스 | `IResponse` + `IFinResponse` 2개 | `IResponse<T>` 1개로 통합 |
| 리플렉션 의존 | 3곳에서 리플렉션 | **0곳** - 완전 제거 |

---

## 발견된 버그와 해결

### 버그: Value 속성으로 인한 무한 재귀

초기 설계에서 `IResponse<T>`에 `Value` 속성을 포함했습니다:

```csharp
public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>, new()
{
    public TSelf? Value => IsSuccess ? (TSelf)this : default;  // 무한 재귀!
}
```

### 원인

Record 타입의 `ToString()` -> `PrintMembers` -> `Value` 속성 접근 -> `(TSelf)this` 반환 -> `ToString()` -> 무한 재귀 -> `StackOverflowException`

### 해결

`Value` 속성을 완전히 제거했습니다. 성공 시 `Value`는 `this` 자체이므로 별도 속성이 불필요합니다.

---

## 4단계: Positional Record 문법 적용

### 개선

C#의 positional record 문법으로 Response 정의 코드를 60% 감소:

```csharp
// Before (20줄)
public sealed record class Response : ResponseBase<Response>
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    // ...
    public Response() { }
    public Response(Guid productId, string name, ...) { ... }
}

// After (8줄)
public sealed record class Response(
    Guid ProductId,
    string Name,
    decimal Price) : ResponseBase<Response>
{
    public Response() : this(Guid.Empty, string.Empty, 0m) { }
}
```

파라미터 없는 생성자가 필요한 이유: `ResponseBase<TSelf>`의 `CreateFail` 메서드에서 `new TSelf()` 호출에 `new()` 제약 충족이 필요합니다.

---

## 5단계: ToResponse 확장 메서드

### 개선

`Fin<T>` -> `IResponse<TResponse>` 변환의 반복 패턴을 확장 메서드로 캡슐화:

```csharp
// 확장 메서드
public static TResponse ToResponse<TSource, TResponse>(
    this Fin<TSource> fin,
    Func<TSource, TResponse> mapper)
    where TResponse : IResponse<TResponse>
{
    return fin.Match(
        Succ: mapper,
        Fail: error => TResponse.CreateFail(error));
}

// Handler에서 사용
return createResult.ToResponse(product => new Response(
    product.Id, product.Name, product.Price));
```

---

## 최종 구조: FinResponse&lt;A&gt;

위 진화 과정을 거쳐 최종적으로 `FinResponse<A>` abstract record 기반으로 안정화되었습니다.

### 현재 타입 정의

```csharp
public abstract record FinResponse<A>
{
    public sealed record Succ(A Value) : FinResponse<A>;
    public sealed record Fail(Error Error) : FinResponse<A>;

    public abstract bool IsSucc { get; }
    public abstract bool IsFail { get; }
}
```

### 정적 팩토리

```csharp
// FinResponse 정적 클래스의 팩토리 메서드
FinResponse.Succ(value)       // 성공 응답 생성
FinResponse.Fail<A>(error)    // 실패 응답 생성
```

### 값 접근

```csharp
// ThrowIfFail()로 값 접근 (내부적으로 SuccValue 사용)
A value = response.ThrowIfFail();
```

### CRTP 팩토리 패턴

Pipeline에서 실패 응답을 생성할 때 사용합니다:

```csharp
// 인터페이스
public interface IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}

// Pipeline에서 사용
return TResponse.CreateFail(error);
```

### Pipeline에서의 에러 접근

Pipeline에서 `FinResponse`의 에러에 접근할 때는 `IFinResponseWithError` 인터페이스로 캐스팅합니다:

```csharp
if (response is IFinResponseWithError { IsFail: true } failResponse)
{
    LogError(failResponse.Error);
}
```

### Fin -> FinResponse 변환

```csharp
// LINQ 기반 Usecase에서 실행 흐름
FinT<IO, Response> usecase = ...;
Fin<Response> response = await usecase.Run().RunAsync();
return response.ToFinResponse();  // Fin<T> -> FinResponse<T> 변환
```

### 암시적 변환

```csharp
// 성공 반환 - 값을 직접 반환
return new Response(productId, name);

// 실패 반환 - Error를 직접 반환
return Error.New("상품을 찾을 수 없습니다");

// 명시적 실패 반환
return FinResponse.Fail<Response>(error);
```

---

## 핵심 교훈

### 기술적 교훈

1. **리플렉션 제거**: C# 11 `static abstract` 인터페이스 멤버로 타입 안전한 팩토리 패턴 구현
2. **CRTP 활용**: `IFinResponseFactory<TSelf>`로 Pipeline에서 타입별 팩토리 메서드 호출
3. **순환 참조 주의**: Record의 `PrintMembers`가 모든 속성을 순회하므로 자기 참조 속성 주의
4. **점진적 검증**: 각 단계에서 빌드 -> 테스트 -> 실행 순으로 검증

### 최종 구조 비교

```
Before (Fin<T> 직접 사용):
  Pipeline: FinResponseUtilites.CreateFail() <- 리플렉션
  Pipeline: FinResponseUtilites.IsSucc()     <- 리플렉션
  Pipeline: FinResponseUtilites.GetError()   <- 리플렉션

After (FinResponse<A>):
  Pipeline: TResponse.CreateFail()    <- static abstract (CRTP)
  Pipeline: response.IsSucc           <- 직접 접근
  Pipeline: response.IsFail           <- 직접 접근
  Pipeline: (IFinResponseWithError)   <- 인터페이스 캐스팅
```

### 요구 사항

- **.NET 7+**: C# 11의 `static abstract` 인터페이스 멤버 필요

---

## 트러블슈팅

### Record의 Value 속성에서 StackOverflowException이 발생한다

**원인:** Record 타입의 `ToString()` → `PrintMembers` → `Value` 속성 접근 → `(TSelf)this` 반환 → `ToString()` 호출로 무한 재귀가 발생합니다.

**해결:** `Value` 속성을 제거합니다. 성공 시 `Value`는 `this` 자체이므로 별도 속성이 불필요합니다. 현재 `FinResponse<A>`는 `ThrowIfFail()`로 값에 접근합니다.

### Pipeline에서 FinResponse의 에러에 접근할 수 없다

**원인:** `FinResponse<A>`의 제네릭 타입 파라미터 때문에 Pipeline에서 직접 `Error` 속성에 접근하기 어렵습니다.

**해결:** `IFinResponseWithError` 인터페이스로 캐스팅합니다.
```csharp
if (response is IFinResponseWithError { IsFail: true } failResponse)
{
    LogError(failResponse.Error);
}
```

### Positional Record에서 파라미터 없는 생성자 누락 컴파일 에러

**원인:** `ResponseBase<TSelf>`의 `CreateFail` 메서드에서 `new TSelf()` 호출에 `new()` 제약 충족이 필요합니다. Positional record는 기본적으로 파라미터 없는 생성자를 제공하지 않습니다.

**해결:**
```csharp
public sealed record Response(Guid ProductId, string Name, decimal Price)
    : ResponseBase<Response>
{
    public Response() : this(Guid.Empty, string.Empty, 0m) { }  // 파라미터 없는 생성자 추가
}
```

---

## FAQ

### Q1. FinResponse<A>와 Fin<T>의 차이는 무엇인가요?

`Fin<T>`는 LanguageExt 라이브러리의 sealed struct로 Pipeline에서 직접 제약 조건으로 사용할 수 없습니다. `FinResponse<A>`는 Functorium이 정의한 abstract record로, `IsSucc`/`IsFail` 속성과 `IFinResponseFactory<TSelf>` CRTP 패턴을 통해 Pipeline에서 리플렉션 없이 성공/실패를 인식하고 실패 응답을 생성할 수 있습니다.

### Q2. 왜 리플렉션을 제거해야 했나요?

리플렉션은 런타임 성능 비용이 있고, 컴파일 타임 타입 안전성을 보장하지 못합니다. C# 11의 `static abstract` 인터페이스 멤버를 활용하면 `TResponse.CreateFail(error)` 같은 호출이 컴파일 타임에 검증되어 성능과 안전성을 모두 확보할 수 있습니다.

### Q3. Usecase에서 성공/실패를 어떻게 반환하나요?

암시적 변환을 사용할 수 있습니다. 성공 시 `return new Response(...)`, 실패 시 `return Error.New("메시지")` 또는 명시적으로 `return FinResponse.Fail<Response>(error)`를 사용합니다.

### Q4. .NET 7 미만에서는 FinResponse를 사용할 수 없나요?

`FinResponse<A>`는 C# 11의 `static abstract` 인터페이스 멤버를 사용하므로 .NET 7 이상이 필요합니다. .NET 7 미만에서는 리플렉션 기반 접근 방식을 사용해야 합니다.

### Q5. ToFinResponse()는 언제 호출하나요?

LINQ 기반 Usecase에서 `FinT<IO, Response>` 합성 후, `usecase.Run().RunAsync()`로 `Fin<Response>`를 얻고, `.ToFinResponse()`로 `FinResponse<Response>`로 변환합니다. 이 변환은 Usecase Handler의 마지막 단계에서 한 번 호출합니다.

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) | Use Case와 CQRS (FinResponse 사용 패턴) |
| [C# 11 Static Abstract Members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#generic-math-support) | C# 11 static abstract 공식 문서 |
| [CRTP](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern) | Curiously Recurring Template Pattern |
| [LanguageExt](https://github.com/louthy/language-ext) | Fin 타입 제공 라이브러리 |
