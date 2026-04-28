---
title: "CQRS와 값 객체 통합"
---
## 개요

API 엔드포인트에서 사용자 이름, 이메일, 나이를 문자열과 정수로 받아 처리한다면, 검증 로직은 어디에 위치해야 할까요? Controller에서? Application Layer에서? 값 객체를 CQRS 아키텍처와 통합하면 검증 로직이 값 객체 내부에 캡슐화되어, 유효하지 않은 데이터가 도메인 계층에 도달하는 것 자체를 방지할 수 있습니다.

이 장에서는 Mediator 패턴 기반의 Command/Query Handler에서 값 객체를 활용하여 입력을 검증하고, `Fin<T>`를 API Response로 변환하는 패턴을 다룹니다.

## 학습 목표

- Command Handler 내에서 입력값을 값 객체로 변환하여 검증하는 패턴을 구현할 수 있습니다.
- 조회 결과를 DTO로 변환할 때 값 객체의 값을 추출하는 방법을 적용할 수 있습니다.
- `Fin<T>`를 HTTP API Response로 변환하는 확장 메서드를 구현할 수 있습니다.
- `Bind` 패턴으로 여러 값 객체를 순차적으로 검증하고 조합할 수 있습니다.

## 왜 필요한가?

CQRS 아키텍처에서 값 객체를 통합하면 여러 이점을 얻을 수 있습니다.

Command Handler에서 원시 타입을 값 객체로 변환하면 검증 로직이 값 객체 내부에 캡슐화되어, Controller나 Application Layer에서 중복 검증이 불필요해집니다. Handler 내부에서는 검증된 `Email`, `Age`, `UserName` 같은 타입으로 작업하므로 유효하지 않은 데이터가 도메인 계층에 도달할 수 없습니다. 또한 `Fin<T>`를 `ApiResponse<T>`로 변환하면 모든 API 엔드포인트에서 일관된 응답 형식을 유지할 수 있습니다.

## 핵심 개념

### Command에서 값 객체 검증

Command Handler에서 입력값을 값 객체로 변환하여 검증합니다. `Bind` 패턴으로 여러 값 객체를 순차적으로 검증할 수 있습니다.

```csharp
public sealed record CreateUserCommand(string Name, string Email, int Age)
    : IRequest<Fin<CreateUserResponse>>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Fin<CreateUserResponse>>
{
    public ValueTask<Fin<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Bind 패턴으로 순차적 검증
        var result = UserName.Create(request.Name)
            .Bind(name => Email.Create(request.Email)
                .Bind(email => Age.Create(request.Age)
                    .Map(age =>
                    {
                        var userId = _repository.Save(name, email, age);
                        return new CreateUserResponse(userId);
                    })));

        return ValueTask.FromResult(result);
    }
}
```

`Bind`는 성공 시에만 다음 단계로 진행합니다. 첫 번째 검증이 실패하면 이후 검증은 수행되지 않고 즉시 에러가 반환됩니다.

### Query와 DTO 변환

Repository에서 값 객체로 저장된 데이터를 조회하고, DTO로 변환하여 반환합니다.

```csharp
public sealed record GetUserQuery(Guid UserId) : IRequest<Fin<UserDto>>;

public sealed record UserDto(string Name, string Email, int Age);

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, Fin<UserDto>>
{
    public ValueTask<Fin<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var result = _repository.FindById(request.UserId);
        return ValueTask.FromResult(result);
    }
}

// Repository
public Fin<UserDto> FindById(Guid id)
{
    if (_users.TryGetValue(id, out var user))
    {
        // 값 객체의 Value를 추출하여 DTO 생성
        return new UserDto(user.Name.Value, user.Email.Value, user.Age.Value);
    }
    return RepositoryErrors.UserNotFound(id);
}
```

도메인에서는 `UserName`, `Email`, `Age` 값 객체를 사용하고, API 응답에는 원시 타입의 DTO를 반환하여 도메인 모델과 API 계약을 분리합니다.

### Fin\<T\> -> ApiResponse 변환

