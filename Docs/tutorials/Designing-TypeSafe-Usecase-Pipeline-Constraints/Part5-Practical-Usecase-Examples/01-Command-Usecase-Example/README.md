# 18장: Command Usecase 완전 예제

## 개요

이 장에서는 Functorium의 `ICommandRequest<TSuccess>` 인터페이스를 활용하여 **Command Usecase의 완전한 구현 예제**를 작성합니다. Nested class 패턴을 사용하여 Request, Response, Validator, Handler를 하나의 클래스 안에 응집력 있게 구성하고, `FinResponse<T>`를 통해 성공/실패를 타입 안전하게 처리합니다.

```
Command Usecase 구조:

CreateProductCommand (최상위 클래스)
├── Request   : ICommandRequest<Response>   ← 요청 정의
├── Response                                ← 응답 정의
├── Validator                               ← 유효성 검사
└── Handler                                 ← 비즈니스 로직
```

## 핵심 개념

### 1. ICommandRequest 인터페이스

`ICommandRequest<TSuccess>`는 Mediator의 `ICommand<FinResponse<TSuccess>>`를 상속합니다. 이를 통해 Request가 자동으로 Pipeline을 거치게 됩니다.

```csharp
// Functorium 정의
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
```

Request record가 `ICommandRequest<Response>`를 구현하면, Mediator Pipeline이 이 요청을 Command로 인식하고 Transaction Pipeline 등을 적용합니다.

### 2. Nested Class 패턴

하나의 Usecase와 관련된 모든 타입을 최상위 클래스 안에 중첩하여 정의합니다.

```csharp
public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public static class Validator { ... }
    public sealed class Handler { ... }
}
```

이 패턴의 장점:
- **응집력**: 관련 타입이 한 파일에 모여 있어 탐색이 쉽습니다.
- **네이밍 충돌 방지**: `CreateProductCommand.Request`처럼 전체 경로로 접근합니다.
- **의도 표현**: 클래스 이름만으로 Command/Query 구분이 명확합니다.

### 3. FinResponse를 통한 결과 처리

Handler는 `FinResponse<Response>`를 반환합니다. 성공 시 암시적 변환으로 Response를 직접 반환하고, 실패 시 `Error`를 반환합니다.

```csharp
// 성공: 암시적 변환 (Response → FinResponse<Response>)
return new Response(productId, request.Name, request.Price);

// 실패: 암시적 변환 (Error → FinResponse<Response>)
return Error.New("Name is required");
```

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

1. `ICommandRequest<TSuccess>` 인터페이스의 역할과 Pipeline 연결 방식을 이해할 수 있다
2. Nested class 패턴으로 Request/Response/Validator/Handler를 응집력 있게 구성할 수 있다
3. `FinResponse<T>`의 암시적 변환을 활용하여 성공/실패를 간결하게 반환할 수 있다
4. Validator를 Handler에서 분리하여 독립적으로 테스트할 수 있다

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

---

[← 이전: 17장 Fin → FinResponse 브릿지](../../Part4-Pipeline-Constraint-Patterns/04-Fin-To-FinResponse-Bridge/README.md) | [다음: 19장 Query Usecase 완전 예제 →](../02-Query-Usecase-Example/README.md)
