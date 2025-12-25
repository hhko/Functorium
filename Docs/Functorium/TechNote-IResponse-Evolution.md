# 기술 노트: IResponse 진화 과정

이 문서는 Functorium의 응답 타입 시스템이 `IFinResponse`에서 `IResponse<T> + ResponseBase<T>`로 진화한 과정과 기술적 이슈 해결 과정을 기록합니다.

## 목차
- [개요](#개요)
- [1단계: IFinResponse](#1단계-ifinresponse)
- [2단계: Fin&lt;T&gt; 직접 사용](#2단계-fint-직접-사용)
- [3단계: IResponse&lt;T&gt; + ResponseBase&lt;T&gt;](#3단계-responset--responsebaset)
- [발견된 버그와 해결](#발견된-버그와-해결)
- [4단계: Positional Record 문법 적용](#4단계-positional-record-문법-적용)
- [5단계: ToResponse 확장 메서드](#5단계-toresponse-확장-메서드)
- [결론](#결론)

<br/>

## 개요

### 문제 정의

Mediator 패턴의 Pipeline에서 성공/실패 상태를 확인하고 실패 응답을 생성하는 과정에서 **리플렉션**이 필요했습니다. 리플렉션은 성능 저하와 타입 안전성 문제를 야기합니다.

### 진화 요약

| 단계 | 접근 방식 | 리플렉션 사용 | 문제점 |
|------|----------|:------------:|--------|
| 1단계 | `IFinResponse` | 1곳 | 성공/실패 분기 리플렉션 |
| 2단계 | `Fin<T>` 직접 사용 | 3곳 | **악화** - 더 많은 리플렉션 |
| 3단계 | `IResponse<T>` + `ResponseBase<T>` | 0곳 | **해결** - 리플렉션 완전 제거 |
| 4단계 | Positional Record 문법 적용 | 0곳 | **개선** - 코드량 60% 감소 |
| 5단계 | `ToResponse` 확장 메서드 | 0곳 | **개선** - Handler 코드 단순화 |

<br/>

## 1단계: IFinResponse

### 구조

```csharp
// Handler 반환 타입
public interface ICommandRequest<TResponse>
    : ICommand<Fin<TResponse>>
    where TResponse : IResponse;

// IFinResponse 인터페이스
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

### 문제점

- `Fin<T>`를 `IFinResponse`로 캐스팅하기 위해 리플렉션 필요
- 실패 응답 생성 시에도 리플렉션 필요:

```csharp
// FinResponseUtilites.cs - 리플렉션 사용
public static TResponse CreateFail<TResponse>(Error error)
{
    var finType = typeof(Fin<>).MakeGenericType(/* ... */);  // 리플렉션
    // ...
}
```

<br/>

## 2단계: Fin&lt;T&gt; 직접 사용

### 시도한 개선

`IFinResponse` 인터페이스 없이 `Fin<T>`를 직접 사용하여 더 깔끔한 코드를 목표로 했습니다.

### 구조

```csharp
public interface ICommandRequest<TResponse>
    : ICommand<Fin<TResponse>>
    where TResponse : IResponse;
```

### Pipeline 코드

```csharp
public async ValueTask<TResponse> Handle(...)
{
    TResponse response = await next(request, cancellationToken);

    // 문제: TResponse가 Fin<T>인지 알 수 없음
    // 리플렉션으로 성공/실패 확인 필요
    bool isSucc = FinResponseUtilites.IsSucc(response);           // 리플렉션 1
    Error error = FinResponseUtilites.GetError(response);         // 리플렉션 2

    return response;
}

// 실패 응답 생성도 리플렉션 필요
return FinResponseUtilites.CreateFail<TResponse>(error);          // 리플렉션 3
```

### FinResponseUtilites 구현 (리플렉션)

```csharp
public static class FinResponseUtilites
{
    // 리플렉션 1: 성공 여부 확인
    public static bool IsSucc<TResponse>(TResponse response)
    {
        var type = response.GetType();
        var property = type.GetProperty("IsSucc");  // 리플렉션
        return (bool)property.GetValue(response);
    }

    // 리플렉션 2: 에러 추출
    public static Error GetError<TResponse>(TResponse response)
    {
        var type = response.GetType();
        // 리플렉션으로 Error 추출...
    }

    // 리플렉션 3: 실패 응답 생성
    public static TResponse CreateFail<TResponse>(Error error)
    {
        var innerType = typeof(TResponse).GetGenericArguments()[0];
        var finType = typeof(Fin<>).MakeGenericType(innerType);
        // 리플렉션으로 Fin<T>.Fail 생성...
    }
}
```

### 결과: 악화

| 항목 | IFinResponse | Fin&lt;T&gt; 직접 사용 |
|------|:-----------:|:-------------------:|
| 성공/실패 확인 | 리플렉션 | 리플렉션 |
| 에러 추출 | 리플렉션 | 리플렉션 |
| 실패 응답 생성 | 리플렉션 | 리플렉션 |
| **총 리플렉션** | **1곳** | **3곳** |

"더 나아졌나?" → **아니오, 더 악화됨**

<br/>

## 3단계: IResponse&lt;T&gt; + ResponseBase&lt;T&gt;

### 핵심 아이디어

1. **C# 11 `static abstract`**: 인터페이스에서 정적 팩토리 메서드 강제
2. **직접 속성 접근**: 인터페이스에 `IsSuccess`, `Error` 속성 정의
3. **CRTP 패턴**: `ResponseBase<TSelf>`로 보일러플레이트 최소화

### 타입 정의

```csharp
// IResponse.cs - 기본 인터페이스
public interface IResponse
{
    bool IsSuccess { get; }
    Error? Error { get; }
}

// IResponseT.cs - 팩토리 메서드 포함
public interface IResponse<T> : IResponse where T : IResponse<T>
{
    static abstract T CreateFail(Error error);  // C# 11 static abstract
}

// ResponseBase.cs - 기본 구현 제공
public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>, new()
{
    public bool IsSuccess { get; init; } = true;
    public Error? Error { get; init; } = null;

    public static TSelf CreateFail(Error error)
        => new TSelf() { IsSuccess = false, Error = error };
}
```

### Request 인터페이스 수정

```csharp
// Before
public interface ICommandRequest<TResponse>
    : ICommand<Fin<TResponse>>
    where TResponse : IResponse;

// After - Fin<T> 제거, IResponse<TResponse> 제약 추가
public interface ICommandRequest<TResponse>
    : ICommand<TResponse>
    where TResponse : IResponse<TResponse>;
```

### Pipeline 코드 (리플렉션 없음)

```csharp
public sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IResponse<TResponse>  // 제약 조건 추가
{
    public async ValueTask<TResponse> Handle(...)
    {
        if (errors.Length > 0)
        {
            // static abstract 호출 - 리플렉션 없음!
            return TResponse.CreateFail(Error.Many(errors));
        }

        TResponse response = await next(request, cancellationToken);

        // 직접 속성 접근 - 리플렉션 없음!
        if (response.IsSuccess)
            LogSuccess(response);
        else
            LogError(response.Error!);

        return response;
    }
}
```

### Response 구현 예시

```csharp
// 사용자가 작성하는 Response 클래스
public sealed record class Response : ResponseBase<Response>
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }

    public Response() { }  // new() 제약을 위한 파라미터 없는 생성자

    public Response(Guid productId, string name, decimal price)
    {
        ProductId = productId;
        Name = name;
        Price = price;
    }
}
```

### Handler 구현 예시

```csharp
public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
{
    // 성공: 직접 인스턴스 생성
    return new Response(
        productId: Guid.NewGuid(),
        name: request.Name,
        price: request.Price);

    // 실패: static 메서드 호출
    return Response.CreateFail(DomainErrors.InvalidPrice(request.Price));
}
```

### 결과: 해결

| 항목 | Fin&lt;T&gt; 직접 사용 | IResponse&lt;T&gt; |
|------|:-------------------:|:-----------------:|
| 성공/실패 확인 | 리플렉션 | `response.IsSuccess` |
| 에러 추출 | 리플렉션 | `response.Error` |
| 실패 응답 생성 | 리플렉션 | `TResponse.CreateFail()` |
| **총 리플렉션** | **3곳** | **0곳** |

<br/>

## 발견된 버그와 해결

### 버그: Value 속성으로 인한 무한 재귀

초기 설계에서 `IResponse<T>`에 `Value` 속성을 포함했습니다:

```csharp
// 초기 설계 (버그 있음)
public interface IResponse<T> : IResponse where T : IResponse<T>
{
    T? Value { get; }  // 문제 발생
    static abstract T CreateFail(Error error);
}

public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>, new()
{
    public bool IsSuccess { get; init; } = true;
    public Error? Error { get; init; } = null;
    public TSelf? Value => IsSuccess ? (TSelf)this : default;  // 무한 재귀!
}
```

### 증상

`CqrsPipeline.Demo` 실행 시 스택 오버플로우 발생:

```
System.InsufficientExecutionStackException: Insufficient stack to continue executing the program safely.
```

### 원인 분석

Record 타입의 `ToString()` 호출 시 `PrintMembers`가 모든 속성을 직렬화합니다:

1. `response.ToString()` 호출
2. `PrintMembers`에서 `Value` 속성 접근
3. `Value`가 `(TSelf)this`를 반환
4. 반환된 값의 `ToString()` 호출
5. **2번으로 돌아감** → 무한 재귀

```
ToString() → PrintMembers → Value → (TSelf)this → ToString() → ...
```

### 해결

`Value` 속성을 완전히 제거했습니다:

```csharp
// 수정된 설계
public interface IResponse<T> : IResponse where T : IResponse<T>
{
    // Value 속성 제거
    static abstract T CreateFail(Error error);
}

public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>, new()
{
    public bool IsSuccess { get; init; } = true;
    public Error? Error { get; init; } = null;
    // Value 속성 제거

    public static TSelf CreateFail(Error error)
        => new TSelf() { IsSuccess = false, Error = error };
}
```

### 제거 근거

- 성공 시 `Value`는 `this` 자체이므로 별도 속성 불필요
- `IsSuccess`로 성공 여부 확인 후 Response 자체를 사용하면 됨

<br/>

## 4단계: Positional Record 문법 적용

### 문제 인식

3단계에서 `ResponseBase<T>`를 사용하면서 Response 클래스 정의가 verbose해졌습니다:

```csharp
// 3단계 Response 정의 (verbose) - 20줄
public sealed record class Response : ResponseBase<Response>
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public DateTime CreatedAt { get; init; }

    public Response() { }  // new() 제약 충족
    public Response(Guid productId, string name, string description, decimal price, int stockQuantity, DateTime createdAt)
    {
        ProductId = productId;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = createdAt;
    }
}
```

### 개선: Positional Record 문법

C#의 positional record 문법을 활용하여 코드를 대폭 줄였습니다:

```csharp
// 4단계 Response 정의 (concise) - 8줄
public sealed record class Response(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt) : ResponseBase<Response>
{
    public Response() : this(Guid.Empty, string.Empty, string.Empty, 0m, 0, default) { }
}
```

### 핵심 기술

1. **Positional 파라미터**: 주 생성자에서 모든 속성 정의
2. **`: this(...)` 호출**: 파라미터 없는 생성자에서 주 생성자 호출
3. **기본값 명시**: `new()` 제약을 충족하면서 각 속성에 적절한 기본값 제공

### 변환 결과 비교

| Response 클래스 | Before (줄) | After (줄) | 감소율 |
|----------------|:-----------:|:----------:|:------:|
| CreateProductCommand.Response | 20 | 8 | 60% |
| GetProductByIdQuery.Response | 22 | 9 | 59% |
| GetAllProductsQuery.Response | 10 | 4 | 60% |
| UpdateProductCommand.Response | 20 | 8 | 60% |

### 기본값 패턴

파라미터 없는 생성자의 기본값은 타입에 따라 다음과 같이 설정합니다:

```csharp
// 기본값 패턴
Guid       → Guid.Empty
string     → string.Empty
decimal    → 0m
int        → 0
DateTime   → default
DateTime?  → null
Seq<T>     → Seq<T>.Empty
```

### 주의사항

- 파라미터 없는 생성자의 기본값은 `CreateFail()` 호출 시에만 사용됨
- 실패 응답에서 비즈니스 속성은 의미 없음 (Error 필드만 유효)
- 성공 응답은 항상 positional 파라미터로 생성되므로 기본값 미사용

### 파라미터 없는 생성자 대안 검토

"기본값을 더 간결하게 작성할 수 없는가?"에 대한 검토 결과입니다.

#### 대안 1: Positional 파라미터에 기본값 지정

```csharp
public sealed record class Response(
    Guid ProductId = default,
    string Name = "",
    string Description = "",
    decimal Price = 0m,
    int StockQuantity = 0,
    DateTime CreatedAt = default) : ResponseBase<Response>;
```

**결과**: 컴파일 오류
- 모든 파라미터에 기본값이 있어도 `new Response()`는 여전히 컴파일 오류
- C# 컴파일러는 "모든 파라미터에 기본값이 있는 생성자"를 "파라미터 없는 생성자"로 취급하지 않음
- `new()` 제약은 진정한 파라미터 없는 생성자를 요구함

#### 대안 2: `new()` 제약 제거

```csharp
public abstract record ResponseBase<TSelf> : IResponse<TSelf>
    where TSelf : ResponseBase<TSelf>  // new() 제약 제거
{
    public static TSelf CreateFail(Error error)
        => ???  // 인스턴스 생성 방법 없음
}
```

**결과**: 불가능
- `new()` 제약 없이는 `CreateFail()`에서 `new TSelf()` 호출 불가
- 리플렉션으로 돌아가야 함 → 원점 회귀

#### 대안 3: `Activator.CreateInstance` 사용

```csharp
public static TSelf CreateFail(Error error)
    => Activator.CreateInstance<TSelf>() with { IsSuccess = false, Error = error };
```

**결과**: 기술적으로 가능하나 부적합
- 런타임 리플렉션 사용 → 성능 저하
- Native AOT 컴파일 시 문제 발생 가능
- 리플렉션 제거라는 원래 목표와 충돌

#### 대안 4: 팩토리 델리게이트 전달

```csharp
public interface IResponse<T> : IResponse where T : IResponse<T>
{
    static abstract T CreateFail(Error error);
    static abstract T CreateDefault();  // 추가
}
```

**결과**: 보일러플레이트 증가
- 사용자가 두 개의 static 메서드를 구현해야 함
- `ResponseBase<T>`의 장점(보일러플레이트 최소화) 상실

#### 결론: 현재 방식이 최선

```csharp
public Response() : this(Guid.Empty, string.Empty, string.Empty, 0m, 0, default) { }
```

이 한 줄이 필요한 이유:
1. **컴파일 타임 안전성**: 리플렉션 없이 `new TSelf()` 호출 가능
2. **Native AOT 호환**: 런타임 코드 생성 없음
3. **명시적 의도**: 기본값이 무엇인지 코드에서 명확히 보임
4. **최소한의 추가**: 단 1줄의 코드만 필요

C# 언어에서 "positional record + 파라미터 없는 생성자 자동 생성"을 지원하지 않는 한, 이것이 가장 간결한 방법입니다.

### Handler에서의 사용 (변경 없음)

```csharp
public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
{
    // 성공: positional 생성자 사용
    return new Response(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.CreatedAt);

    // 실패: static 메서드 호출 (기본값 사용)
    return Response.CreateFail(Error.New("오류 메시지"));
}
```

<br/>

## 5단계: ToResponse 확장 메서드

### 문제 인식

Handler에서 `Fin<T>`를 `IResponse<TResponse>`로 변환할 때 반복적인 `Match` 패턴이 발생합니다:

```csharp
// 반복되는 패턴 - 매번 Fail 케이스를 동일하게 처리
return createResult.Match<Response>(
    Succ: product => new Response(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.CreatedAt),
    Fail: error => Response.CreateFail(error));  // 항상 동일한 패턴
```

### 개선: ToResponse 확장 메서드

`Fin<T>`에 대한 확장 메서드를 추가하여 Handler 코드를 단순화했습니다:

```csharp
// FinExtensions.cs
public static class FinExtensions
{
    public static TResponse ToResponse<TSource, TResponse>(
        this Fin<TSource> fin,
        Func<TSource, TResponse> mapper)
        where TResponse : IResponse<TResponse>
    {
        return fin.Match(
            Succ: mapper,
            Fail: error => TResponse.CreateFail(error));
    }
}
```

### Handler 코드 비교

**Before (Match 패턴)**:
```csharp
return createResult.Match<Response>(
    Succ: product => new Response(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.CreatedAt),
    Fail: error => Response.CreateFail(error));
```

**After (ToResponse)**:
```csharp
return createResult.ToResponse(product => new Response(
    product.Id,
    product.Name,
    product.Description,
    product.Price,
    product.StockQuantity,
    product.CreatedAt));
```

### 효과

1. **Fail 케이스 자동 처리**: `TResponse.CreateFail(error)` 호출이 확장 메서드에 캡슐화
2. **코드량 감소**: 약 30% 감소 (Fail 람다 제거)
3. **일관성 보장**: 모든 Handler에서 동일한 실패 처리 보장
4. **타입 안전성 유지**: `static abstract` 인터페이스 멤버 활용으로 리플렉션 없음

### 추가 확장 메서드

다양한 사용 시나리오를 지원하기 위해 여러 오버로드를 제공합니다:

#### 1. ToResponse (기본)
```csharp
// 성공 시 mapper 호출, 실패 시 자동으로 CreateFail 호출
return result.ToResponse(product => new Response(product.Id, product.Name));
```

#### 2. ToResponse (성공/실패 모두 커스텀)
```csharp
// 성공/실패 모두 커스텀 처리 필요한 경우
return result.ToResponse(
    onSuccess: product => new Response(product.Id, product.Name),
    onFail: error => Response.CreateFail(Error.New($"Custom: {error.Message}")));
```

#### 3. ToResponseOrNull (성공만 처리)
```csharp
// 성공 케이스만 처리하고 실패는 상위에서 별도 처리
TResponse? response = result.ToResponseOrNull(product => new Response(product.Id));
if (response is null)
{
    // 실패 처리 로직
}
```

#### 4. ToFailResponseOrNull (실패만 처리)
```csharp
// 실패 케이스를 먼저 처리하고 빠른 반환
if (result.ToFailResponseOrNull<Product, Response>() is { } failResponse)
    return failResponse;

// 이후 성공 로직 처리
Product product = (Product)result;
```

### 적용 범위

`ToResponse`는 성공 시 단순 매핑만 필요한 경우에 적합합니다:

```csharp
// 적합한 케이스: 단순 매핑
return result.ToResponse(product => new Response(product.Id, product.Name));

// 적합하지 않은 케이스: 성공 값에 대한 추가 로직 필요
return result.Match<Response>(
    Succ: user =>
    {
        if (user is null)  // 추가 검증 필요
            return Response.CreateFail(Error.New("Not found"));
        return new Response(user.Id, user.Name);
    },
    Fail: error => Response.CreateFail(error));
```

<br/>

## 결론

### 최종 구조 비교

```
Before (Fin<T> 직접 사용):
┌─────────────────────────────────────────────────────────────┐
│ ICommandRequest<TResponse> : ICommand<Fin<TResponse>>       │
│   └── Handler: ValueTask<Fin<TResponse>>                    │
│       └── Pipeline: FinResponseUtilites.CreateFail() ← 리플렉션 │
│       └── Pipeline: FinResponseUtilites.IsSucc()     ← 리플렉션 │
│       └── Pipeline: FinResponseUtilites.GetError()   ← 리플렉션 │
└─────────────────────────────────────────────────────────────┘

After (IResponse<T> + ResponseBase<T>):
┌─────────────────────────────────────────────────────────────┐
│ ICommandRequest<TResponse> : ICommand<TResponse>            │
│   └── Handler: ValueTask<TResponse>                         │
│       └── Pipeline: TResponse.CreateFail()    ← static abstract │
│       └── Pipeline: response.IsSuccess        ← 직접 접근       │
│       └── Pipeline: response.Error            ← 직접 접근       │
└─────────────────────────────────────────────────────────────┘
```

### 핵심 교훈

1. **리플렉션 제거**: C# 11 `static abstract` 인터페이스 멤버로 타입 안전한 팩토리 패턴 구현
2. **CRTP 활용**: `ResponseBase<TSelf>`로 보일러플레이트 최소화
3. **순환 참조 주의**: Record의 `PrintMembers`가 모든 속성을 순회하므로 자기 참조 속성 주의
4. **점진적 검증**: 각 단계에서 빌드 → 테스트 → 실행 순으로 검증

### 요구 사항

- **.NET 7+**: C# 11의 `static abstract` 인터페이스 멤버 필요
- **파라미터 없는 생성자**: `new()` 제약으로 인해 필수

### 수정된 파일 목록

**핵심 타입 (4개)**
- `Src/Functorium/Applications/Cqrs/IResponse.cs`
- `Src/Functorium/Applications/Cqrs/IResponseT.cs` (신규)
- `Src/Functorium/Applications/Cqrs/ResponseBase.cs` (신규)
- `Src/Functorium/Applications/Cqrs/FinExtensions.cs` (신규)

**Request 인터페이스 (2개)**
- `Src/Functorium/Applications/Cqrs/ICommandRequest.cs`
- `Src/Functorium/Applications/Cqrs/IQueryRequest.cs`

**Pipeline (5개)**
- `Src/Functorium/Applications/Pipelines/UsecaseValidationPipeline.cs`
- `Src/Functorium/Applications/Pipelines/UsecaseExceptionPipeline.cs`
- `Src/Functorium/Applications/Pipelines/UsecaseLoggerPipeline.cs`
- `Src/Functorium/Applications/Pipelines/UsecaseTracePipeline.cs`
- `Src/Functorium/Applications/Pipelines/UsecaseMetricPipeline.cs`

**삭제된 파일 (1개)**
- `Src/Functorium/Applications/Cqrs/FinResponseUtilites.cs`

**Tutorial Usecase (7개)**
- `Tutorials/CqrsPipeline/Src/CqrsPipeline.Demo/Usecases/*.cs`
- `Tutorials/Cqrs/Src/Cqrs.Demo/Usecases/*.cs`

## 참고 자료

- [C# 11 Static Abstract Members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#generic-math-support)
- [Curiously Recurring Template Pattern (CRTP)](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern)
- [LanguageExt Fin&lt;T&gt;](https://github.com/louthy/language-ext)