`Fin<T>`를 HTTP API에서 사용할 수 있는 `ApiResponse<T>`로 변환하는 확장 메서드입니다.

```csharp
public static class FinExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: data => ApiResponse<T>.Success(data),
            Fail: error => ApiResponse<T>.Failure(error.Message)
        );
    }
}

public class ApiResponse<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ApiResponse<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static ApiResponse<T> Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
```

내부적으로는 `Fin<T>`로 성공/실패를 처리하고, API 경계에서 클라이언트가 이해할 수 있는 형식으로 변환합니다.

### Mediator 패턴과 값 객체

Mediator 패턴은 Command/Query와 Handler 사이의 결합도를 낮춥니다. 값 객체와 결합하면 입력 검증이 Handler 내부로 캡슐화됩니다.

```csharp
// DI 설정
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
services.AddSingleton<UserRepository>();

// Command 전송
var command = new CreateUserCommand("홍길동", "hong@example.com", 25);
var result = await mediator.Send(command);

// 결과 처리
result.Match(
    Succ: response => Console.WriteLine($"성공: 사용자 ID = {response.UserId}"),
    Fail: error => Console.WriteLine($"실패: {error.Message}")
);
```

Controller는 요청을 Command로 변환하여 전송하고, Handler는 검증과 비즈니스 로직을 담당합니다. 각 계층의 책임이 명확해집니다.

## 실전 지침

### 예상 출력
```
=== CQRS와 값 객체 통합 ===

1. Command에서 값 객체 사용
────────────────────────────────────────
   성공: 사용자 ID = 550e8400-e29b-41d4-a716-446655440001
   실패: 사용자 이름이 비어있습니다.

2. Query에서 값 객체 사용
────────────────────────────────────────
   사용자: 기존 사용자, 이메일: existing@example.com, 나이: 30
   오류: 사용자를 찾을 수 없습니다.

3. Fin<T> → Response 변환 (FinExtensions)
────────────────────────────────────────
   성공 응답: Status=True, Data=UserDto { Name = 홍길동, Email = hong@example.com, Age = 25 }
   실패 응답: Status=False, Error=사용자를 찾을 수 없습니다.
```

### Controller에서의 사용 예시

실제 Web API 프로젝트에서 Mediator와 `ToApiResponse()`를 조합하는 패턴입니다.

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Name, request.Email, request.Age);
        var result = await _mediator.Send(command);

        var response = result.ToApiResponse();

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);

        var response = result.ToApiResponse();

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }
}
```

## 프로젝트 설명

### 프로젝트 구조
```
03-CQRS-Integration/
├── CqrsIntegration/
│   ├── Program.cs                # 메인 실행 파일 (값 객체, Command/Query, Handler 포함)
│   └── CqrsIntegration.csproj    # 프로젝트 파일
└── README.md                     # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Mediator.Abstractions" />
  <PackageReference Include="Mediator.SourceGenerator" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
</ItemGroup>
```

### 핵심 코드

**값 객체 정의**
```csharp
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Domain.Empty(value ?? "null");
        if (!value.Contains('@'))
            return Domain.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    // IEquatable<Email> 구현...
}
```

**Command/Query 정의**
```csharp
// Command: 사용자 생성
public sealed record CreateUserCommand(string Name, string Email, int Age)
    : IRequest<Fin<CreateUserResponse>>;

public sealed record CreateUserResponse(Guid UserId);

// Query: 사용자 조회
public sealed record GetUserQuery(Guid UserId) : IRequest<Fin<UserDto>>;

