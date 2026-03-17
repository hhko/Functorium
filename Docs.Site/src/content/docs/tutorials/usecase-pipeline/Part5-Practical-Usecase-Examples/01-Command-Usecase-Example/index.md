---
title: "Command Usecase 예제"
---

## 개요

실제 Command Usecase에서 Pipeline과 FinResponse는 어떻게 동작할까요? 이 장에서는 Functorium의 `ICommandRequest<TSuccess>` 인터페이스를 활용하여 **Command Usecase의 완전한 구현 예제**를 작성합니다. Nested class 패턴을 사용하여 Request, Response, Validator, Handler를 하나의 클래스 안에 응집력 있게 구성하고, `FinResponse<T>`를 통해 성공/실패를 타입 안전하게 처리합니다.

```
Command Usecase 구조:

CreateProductCommand (최상위 클래스)
├── Request   : ICommandRequest<Response>           ← 요청 정의
├── Response                                        ← 응답 정의
├── Validator                                       ← 유효성 검사
└── Handler   : ICommandUsecase<Request, Response>  ← 비즈니스 로직
```

## 핵심 개념

### 1. ICommandRequest 인터페이스

`ICommandRequest<TSuccess>`는 Mediator의 `ICommand<FinResponse<TSuccess>>`를 상속합니다. 이를 통해 Request가 자동으로 Pipeline을 거치게 됩니다.

```csharp
// Functorium 정의
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
```

Request record가 `ICommandRequest<Response>`를 구현하면, Mediator Pipeline이 이 요청을 Command로 인식하고 Transaction Pipeline 등을 적용합니다.

Handler는 `ICommandUsecase<TCommand, TSuccess>`를 구현합니다. 이 인터페이스는 `ICommandHandler<TCommand, FinResponse<TSuccess>>`를 상속하므로, Handler가 이를 구현하면 Mediator가 자동으로 Pipeline 체인에 등록합니다:

```csharp
// Functorium 정의
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess> { }
```

### 2. Nested Class 패턴

하나의 Usecase와 관련된 모든 타입을 최상위 클래스 안에 중첩하여 정의합니다.

```csharp
public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public static class Validator { ... }
    public sealed class Handler : ICommandUsecase<Request, Response> { ... }
}
```

이 패턴의 장점:
- **응집력**: 관련 타입이 한 파일에 모여 있어 탐색이 쉽습니다.
- **네이밍 충돌 방지**: `CreateProductCommand.Request`처럼 전체 경로로 접근합니다.
- **의도 표현**: 클래스 이름만으로 Command/Query 구분이 명확합니다.

### 3. FinResponse를 통한 결과 처리

Handler는 `ValueTask<FinResponse<Response>>`를 반환합니다. `ICommandUsecase`가 비동기 시그니처를 요구하기 때문입니다. Validator의 결과를 `Bind`로 체이닝하여 검증-비즈니스 로직을 Railway 방식으로 연결합니다:

```csharp
public ValueTask<FinResponse<Response>> Handle(Request command, CancellationToken cancellationToken)
{
    var result = Validator.Validate(command)
        .Bind(req =>
        {
            var productId = Guid.NewGuid().ToString("N")[..8];
            return FinResponse.Succ(new Response(productId, req.Name, req.Price));
        });

    return new ValueTask<FinResponse<Response>>(result);
}
```

`Bind`를 사용하면 `if (validated.IsFail)` 분기 없이 검증 실패가 자동으로 전파됩니다.

### 4. Validator 분리

Validator는 static class로 정의하여 Handler와 독립적으로 테스트할 수 있습니다. Validator는 `FinResponse<Request>`를 반환하여 검증 결과를 Railway 방식으로 전달합니다.

```csharp
public static class Validator
{
    public static FinResponse<Request> Validate(Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.New("Name is required");

        if (request.Price <= 0)
            return Error.New("Price must be positive");

        return request;  // 암시적 변환
    }
}
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `ICommandRequest<TSuccess>`와 `ICommandUsecase<TCommand, TSuccess>` 인터페이스의 역할과 Pipeline 연결 방식을 이해할 수 있다
2. Nested class 패턴으로 Request/Response/Validator/Handler를 응집력 있게 구성할 수 있다
3. `FinResponse<T>`의 암시적 변환을 활용하여 성공/실패를 간결하게 반환할 수 있다
4. Validator를 Handler에서 분리하여 독립적으로 테스트할 수 있다

## FAQ

### Q1: Nested class 패턴에서 Request, Response, Validator, Handler를 분리된 파일로 나눌 수 있나요?
**A**: `partial class`를 사용하면 각 중첩 타입을 별도 파일에 정의할 수 있습니다. 하지만 하나의 Usecase가 한 파일에 모여 있으면 **탐색과 이해가 쉬워**지므로, 중첩 타입의 크기가 작은 경우에는 한 파일에 두는 것을 권장합니다.

### Q2: `ICommandRequest<TSuccess>`에서 `TSuccess`가 `Response`인데, 왜 `FinResponse<Response>`를 직접 사용하지 않나요?
**A**: `ICommandRequest<TSuccess>`가 내부적으로 `ICommand<FinResponse<TSuccess>>`를 상속하므로, `TSuccess`만 지정하면 `FinResponse<Response>`가 **자동으로 결정**됩니다. 이를 통해 Usecase 코드에서 `FinResponse` 래핑을 명시적으로 작성하지 않아도 됩니다.

### Q3: Validator가 `FinResponse<Request>`를 반환하는 이유는 무엇인가요?
**A**: Validator가 `FinResponse<Request>`를 반환하면, 검증 성공 시 원본 Request를 그대로 전달하고, 실패 시 `Error`를 포함한 실패 응답을 반환합니다. 이를 통해 **Railway-Oriented Programming** 방식으로 검증 결과를 Handler에 자연스럽게 체이닝할 수 있습니다.

### Q4: 암시적 변환으로 `return Error.New("...")` 형태가 가능한 원리는 무엇인가요?
**A**: `FinResponse<A>`에 `implicit operator`가 정의되어 있어 `Error` 타입의 값이 자동으로 `FinResponse<A>.Fail(error)`로 변환됩니다. 마찬가지로 `A` 타입의 값은 `FinResponse<A>.Succ(value)`로 변환됩니다. 이 암시적 변환이 보일러플레이트를 크게 줄입니다.

Command Usecase의 구조를 확인했으니, 다음 장에서는 읽기 전용인 Query Usecase가 어떻게 다르게 구성되는지 살펴봅니다.

## 프로젝트 구조

```
01-Command-Usecase-Example/
├── CommandUsecaseExample/
│   ├── CommandUsecaseExample.csproj
│   ├── CreateProductCommand.cs
│   └── Program.cs
├── CommandUsecaseExample.Tests.Unit/
│   ├── CommandUsecaseExample.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── CreateProductCommandTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project CommandUsecaseExample

# 테스트 실행
dotnet test --project CommandUsecaseExample.Tests.Unit
```