public sealed record UserDto(string Name, string Email, int Age);
```

**Handler 구현**
```csharp
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Fin<CreateUserResponse>>
{
    private readonly UserRepository _repository;

    public CreateUserCommandHandler(UserRepository repository) => _repository = repository;

    public ValueTask<Fin<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = UserName.Create(request.Name)
            .Bind(name => Email.Create(request.Email)
                .Bind(email => Age.Create(request.Age)
                    .Map(age =>
                    {
                        var userId = _repository.Save(name, email, age);
                        return new CreateUserResponse(userId);
                    })));

        return ValueTask.FromResult(result);
    }
}
```

## 한눈에 보는 정리

### CQRS와 값 객체 통합 패턴

각 계층이 값 객체를 어떻게 활용하는지 정리합니다.

| 계층 | 역할 | 값 객체 활용 |
|------|------|-------------|
| Controller | 요청 수신, 응답 반환 | DTO 사용, `ToApiResponse()` 변환 |
| Command/Query | 요청 데이터 전달 | 원시 타입으로 전달 |
| Handler | 검증, 비즈니스 로직 | 값 객체로 변환하여 검증 |
| Repository | 데이터 저장/조회 | 값 객체로 저장, DTO로 반환 |

### Bind vs Apply 패턴 선택

검증 전략에 따라 적합한 패턴을 선택합니다.

| 패턴 | 특징 | 적합한 상황 |
|------|------|------------|
| `Bind` (순차 검증) | 첫 번째 실패 시 중단 | 의존적인 검증, 리소스 절약 |
| `Apply` (병렬 검증) | 모든 에러 수집 | 폼 검증, 사용자 피드백 |

### API Response 구조

```
성공 시:
{
  "isSuccess": true,
  "data": { ... },
  "errorMessage": null
}

실패 시:
{
  "isSuccess": false,
  "data": null,
  "errorMessage": "사용자 이름이 비어있습니다."
}
```

## FAQ

### Q1: Command에서 원시 타입 대신 값 객체를 직접 받을 수 없나요?
**A**: 기술적으로 가능하지만 권장하지 않습니다. Command/Query는 API 경계의 계약이므로 직렬화 가능한 원시 타입을 사용하는 것이 일반적입니다. Handler에서 값 객체로 변환하는 것이 더 명확하고 테스트하기 쉽습니다.

### Q2: Apply 패턴으로 모든 에러를 한 번에 수집하려면?
**A**: `Validation<Error, T>`와 `Apply` 패턴을 사용합니다.

```csharp
public ValueTask<Fin<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken ct)
{
    var validation = (UserName.Validate(request.Name),
                      Email.Validate(request.Email),
                      Age.Validate(request.Age))
        .Apply((name, email, age) =>
        {
            var userId = _repository.Save(name, email, age);
            return new CreateUserResponse(userId);
        });

    return ValueTask.FromResult(validation.ToFin());
}
```

사용자 피드백이 중요한 폼 검증에서는 모든 필드의 에러를 한 번에 보여줄 수 있는 Apply 패턴이 더 적합합니다.

### Q3: Repository에서 Fin\<T\>를 반환하는 이유는?
**A**: "사용자를 찾을 수 없음"은 예외적 상황이 아니라 비즈니스적으로 예상 가능한 결과입니다. `Fin<T>`를 반환하면 호출자가 성공과 실패 모두를 명시적으로 처리해야 하므로 누락 없이 안전한 코드를 작성할 수 있습니다.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd CqrsIntegration.Tests.Unit
dotnet test
```

### 테스트 구조
```
CqrsIntegration.Tests.Unit/
├── CreateUserCommandHandlerTests.cs  # Command 핸들러 테스트
├── GetUserQueryHandlerTests.cs       # Query 핸들러 테스트
└── FinExtensionsTests.cs             # Fin→ApiResponse 변환 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| CreateUserCommandHandlerTests | 값 객체 검증, Bind 순차 검증, 성공/실패 시나리오 |
| GetUserQueryHandlerTests | 존재하는 사용자 조회, 미존재 사용자 처리 |
| FinExtensionsTests | ToApiResponse 변환, Success/Failure 매핑 |

CQRS 통합으로 값 객체가 API 계층에서 도메인 계층까지 자연스럽게 흐르는 구조를 갖추었습니다. 다음 장에서는 이 모든 패턴을 효과적으로 검증하기 위한 테스트 전략을 다룹니다.

---

→ [4장: 테스트 전략](../04-Testing-Strategies/)
